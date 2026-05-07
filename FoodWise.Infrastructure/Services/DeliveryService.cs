using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// DeliveryService, QR destekli teslim kutusu akışını yönetir.
// Onaylanan talepten teslimat oluşturur, boş kutu atar, bırakma ve teslim alma işlemlerini kontrol eder.
using FoodWise.Application.DTOs.Delivery;
using FoodWise.Application.Interfaces;
using FoodWise.Domain.Entities;
using FoodWise.Domain.Enums;
using FoodWise.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FoodWise.Infrastructure.Services;

public class DeliveryService : IDeliveryService
{
    private readonly FoodWiseDbContext _context;

    public DeliveryService(FoodWiseDbContext context)
    {
        _context = context;
    }

    public async Task<DeliveryDto?> CreateDeliveryAsync(string donorUserId, int shareRequestId)
    {
        // Sadece onaylanmış paylaşım talebi için teslimat oluşturulur.
        var request = await _context.ShareRequests
            .Include(x => x.ShareListing)
                .ThenInclude(x => x.StockItem)
                    .ThenInclude(x => x.Product)
            .Include(x => x.ShareListing)
                .ThenInclude(x => x.StockItem)
                    .ThenInclude(x => x.Unit)
            .Include(x => x.ShareListing)
                .ThenInclude(x => x.DeliveryPoint)
            .FirstOrDefaultAsync(x => x.Id == shareRequestId);

        if (request == null)
            return null;

        // Teslimatı sadece ilan sahibi oluşturabilir.
        if (request.ShareListing.DonorUserId != donorUserId)
            return null;

        // Talep onaylanmamışsa teslimat oluşturulmaz.
        if (request.Status != ShareRequestStatus.Approved)
            return null;

        // Aynı talep için daha önce teslimat oluşturulduysa tekrar oluşturma.
        var existingDelivery = await GetDeliveryEntityByRequestIdAsync(shareRequestId);

        if (existingDelivery != null)
            return MapToDto(existingDelivery);

        // İlgili teslim noktasında boş ve aktif bir kutu aranır.
        var availableBox = await _context.DeliveryBoxes
            .FirstOrDefaultAsync(x =>
                x.DeliveryPointId == request.ShareListing.DeliveryPointId &&
                x.IsActive &&
                !x.IsOccupied);

        if (availableBox == null)
            return null;

        // Teslimat kaydı oluşturulur ve kutu rezerve edilir.
        var delivery = new Delivery
        {
            ShareListingId = request.ShareListingId,
            ShareRequestId = request.Id,
            DonorUserId = request.ShareListing.DonorUserId,
            ReceiverUserId = request.RequesterUserId,
            DeliveryPointId = request.ShareListing.DeliveryPointId,
            DeliveryBoxId = availableBox.Id,

            // QrToken teslimatın iç takip kodudur.
            // Asıl okutulan QR, DeliveryBox.QrCodeValue alanıdır.
            QrToken = Guid.NewGuid().ToString("N"),

            Status = DeliveryStatus.QrGenerated,
            CreatedAt = DateTime.Now,
            ExpiresAt = request.ShareListing.PickupEndTime
        };

        availableBox.IsOccupied = true;
        availableBox.UpdatedAt = DateTime.Now;

        request.ShareListing.Status = ShareListingStatus.QrGenerated;
        request.ShareListing.UpdatedAt = DateTime.Now;

        await _context.Deliveries.AddAsync(delivery);
        await _context.SaveChangesAsync();

        var createdDelivery = await GetDeliveryEntityByIdAsync(delivery.Id);

        return createdDelivery == null ? null : MapToDto(createdDelivery);
    }

    public async Task<DeliveryDto?> MarkAsDroppedOffAsync(string donorUserId, int deliveryId, DropOffDeliveryDto model)
    {
        // Ürün sahibi ürünü kutuya bıraktığında teslimat DroppedOff durumuna geçer.
        var delivery = await GetDeliveryEntityByIdAsync(deliveryId);

        if (delivery == null)
            return null;

        // Sadece bağışçı/ilan sahibi ürünü kutuya bıraktığını işaretleyebilir.
        if (delivery.DonorUserId != donorUserId)
            return null;

        // Teslimat daha önce oluşturulmuş ama henüz bırakılmamış olmalıdır.
        if (delivery.Status != DeliveryStatus.QrGenerated && delivery.Status != DeliveryStatus.Pending)
            return null;

        if (delivery.ExpiresAt < DateTime.Now)
        {
            delivery.Status = DeliveryStatus.Expired;

            if (delivery.DeliveryBox != null)
            {
                delivery.DeliveryBox.IsOccupied = false;
                delivery.DeliveryBox.UpdatedAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();
            return null;
        }

        delivery.Status = DeliveryStatus.DroppedOff;
        delivery.DroppedOffAt = DateTime.Now;
        delivery.DropOffImageUrl = model.DropOffImageUrl;
        delivery.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        var updatedDelivery = await GetDeliveryEntityByIdAsync(delivery.Id);

        return updatedDelivery == null ? null : MapToDto(updatedDelivery);
    }

    public async Task<DeliveryDto?> ScanBoxQrAsync(string receiverUserId, ScanDeliveryBoxDto model)
    {
        // Alıcı kutudaki QR kodu okutur.
        // Sistem önce QR değerinden kutuyu bulur.
        var deliveryBox = await _context.DeliveryBoxes
            .FirstOrDefaultAsync(x =>
                x.QrCodeValue == model.QrCodeValue &&
                x.IsActive);

        if (deliveryBox == null)
            return null;

        // Bu kutuda, bu alıcıya ait, bırakılmış ve teslim alınmamış teslimat aranır.
        var delivery = await _context.Deliveries
            .Include(x => x.ShareListing)
                .ThenInclude(x => x.StockItem)
                    .ThenInclude(x => x.Product)
            .Include(x => x.ShareListing)
                .ThenInclude(x => x.StockItem)
                    .ThenInclude(x => x.Unit)
            .Include(x => x.DeliveryPoint)
            .Include(x => x.DeliveryBox)
            .FirstOrDefaultAsync(x =>
                x.DeliveryBoxId == deliveryBox.Id &&
                x.ReceiverUserId == receiverUserId &&
                x.Status == DeliveryStatus.DroppedOff);

        if (delivery == null)
            return null;

        // Süresi dolmuş teslimatlar geçersiz kabul edilir.
        if (delivery.ExpiresAt < DateTime.Now)
        {
            delivery.Status = DeliveryStatus.Expired;

            deliveryBox.IsOccupied = false;
            deliveryBox.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return null;
        }

        // QR doğruysa teslimat bilgileri alıcıya gösterilir.
        return MapToDto(delivery);
    }

    public async Task<DeliveryDto?> CompleteDeliveryAsync(string receiverUserId, int deliveryId)
    {
        // Alıcı QR doğrulamasından sonra ürünü teslim aldığını onaylar.
        var delivery = await GetDeliveryEntityByIdAsync(deliveryId);

        if (delivery == null)
            return null;

        // Sadece teslimatın alıcısı teslimatı tamamlayabilir.
        if (delivery.ReceiverUserId != receiverUserId)
            return null;

        // Ürün teslim kutusuna bırakılmış olmalıdır.
        if (delivery.Status != DeliveryStatus.DroppedOff)
            return null;

        if (delivery.ExpiresAt < DateTime.Now)
        {
            delivery.Status = DeliveryStatus.Expired;

            if (delivery.DeliveryBox != null)
            {
                delivery.DeliveryBox.IsOccupied = false;
                delivery.DeliveryBox.UpdatedAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();
            return null;
        }

        delivery.Status = DeliveryStatus.Delivered;
        delivery.PickedUpAt = DateTime.Now;
        delivery.DeliveredAt = DateTime.Now;
        delivery.UpdatedAt = DateTime.Now;

        // İlan teslim edildi durumuna geçer.
        delivery.ShareListing.Status = ShareListingStatus.Delivered;
        delivery.ShareListing.UpdatedAt = DateTime.Now;

        // Stok ürünü artık paylaşıldı kabul edilir.
        delivery.ShareListing.StockItem.Status = StockItemStatus.Shared;
        delivery.ShareListing.StockItem.UpdatedAt = DateTime.Now;

        // Kutu boşaltılır.
        if (delivery.DeliveryBox != null)
        {
            delivery.DeliveryBox.IsOccupied = false;
            delivery.DeliveryBox.UpdatedAt = DateTime.Now;
        }

        await _context.SaveChangesAsync();

        var completedDelivery = await GetDeliveryEntityByIdAsync(delivery.Id);

        return completedDelivery == null ? null : MapToDto(completedDelivery);
    }

    public async Task<List<DeliveryDto>> GetMyDonatedDeliveriesAsync(string donorUserId)
    {
        // Ürün sahibinin oluşturduğu teslimatlar listelenir.
        var deliveries = await _context.Deliveries
            .Include(x => x.ShareListing)
                .ThenInclude(x => x.StockItem)
                    .ThenInclude(x => x.Product)
            .Include(x => x.ShareListing)
                .ThenInclude(x => x.StockItem)
                    .ThenInclude(x => x.Unit)
            .Include(x => x.DeliveryPoint)
            .Include(x => x.DeliveryBox)
            .Where(x => x.DonorUserId == donorUserId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        return deliveries.Select(MapToDto).ToList();
    }

    public async Task<List<DeliveryDto>> GetMyReceivedDeliveriesAsync(string receiverUserId)
    {
        // Alıcının teslim alacağı veya aldığı teslimatlar listelenir.
        var deliveries = await _context.Deliveries
            .Include(x => x.ShareListing)
                .ThenInclude(x => x.StockItem)
                    .ThenInclude(x => x.Product)
            .Include(x => x.ShareListing)
                .ThenInclude(x => x.StockItem)
                    .ThenInclude(x => x.Unit)
            .Include(x => x.DeliveryPoint)
            .Include(x => x.DeliveryBox)
            .Where(x => x.ReceiverUserId == receiverUserId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        return deliveries.Select(MapToDto).ToList();
    }

    private async Task<Delivery?> GetDeliveryEntityByIdAsync(int deliveryId)
    {
        return await _context.Deliveries
            .Include(x => x.ShareListing)
                .ThenInclude(x => x.StockItem)
                    .ThenInclude(x => x.Product)
            .Include(x => x.ShareListing)
                .ThenInclude(x => x.StockItem)
                    .ThenInclude(x => x.Unit)
            .Include(x => x.DeliveryPoint)
            .Include(x => x.DeliveryBox)
            .FirstOrDefaultAsync(x => x.Id == deliveryId);
    }

    private async Task<Delivery?> GetDeliveryEntityByRequestIdAsync(int shareRequestId)
    {
        return await _context.Deliveries
            .Include(x => x.ShareListing)
                .ThenInclude(x => x.StockItem)
                    .ThenInclude(x => x.Product)
            .Include(x => x.ShareListing)
                .ThenInclude(x => x.StockItem)
                    .ThenInclude(x => x.Unit)
            .Include(x => x.DeliveryPoint)
            .Include(x => x.DeliveryBox)
            .FirstOrDefaultAsync(x => x.ShareRequestId == shareRequestId);
    }

    private DeliveryDto MapToDto(Delivery delivery)
    {
        return new DeliveryDto
        {
            Id = delivery.Id,
            ShareListingId = delivery.ShareListingId,
            ShareRequestId = delivery.ShareRequestId,
            DonorUserId = delivery.DonorUserId,
            ReceiverUserId = delivery.ReceiverUserId,
            DeliveryPointId = delivery.DeliveryPointId,
            DeliveryPointName = delivery.DeliveryPoint.Name,
            DeliveryBoxId = delivery.DeliveryBoxId,
            BoxCode = delivery.DeliveryBox?.BoxCode,
            BoxQrCodeValue = delivery.DeliveryBox?.QrCodeValue,
            ProductName = delivery.ShareListing.StockItem.Product.Name,
            Quantity = delivery.ShareListing.Quantity,
            UnitName = delivery.ShareListing.StockItem.Unit.ShortName,
            Status = delivery.Status.ToString(),
            DroppedOffAt = delivery.DroppedOffAt,
            PickedUpAt = delivery.PickedUpAt,
            DeliveredAt = delivery.DeliveredAt,
            ExpiresAt = delivery.ExpiresAt,
            DropOffImageUrl = delivery.DropOffImageUrl
        };
    }
}

// DeliveryService, QR destekli ortak teslim kutusu akışını yönetir.
// Onaylanan talepten teslimat oluşturur, aktif QR kutusu atar,
// bırakma, QR doğrulama ve teslim alma işlemlerini kontrol eder.
// Bir teslim kutusu birden fazla aktif teslimatı barındırabilir.

using FoodWise.Application.DTOs.Delivery;
using FoodWise.Application.DTOs.Notification;
using FoodWise.Application.Interfaces;
using FoodWise.Domain.Entities;
using FoodWise.Domain.Enums;
using FoodWise.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FoodWise.Infrastructure.Services;

public class DeliveryService : IDeliveryService
{
    private readonly FoodWiseDbContext _context;
    private readonly IEcoPointService _ecoPointService;
    private readonly INotificationService _notificationService;

    public DeliveryService(
        FoodWiseDbContext context,
        IEcoPointService ecoPointService,
        INotificationService notificationService)
    {
        _context = context;
        _ecoPointService = ecoPointService;
        _notificationService = notificationService;
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

        // Aynı talep için daha önce teslimat oluşturulduysa tekrar oluşturulmaz.
        var existingDelivery = await GetDeliveryEntityByRequestIdAsync(shareRequestId);

        if (existingDelivery != null)
            return MapToDto(existingDelivery);

        // İlgili teslim noktasındaki aktif QR kutularından biri seçilir.
        // Kutular ortak kullanım mantığıyla çalışır; bir kutuya birden fazla teslimat atanabilir.
        // Birden fazla aktif kutu varsa aktif teslimat sayısı daha az olan kutu tercih edilir.
        var availableBoxInfo = await _context.DeliveryBoxes
            .Where(x =>
                x.DeliveryPointId == request.ShareListing.DeliveryPointId &&
                x.IsActive)
            .Select(x => new
            {
                Box = x,
                ActiveDeliveryCount = _context.Deliveries.Count(d =>
                    d.DeliveryBoxId == x.Id &&
                    (
                        d.Status == DeliveryStatus.Pending ||
                        d.Status == DeliveryStatus.QrGenerated ||
                        d.Status == DeliveryStatus.DroppedOff
                    ))
            })
            .OrderBy(x => x.ActiveDeliveryCount)
            .ThenBy(x => x.Box.BoxCode)
            .FirstOrDefaultAsync();

        var availableBox = availableBoxInfo?.Box;

        if (availableBox == null)
            return null;

        var delivery = new Delivery
        {
            ShareListingId = request.ShareListingId,
            ShareRequestId = request.Id,
            DonorUserId = request.ShareListing.DonorUserId,
            ReceiverUserId = request.RequesterUserId,
            DeliveryPointId = request.ShareListing.DeliveryPointId,
            DeliveryBoxId = availableBox.Id,

            // QrToken teslimatın iç takip kodudur.
            // Kullanıcının doğruladığı QR değeri DeliveryBox.QrCodeValue alanıdır.
            QrToken = Guid.NewGuid().ToString("N"),

            Status = DeliveryStatus.QrGenerated,
            CreatedAt = DateTime.Now,
            ExpiresAt = request.ShareListing.PickupEndTime,

            // Teslimat ilk oluşturulduğunda QR henüz doğrulanmamıştır.
            IsQrVerified = false,
            QrVerifiedAt = null
        };

        request.ShareListing.Status = ShareListingStatus.QrGenerated;
        request.ShareListing.UpdatedAt = DateTime.Now;

        await _context.Deliveries.AddAsync(delivery);
        await _context.SaveChangesAsync();

        await _notificationService.CreateAsync(request.RequesterUserId, new CreateNotificationDto
        {
            Title = "Teslimat oluşturuldu",
            Message = $"{request.ShareListing.StockItem.Product.Name} için teslimat oluşturuldu. Teslimatlar sayfasından takip edebilirsin.",
            Type = NotificationType.DeliveryCreated,
            TargetUrl = "/Delivery/Incoming"
        });

        var createdDelivery = await GetDeliveryEntityByIdAsync(delivery.Id);

        return createdDelivery == null ? null : MapToDto(createdDelivery);
    }

    public async Task<DeliveryDto?> MarkAsDroppedOffAsync(string donorUserId, int deliveryId, DropOffDeliveryDto model)
    {
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
            delivery.UpdatedAt = DateTime.Now;

            if (delivery.ShareListing != null)
            {
                delivery.ShareListing.Status = ShareListingStatus.Expired;
                delivery.ShareListing.UpdatedAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();
            return null;
        }

        delivery.Status = DeliveryStatus.DroppedOff;
        delivery.DroppedOffAt = DateTime.Now;
        delivery.DropOffImageUrl = model.DropOffImageUrl;
        delivery.UpdatedAt = DateTime.Now;

        // Ürün kutuya bırakıldığında alıcının QR doğrulaması yeniden beklenir.
        delivery.IsQrVerified = false;
        delivery.QrVerifiedAt = null;

        await _context.SaveChangesAsync();

        await _notificationService.CreateAsync(delivery.ReceiverUserId, new CreateNotificationDto
        {
            Title = "Ürün kutuya bırakıldı",
            Message = $"{delivery.ShareListing.StockItem.Product.Name} teslim noktasına bırakıldı. QR doğrulama ile teslim alabilirsin.",
            Type = NotificationType.DeliveryDroppedOff,
            TargetUrl = "/Delivery/Incoming"
        });

        var updatedDelivery = await GetDeliveryEntityByIdAsync(delivery.Id);

        return updatedDelivery == null ? null : MapToDto(updatedDelivery);
    }

    public async Task<DeliveryDto?> ScanBoxQrAsync(string receiverUserId, ScanDeliveryBoxDto model)
    {
        // Aynı QR kutusunda birden fazla teslimat olabilir.
        // Bu yüzden doğrulama sadece QR koduna göre değil,
        // DeliveryId + ReceiverUserId + QR değeri birlikte kontrol edilerek yapılır.

        if (model.DeliveryId <= 0 || string.IsNullOrWhiteSpace(model.QrCodeValue))
            return null;

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
                x.Id == model.DeliveryId &&
                x.ReceiverUserId == receiverUserId &&
                x.Status == DeliveryStatus.DroppedOff);

        if (delivery == null)
            return null;

        // Teslimata atanmış aktif QR kutusu olmalıdır.
        if (delivery.DeliveryBox == null || !delivery.DeliveryBox.IsActive)
            return null;

        var expectedQrCode = delivery.DeliveryBox.QrCodeValue?.Trim();
        var enteredQrCode = model.QrCodeValue.Trim();

        // Girilen QR değeri, bu teslimata atanmış kutunun QR değeriyle eşleşmelidir.
        if (!string.Equals(expectedQrCode, enteredQrCode, StringComparison.OrdinalIgnoreCase))
            return null;

        // Süresi dolmuş teslimatlar geçersiz kabul edilir.
        if (delivery.ExpiresAt < DateTime.Now)
        {
            delivery.Status = DeliveryStatus.Expired;
            delivery.UpdatedAt = DateTime.Now;

            if (delivery.ShareListing != null)
            {
                delivery.ShareListing.Status = ShareListingStatus.Expired;
                delivery.ShareListing.UpdatedAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();

            return null;
        }

        // QR doğruysa sadece bu teslimat doğrulanmış kabul edilir.
        delivery.IsQrVerified = true;
        delivery.QrVerifiedAt = DateTime.Now;
        delivery.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        var verifiedDelivery = await GetDeliveryEntityByIdAsync(delivery.Id);

        return verifiedDelivery == null ? null : MapToDto(verifiedDelivery);
    }
    public async Task<DeliveryDto?> CompleteDeliveryAsync(string receiverUserId, int deliveryId)
    {
        // Tamamlama işleminden önce süresi geçmiş DroppedOff teslimatlar Expired yapılır.
        await ExpireOverdueDroppedOffDeliveriesAsync(receiverUserId);

        var delivery = await GetDeliveryEntityByIdAsync(deliveryId);

        if (delivery == null)
            return null;

        // Sadece teslimatın alıcısı teslimatı tamamlayabilir.
        if (delivery.ReceiverUserId != receiverUserId)
            return null;

        // Ürün teslim kutusuna bırakılmış olmalıdır.
        if (delivery.Status != DeliveryStatus.DroppedOff)
            return null;

        // Alıcı teslimatı tamamlamadan önce QR kodu doğrulamış olmalıdır.
        if (!delivery.IsQrVerified)
            return null;

        if (delivery.ExpiresAt < DateTime.Now)
        {
            delivery.Status = DeliveryStatus.Expired;
            delivery.UpdatedAt = DateTime.Now;

            if (delivery.ShareListing != null)
            {
                delivery.ShareListing.Status = ShareListingStatus.Expired;
                delivery.ShareListing.UpdatedAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();
            return null;
        }

        delivery.Status = DeliveryStatus.Delivered;
        delivery.PickedUpAt = DateTime.Now;
        delivery.DeliveredAt = DateTime.Now;
        delivery.UpdatedAt = DateTime.Now;

        delivery.ShareListing.Status = ShareListingStatus.Delivered;
        delivery.ShareListing.UpdatedAt = DateTime.Now;

        delivery.ShareListing.StockItem.Status = StockItemStatus.Shared;
        delivery.ShareListing.StockItem.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        await _ecoPointService.AddPointAsync(
            delivery.DonorUserId,
            10,
            EcoPointActionType.DeliveryCompletedAsDonor,
            "Ürününü başarıyla paylaşarak gıda israfını azaltmaya katkı sağladın.",
            delivery.Id);

        await _ecoPointService.AddPointAsync(
            delivery.ReceiverUserId,
            3,
            EcoPointActionType.DeliveryCompletedAsReceiver,
            "Paylaşılan ürünü teslim alarak gıda israfını azaltmaya katkı sağladın.",
            delivery.Id);

        await _notificationService.CreateAsync(delivery.DonorUserId, new CreateNotificationDto
        {
            Title = "Teslimat tamamlandı",
            Message = $"{delivery.ShareListing.StockItem.Product.Name} teslimatı tamamlandı. +10 Eco Puan kazandın.",
            Type = NotificationType.DeliveryCompleted,
            TargetUrl = "/Delivery/Completed"
        });

        var completedDelivery = await GetDeliveryEntityByIdAsync(delivery.Id);

        return completedDelivery == null ? null : MapToDto(completedDelivery);
    }

    private async Task ExpireOverdueDroppedOffDeliveriesAsync(string userId)
    {
        var now = DateTime.Now;

        var overdueDeliveries = await _context.Deliveries
            .Include(x => x.ShareListing)
            .Where(x =>
                (x.DonorUserId == userId || x.ReceiverUserId == userId) &&
                x.Status == DeliveryStatus.DroppedOff &&
                x.ExpiresAt < now)
            .ToListAsync();

        if (!overdueDeliveries.Any())
            return;

        foreach (var delivery in overdueDeliveries)
        {
            delivery.Status = DeliveryStatus.Expired;
            delivery.UpdatedAt = now;

            if (delivery.ShareListing != null)
            {
                delivery.ShareListing.Status = ShareListingStatus.Expired;
                delivery.ShareListing.UpdatedAt = now;
            }
        }

        await _context.SaveChangesAsync();
    }

    public async Task<List<DeliveryDto>> GetMyDonatedDeliveriesAsync(string donorUserId)
    {
        await ExpireOverdueDroppedOffDeliveriesAsync(donorUserId);

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
        await ExpireOverdueDroppedOffDeliveriesAsync(receiverUserId);

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
            DropOffImageUrl = delivery.DropOffImageUrl,
            IsQrVerified = delivery.IsQrVerified,
            QrVerifiedAt = delivery.QrVerifiedAt
        };
    }
}
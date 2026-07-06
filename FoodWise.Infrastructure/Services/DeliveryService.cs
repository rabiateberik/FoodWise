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
using System;

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

    // Onaylanmış paylaşım talebinden yeni bir teslimat kaydı oluşturur.
    // Teslimat oluşturulurken ilgili teslim noktasındaki uygun QR kutusu seçilir.
    public async Task<DeliveryDto?> CreateDeliveryAsync(string donorUserId, int shareRequestId)
    {
        // Paylaşım talebi, ilan, stok ürünü, ürün, birim ve teslim noktası bilgileriyle birlikte alınır.
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

        // Teslimatı sadece paylaşım ilanının sahibi oluşturabilir.
        if (request.ShareListing.DonorUserId != donorUserId)
            return null;

        // Talep onaylanmamışsa teslimat süreci başlatılmaz.
        if (request.Status != ShareRequestStatus.Approved)
            return null;

        // Aynı talep için daha önce teslimat oluşturulduysa yeni kayıt açmak yerine mevcut teslimat döndürülür.
        var existingDelivery = await GetDeliveryEntityByRequestIdAsync(shareRequestId);

        if (existingDelivery != null)
            return MapToDto(existingDelivery);

        // İlgili teslim noktasındaki aktif QR kutuları arasından en uygun olan seçilir.
        // Birden fazla kutu varsa aktif teslimat sayısı en az olan kutu tercih edilir.
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

        // Yeni teslimat kaydı hazırlanır.
        // QrToken sistem içi takip kodudur; kullanıcı QR doğrulamasında kutunun QR değerini okutur.
        var delivery = new Delivery
        {
            ShareListingId = request.ShareListingId,
            ShareRequestId = request.Id,
            DonorUserId = request.ShareListing.DonorUserId,
            ReceiverUserId = request.RequesterUserId,
            DeliveryPointId = request.ShareListing.DeliveryPointId,
            DeliveryBoxId = availableBox.Id,
            QrToken = Guid.NewGuid().ToString("N"),

            Status = DeliveryStatus.QrGenerated,
            CreatedAt = DateTime.Now,
            ExpiresAt = request.ShareListing.PickupEndTime,

            // Teslimat oluşturulduğunda alıcı henüz QR doğrulaması yapmamıştır.
            IsQrVerified = false,
            QrVerifiedAt = null
        };

        // İlan durumu da teslimat sürecine geçtiği için güncellenir.
        request.ShareListing.Status = ShareListingStatus.QrGenerated;
        request.ShareListing.UpdatedAt = DateTime.Now;

        await _context.Deliveries.AddAsync(delivery);
        await _context.SaveChangesAsync();

        // Teslimat oluşturulduğunda alıcıya bildirim gönderilir.
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

    // Bağışçının ürünü teslimat kutusuna bıraktığını işaretler.
    public async Task<DeliveryDto?> MarkAsDroppedOffAsync(string donorUserId, int deliveryId, DropOffDeliveryDto model)
    {
        var delivery = await GetDeliveryEntityByIdAsync(deliveryId);

        if (delivery == null)
            return null;

        // Sadece teslimatın bağışçısı ürünü kutuya bıraktığını işaretleyebilir.
        if (delivery.DonorUserId != donorUserId)
            return null;

        // Teslimat henüz tamamlanmamış ve bırakılabilir durumda olmalıdır.
        if (delivery.Status != DeliveryStatus.QrGenerated && delivery.Status != DeliveryStatus.Pending)
            return null;

        // Teslimat süresi dolduysa teslimat ve ilan durumu expired yapılır.
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

        // Ürün kutuya bırakıldı olarak güncellenir.
        delivery.Status = DeliveryStatus.DroppedOff;
        delivery.DroppedOffAt = DateTime.Now;
        delivery.DropOffImageUrl = model.DropOffImageUrl;
        delivery.UpdatedAt = DateTime.Now;

        // Ürün kutuya bırakıldıktan sonra alıcının QR doğrulaması beklenir.
        delivery.IsQrVerified = false;
        delivery.QrVerifiedAt = null;

        await _context.SaveChangesAsync();

        // Alıcıya ürünün kutuya bırakıldığı bilgisi bildirim olarak gönderilir.
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

    // Alıcının teslimat kutusu üzerindeki QR kodu okutmasını doğrular.
    public async Task<DeliveryDto?> ScanBoxQrAsync(string receiverUserId, ScanDeliveryBoxDto model)
    {
        // Aynı QR kutusunda birden fazla teslimat olabilir.
        // Bu yüzden sadece QR değeri değil; DeliveryId, alıcı kullanıcı ve QR değeri birlikte kontrol edilir.
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

        // Teslimata atanmış aktif bir kutu olmalıdır.
        if (delivery.DeliveryBox == null || !delivery.DeliveryBox.IsActive)
            return null;

        var expectedQrCode = delivery.DeliveryBox.QrCodeValue?.Trim();
        var enteredQrCode = model.QrCodeValue.Trim();

        // Kullanıcının okuttuğu QR değeri, teslimata atanmış kutunun QR değeriyle eşleşmelidir.
        if (!string.Equals(expectedQrCode, enteredQrCode, StringComparison.OrdinalIgnoreCase))
            return null;

        // Süresi dolmuş teslimatlar QR doğrulamasından geçemez.
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

    // Alıcının QR doğrulaması yapılmış teslimatı tamamlamasını sağlar.

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

        var now = DateTime.Now;

        delivery.Status = DeliveryStatus.Delivered;
        delivery.PickedUpAt = now;
        delivery.DeliveredAt = now;
        delivery.UpdatedAt = now;

        delivery.ShareListing.Status = ShareListingStatus.Delivered;
        delivery.ShareListing.UpdatedAt = now;

        delivery.ShareListing.StockItem.Status = StockItemStatus.Shared;
        delivery.ShareListing.StockItem.UpdatedAt = now;

        // Ürünü teslim alan kullanıcının ihtiyaç puanı artırılır.
        // Puan 100'ü geçmesin diye üst sınır uygulanır.
        var receiver = await _context.Users
            .FirstOrDefaultAsync(x => x.Id == delivery.ReceiverUserId && x.IsActive);

        if (receiver != null)
        {
            receiver.NeedScore = Math.Min(100, receiver.NeedScore + 5);
            receiver.UpdatedAt = now;
        }

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


    // Süresi geçmiş ve hâlâ kutuda bekliyor görünen teslimatları expired durumuna çeker.
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

    // Kullanıcının bağışçı olduğu teslimatları listeler.
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

    // Kullanıcının alıcı olduğu teslimatları listeler.
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

    // Teslimat detayını ilişkili ilan, ürün, birim, teslim noktası ve kutu bilgileriyle birlikte getirir.
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

    // Paylaşım talebi Id değerine göre teslimat kaydını getirir.
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

    // Delivery entity'sini API tarafında döndürülecek DeliveryDto yapısına dönüştürür.
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


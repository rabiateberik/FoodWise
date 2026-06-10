using FoodWise.Domain.Common;
using FoodWise.Domain.Enums;

namespace FoodWise.Domain.Entities;

public class Delivery : BaseEntity
{
    public int ShareListingId { get; set; }

    public int ShareRequestId { get; set; }

    public string DonorUserId { get; set; } = null!;

    public string ReceiverUserId { get; set; } = null!;

    public int DeliveryPointId { get; set; }

    public string QrToken { get; set; } = null!;

    public DeliveryStatus Status { get; set; } = DeliveryStatus.Pending;

    public DateTime? DroppedOffAt { get; set; }

    public DateTime? DeliveredAt { get; set; }

    public DateTime ExpiresAt { get; set; }

    public ShareListing ShareListing { get; set; } = null!;

    public ShareRequest ShareRequest { get; set; } = null!;

    public DeliveryPoint DeliveryPoint { get; set; } = null!;

    // Teslimatın atanacağı kutu/bölme bilgisidir.
    public int? DeliveryBoxId { get; set; }

    // Teslimatın bağlı olduğu kutu bilgisidir.
    public DeliveryBox? DeliveryBox { get; set; }

    // Alıcı ürünü kutudan teslim aldığında dolacak tarih alanıdır.
    public DateTime? PickedUpAt { get; set; }

    // Ürün sahibi ürünü kutuya bıraktığında opsiyonel fotoğraf yüklerse tutulur.
    public string? DropOffImageUrl { get; set; }

    // Alıcı QR kodu doğru şekilde doğruladığında true olur.
    // Teslim alma işlemi yapılmadan önce bu alan kontrol edilir.
    public bool IsQrVerified { get; set; } = false;

    // QR kodun başarıyla doğrulandığı tarih bilgisidir.
    public DateTime? QrVerifiedAt { get; set; }
}
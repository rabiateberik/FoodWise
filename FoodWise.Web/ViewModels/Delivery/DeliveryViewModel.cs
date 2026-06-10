// Bu ViewModel, API'den gelen teslimat bilgilerini Web arayüzünde göstermek için kullanılır.
// Bağışlanan ve teslim alınacak ürünlerin kutu, QR, durum ve teslimat bilgileri bu model üzerinden ekrana basılır.
// QR doğrulama bilgisi sayesinde alıcı, ürünü teslim almadan önce kutu QR değerini doğrulamak zorundadır.

namespace FoodWise.Web.ViewModels.Delivery;

public class DeliveryViewModel
{
    public int Id { get; set; }

    public int ShareListingId { get; set; }

    public int ShareRequestId { get; set; }

    public string DonorUserId { get; set; } = string.Empty;

    public string ReceiverUserId { get; set; } = string.Empty;

    public int DeliveryPointId { get; set; }

    public string DeliveryPointName { get; set; } = string.Empty;

    public int? DeliveryBoxId { get; set; }

    public string? BoxCode { get; set; }

    public string? BoxQrCodeValue { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public decimal Quantity { get; set; }

    public string UnitName { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTime? DroppedOffAt { get; set; }

    public DateTime? PickedUpAt { get; set; }

    public DateTime? DeliveredAt { get; set; }

    public DateTime ExpiresAt { get; set; }

    public string? DropOffImageUrl { get; set; }

    // Alıcı QR kodu başarıyla doğruladıysa true olur.
    public bool IsQrVerified { get; set; }

    // QR kodun doğrulandığı tarih bilgisidir.
    public DateTime? QrVerifiedAt { get; set; }

    public string QuantityText => $"{Quantity:0.##} {UnitName}";

    public bool IsDroppedOff =>
        string.Equals(Status, "DroppedOff", StringComparison.OrdinalIgnoreCase);

    public bool IsQrGenerated =>
        string.Equals(Status, "QrGenerated", StringComparison.OrdinalIgnoreCase);

    public bool IsPending =>
        string.Equals(Status, "Pending", StringComparison.OrdinalIgnoreCase);

    public bool IsCompleted =>
        string.Equals(Status, "Delivered", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(Status, "Completed", StringComparison.OrdinalIgnoreCase);

    public bool IsExpired =>
        string.Equals(Status, "Expired", StringComparison.OrdinalIgnoreCase);

    public bool CanCompleteAfterQr =>
        IsDroppedOff && IsQrVerified;

    public string QrVerificationText =>
        IsQrVerified
            ? "QR doğrulandı"
            : "QR doğrulaması bekleniyor";
}
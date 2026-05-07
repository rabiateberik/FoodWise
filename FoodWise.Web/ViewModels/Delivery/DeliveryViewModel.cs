// Bu ViewModel, API'den gelen teslimat bilgilerini Web arayüzünde göstermek için kullanılır.
// Bağışlanan ve teslim alınacak ürünlerin kutu, QR, durum ve teslimat bilgileri bu model üzerinden ekrana basılır.

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
}
// Alıcı kullanıcının teslimat kartından QR doğrulama yaparken göndereceği bilgileri temsil eder.
// Aynı QR kutusunda birden fazla teslimat olabileceği için sadece QR değeri yeterli değildir.
// DeliveryId ile hangi teslimatın doğrulanacağı netleştirilir.

namespace FoodWise.Application.DTOs.Delivery;

public class ScanDeliveryBoxDto
{
    public int DeliveryId { get; set; }

    public string QrCodeValue { get; set; } = null!;
}
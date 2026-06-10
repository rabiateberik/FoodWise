// Bu ViewModel, alıcının belirli bir teslimat için kutu QR kod değerini girmesi için kullanılır.
// Aynı QR kutusunda birden fazla teslimat olabileceği için DeliveryId ile hangi teslimatın doğrulanacağı belirtilir.

using System.ComponentModel.DataAnnotations;

namespace FoodWise.Web.ViewModels.Delivery;

public class ScanDeliveryBoxViewModel
{
    [Required(ErrorMessage = "Teslimat bilgisi bulunamadı.")]
    public int DeliveryId { get; set; }

    [Required(ErrorMessage = "QR kod değeri zorunludur.")]
    public string QrCodeValue { get; set; } = string.Empty;
}
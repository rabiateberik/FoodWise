// Bu ViewModel, alıcının teslim kutusundaki QR kod değerini girmesi için kullanılır.

using System.ComponentModel.DataAnnotations;

namespace FoodWise.Web.ViewModels.Delivery;

public class ScanDeliveryBoxViewModel
{
    [Required(ErrorMessage = "QR kod değeri zorunludur.")]
    public string QrCodeValue { get; set; } = string.Empty;
}
// CreateAdminDeliveryBoxViewModel, admin panelinden yeni teslim kutusu eklemek için kullanılır.
// DeliveryPointOptions sadece formdaki teslim noktası dropdown'u için kullanılır ve API'ye gönderilmez.

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace FoodWise.Web.ViewModels.Admin;

public class CreateAdminDeliveryBoxViewModel
{
    [Required(ErrorMessage = "Teslimat noktası seçimi zorunludur.")]
    public int DeliveryPointId { get; set; }

    [Required(ErrorMessage = "Kutu kodu zorunludur.")]
    public string BoxCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "QR kod değeri zorunludur.")]
    public string QrCodeValue { get; set; } = string.Empty;

    public string? Description { get; set; }

    [JsonIgnore]
    public List<AdminDeliveryPointViewModel> DeliveryPointOptions { get; set; } = new();
}
// CreateAdminDeliveryBoxDto, admin panelinden yeni teslim kutusu eklemek için kullanılır.

using System.ComponentModel.DataAnnotations;

namespace FoodWise.Application.DTOs.Admin;

public class CreateAdminDeliveryBoxDto
{
    [Required(ErrorMessage = "Teslimat noktası seçimi zorunludur.")]
    public int DeliveryPointId { get; set; }

    [Required(ErrorMessage = "Kutu kodu zorunludur.")]
    [MaxLength(50, ErrorMessage = "Kutu kodu en fazla 50 karakter olabilir.")]
    public string BoxCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "QR kod değeri zorunludur.")]
    [MaxLength(200, ErrorMessage = "QR kod değeri en fazla 200 karakter olabilir.")]
    public string QrCodeValue { get; set; } = string.Empty;

    [MaxLength(300, ErrorMessage = "Açıklama en fazla 300 karakter olabilir.")]
    public string? Description { get; set; }
}
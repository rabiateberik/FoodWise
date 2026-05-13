// UpdateAdminDeliveryPointDto, admin panelinden teslimat noktası bilgilerini güncellemek için kullanılır.

using System.ComponentModel.DataAnnotations;

namespace FoodWise.Application.DTOs.Admin;

public class UpdateAdminDeliveryPointDto
{
    [Required(ErrorMessage = "Teslimat noktası adı zorunludur.")]
    [MaxLength(150, ErrorMessage = "Teslimat noktası adı en fazla 150 karakter olabilir.")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500, ErrorMessage = "Açıklama en fazla 500 karakter olabilir.")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Şehir bilgisi zorunludur.")]
    [MaxLength(100, ErrorMessage = "Şehir bilgisi en fazla 100 karakter olabilir.")]
    public string City { get; set; } = string.Empty;

    [Required(ErrorMessage = "İlçe bilgisi zorunludur.")]
    [MaxLength(100, ErrorMessage = "İlçe bilgisi en fazla 100 karakter olabilir.")]
    public string District { get; set; } = string.Empty;

    [MaxLength(100, ErrorMessage = "Mahalle bilgisi en fazla 100 karakter olabilir.")]
    public string? Neighborhood { get; set; }

    public double? Latitude { get; set; }

    public double? Longitude { get; set; }

    [MaxLength(100, ErrorMessage = "Çalışma saatleri en fazla 100 karakter olabilir.")]
    public string? WorkingHours { get; set; }

    [MaxLength(100, ErrorMessage = "Saklama tipi en fazla 100 karakter olabilir.")]
    public string? StorageType { get; set; }
}
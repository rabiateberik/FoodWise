// UpdateAdminDeliveryPointViewModel, admin panelinden teslimat noktası bilgilerini güncellemek için kullanılır.

using System.ComponentModel.DataAnnotations;

namespace FoodWise.Web.ViewModels.Admin;

public class UpdateAdminDeliveryPointViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Teslimat noktası adı zorunludur.")]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required(ErrorMessage = "Şehir bilgisi zorunludur.")]
    public string City { get; set; } = string.Empty;

    [Required(ErrorMessage = "İlçe bilgisi zorunludur.")]
    public string District { get; set; } = string.Empty;

    public string? Neighborhood { get; set; }

    public double? Latitude { get; set; }

    public double? Longitude { get; set; }

    public string? WorkingHours { get; set; }

    public string? StorageType { get; set; }
}
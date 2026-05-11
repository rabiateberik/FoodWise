// Bu ViewModel, kullanıcının profil ve konum bilgilerini güncellemek için kullanılır.

using System.ComponentModel.DataAnnotations;

namespace FoodWise.Web.ViewModels.Profile;

public class UpdateProfileViewModel
{
    [Required(ErrorMessage = "Ad soyad alanı zorunludur.")]
    [StringLength(100, ErrorMessage = "Ad soyad en fazla 100 karakter olabilir.")]
    public string FullName { get; set; } = string.Empty;

    [StringLength(50, ErrorMessage = "Şehir en fazla 50 karakter olabilir.")]
    public string? City { get; set; }

    [StringLength(50, ErrorMessage = "İlçe en fazla 50 karakter olabilir.")]
    public string? District { get; set; }

    [StringLength(100, ErrorMessage = "Mahalle en fazla 100 karakter olabilir.")]
    public string? Neighborhood { get; set; }
}
// Bu ViewModel, FoodWise.Web kayıt ekranından alınan kullanıcı bilgilerini taşır.
// Formdan gelen bilgiler AuthController aracılığıyla FoodWise.API üzerindeki register endpointine gönderilir.
// Kullanıcıdan açık adres alınmaz; şehir, ilçe ve mahalle bilgisi kayıt için yeterlidir.

using System.ComponentModel.DataAnnotations;

namespace FoodWise.Web.ViewModels.Auth;

public class RegisterViewModel
{
    [Required(ErrorMessage = "Ad soyad alanı zorunludur.")]
    [StringLength(100, ErrorMessage = "Ad soyad en fazla 100 karakter olabilir.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email alanı zorunludur.")]
    [EmailAddress(ErrorMessage = "Geçerli bir email adresi giriniz.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Şehir alanı zorunludur.")]
    [StringLength(100, ErrorMessage = "Şehir bilgisi en fazla 100 karakter olabilir.")]
    public string City { get; set; } = string.Empty;

    [Required(ErrorMessage = "İlçe alanı zorunludur.")]
    [StringLength(100, ErrorMessage = "İlçe bilgisi en fazla 100 karakter olabilir.")]
    public string District { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "Mahalle bilgisi en fazla 100 karakter olabilir.")]
    public string? Neighborhood { get; set; }

    [Required(ErrorMessage = "Şifre alanı zorunludur.")]
    [DataType(DataType.Password)]
    [MinLength(6, ErrorMessage = "Şifre en az 6 karakter olmalıdır.")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Şifre tekrar alanı zorunludur.")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Şifreler eşleşmiyor.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
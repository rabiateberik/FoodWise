// Bu ViewModel, FoodWise.Web giriş ekranından alınan email ve şifre bilgilerini taşır.
// Kullanıcının login formunda girdiği veriler AuthController üzerinden API'ye gönderilecektir.

using System.ComponentModel.DataAnnotations;

namespace FoodWise.Web.ViewModels.Auth;

public class LoginViewModel
{
    [Required(ErrorMessage = "Email alanı zorunludur.")]
    [EmailAddress(ErrorMessage = "Geçerli bir email adresi giriniz.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Şifre alanı zorunludur.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
}
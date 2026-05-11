// Bu ViewModel, kullanıcının şifresini güvenli şekilde değiştirmek için kullanılır.

using System.ComponentModel.DataAnnotations;

namespace FoodWise.Web.ViewModels.Profile;

public class ChangePasswordViewModel
{
    [Required(ErrorMessage = "Mevcut şifre zorunludur.")]
    [DataType(DataType.Password)]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Yeni şifre zorunludur.")]
    [MinLength(6, ErrorMessage = "Yeni şifre en az 6 karakter olmalıdır.")]
    [DataType(DataType.Password)]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Yeni şifre tekrarı zorunludur.")]
    [DataType(DataType.Password)]
    [Compare(nameof(NewPassword), ErrorMessage = "Yeni şifreler eşleşmiyor.")]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}
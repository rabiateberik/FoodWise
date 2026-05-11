// Bu ViewModel, kullanıcının hesabını pasif hale getirme isteğini taşır.

using System.ComponentModel.DataAnnotations;

namespace FoodWise.Web.ViewModels.Profile;

public class DeleteAccountViewModel
{
    [Required(ErrorMessage = "Mevcut şifre zorunludur.")]
    [DataType(DataType.Password)]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Onay metni zorunludur.")]
    public string ConfirmText { get; set; } = string.Empty;
}
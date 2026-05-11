// Kullanıcının hesabını pasif hale getirmek için kullanılan DTO modelidir.

using System.ComponentModel.DataAnnotations;

namespace FoodWise.Application.DTOs.Profile;

public class DeleteAccountDto
{
    [Required(ErrorMessage = "Mevcut şifre zorunludur.")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Onay metni zorunludur.")]
    public string ConfirmText { get; set; } = string.Empty;
}
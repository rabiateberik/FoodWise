// Bu DTO, FoodWise.API tarafında kullanıcı kayıt isteğinden gelen verileri taşır.
// Kullanıcının temel kimlik bilgileriyle birlikte şehir, ilçe ve mahalle bilgileri alınır.
// Sistem açık adres bilgisi almaz; kullanıcı gizliliği için yalnızca bölgesel konum bilgisi tutulur.

using System.ComponentModel.DataAnnotations;

namespace FoodWise.Application.DTOs.Auth;

public class RegisterDto
{
    [Required(ErrorMessage = "Ad soyad alanı zorunludur.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email alanı zorunludur.")]
    [EmailAddress(ErrorMessage = "Geçerli bir email adresi giriniz.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Şifre alanı zorunludur.")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Şehir alanı zorunludur.")]
    public string City { get; set; } = string.Empty;

    [Required(ErrorMessage = "İlçe alanı zorunludur.")]
    public string District { get; set; } = string.Empty;

    public string? Neighborhood { get; set; }
}
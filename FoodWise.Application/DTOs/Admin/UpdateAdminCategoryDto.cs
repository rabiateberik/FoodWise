// UpdateAdminCategoryDto, admin panelinden kategori bilgilerini güncellemek için kullanılır.

using System.ComponentModel.DataAnnotations;

namespace FoodWise.Application.DTOs.Admin;

public class UpdateAdminCategoryDto
{
    [Required(ErrorMessage = "Kategori adı zorunludur.")]
    [MaxLength(100, ErrorMessage = "Kategori adı en fazla 100 karakter olabilir.")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500, ErrorMessage = "Açıklama en fazla 500 karakter olabilir.")]
    public string? Description { get; set; }
}
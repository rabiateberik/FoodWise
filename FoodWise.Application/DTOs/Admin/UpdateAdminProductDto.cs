// UpdateAdminProductDto, admin panelinden ürün bilgilerini güncellemek için kullanılır.

using System.ComponentModel.DataAnnotations;

namespace FoodWise.Application.DTOs.Admin;

public class UpdateAdminProductDto
{
    [Required(ErrorMessage = "Kategori seçimi zorunludur.")]
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "Ürün adı zorunludur.")]
    [MaxLength(150, ErrorMessage = "Ürün adı en fazla 150 karakter olabilir.")]
    public string Name { get; set; } = string.Empty;

    [Range(1, 3650, ErrorMessage = "Varsayılan raf ömrü 1 ile 3650 gün arasında olmalıdır.")]
    public int DefaultShelfLifeDays { get; set; }

    [Range(1, 3650, ErrorMessage = "Açıldıktan sonraki raf ömrü 1 ile 3650 gün arasında olmalıdır.")]
    public int? OpenedShelfLifeDays { get; set; }

    [Range(0, 9999, ErrorMessage = "Karbon faktörü 0 veya daha büyük olmalıdır.")]
    public decimal CarbonFactor { get; set; }

    public bool IsSensitiveFood { get; set; }

    public bool IsApproved { get; set; }
}
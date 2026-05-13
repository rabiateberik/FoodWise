// CreateAdminProductViewModel, admin panelinden yeni ürün eklemek için kullanılır.
// CategoryOptions sadece formda kategori dropdown'u göstermek için kullanılır ve API'ye gönderilmez.

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace FoodWise.Web.ViewModels.Admin;

public class CreateAdminProductViewModel
{
    [Required(ErrorMessage = "Kategori seçimi zorunludur.")]
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "Ürün adı zorunludur.")]
    public string Name { get; set; } = string.Empty;

    [Range(1, 3650, ErrorMessage = "Varsayılan raf ömrü 1 ile 3650 gün arasında olmalıdır.")]
    public int DefaultShelfLifeDays { get; set; } = 7;

    public int? OpenedShelfLifeDays { get; set; }

    [Range(0, 9999, ErrorMessage = "Karbon faktörü 0 veya daha büyük olmalıdır.")]
    public decimal CarbonFactor { get; set; } = 1;

    public bool IsSensitiveFood { get; set; }

    public bool IsApproved { get; set; } = true;

    [JsonIgnore]
    public List<AdminCategoryViewModel> CategoryOptions { get; set; } = new();
}
// EditStockItemViewModel, Web MVC tarafında stok ürünü düzenleme formu için kullanılır.
// Mevcut stok bilgileri bu model ile forma doldurulur ve güncelleme işlemi API'ye gönderilir.

using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace FoodWise.Web.ViewModels.Stock;

public class EditStockItemViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Ürün seçimi zorunludur.")]
    [Display(Name = "Ürün")]
    public int ProductId { get; set; }

    [Required(ErrorMessage = "Birim seçimi zorunludur.")]
    [Display(Name = "Birim")]
    public int UnitId { get; set; }

    [Required(ErrorMessage = "Miktar zorunludur.")]
    [Range(0.01, 999999, ErrorMessage = "Miktar 0'dan büyük olmalıdır.")]
    [Display(Name = "Miktar")]
    public decimal Quantity { get; set; }

    [Required(ErrorMessage = "Son kullanma tarihi zorunludur.")]
    [DataType(DataType.Date)]
    [Display(Name = "Son Kullanma Tarihi")]
    public DateTime ExpirationDate { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Açılma Tarihi")]
    public DateTime? OpenedDate { get; set; }

    [Required(ErrorMessage = "Saklama koşulu zorunludur.")]
    [Display(Name = "Saklama Koşulu")]
    public string StorageCondition { get; set; } = string.Empty;

    [Display(Name = "Not")]
    public string? Note { get; set; }

    // Dropdown listeleri form ekranında ürün, birim ve saklama koşulu seçeneklerini göstermek için kullanılır.
    public List<SelectListItem> Products { get; set; } = new();

    public List<SelectListItem> Units { get; set; } = new();

    public List<SelectListItem> StorageConditions { get; set; } = new();
}
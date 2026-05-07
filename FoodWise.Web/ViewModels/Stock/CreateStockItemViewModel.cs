// Bu ViewModel, Web arayüzünden yeni stok ürünü eklemek için kullanılır.
// Formdan alınan bilgiler Stock API'ye gönderilir.

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FoodWise.Web.ViewModels.Stock;

public class CreateStockItemViewModel
{
    [Required(ErrorMessage = "Ürün seçimi zorunludur.")]
    [Display(Name = "Ürün")]
    public int ProductId { get; set; }

    [Required(ErrorMessage = "Birim seçimi zorunludur.")]
    [Display(Name = "Birim")]
    public int UnitId { get; set; }

    [Required(ErrorMessage = "Miktar alanı zorunludur.")]
    [Range(0.01, 9999, ErrorMessage = "Miktar 0'dan büyük olmalıdır.")]
    [Display(Name = "Miktar")]
    public decimal Quantity { get; set; }

    [Required(ErrorMessage = "Son kullanma tarihi zorunludur.")]
    [DataType(DataType.Date)]
    [Display(Name = "Son Kullanma Tarihi")]
    public DateTime ExpirationDate { get; set; } = DateTime.Today.AddDays(3);

    [DataType(DataType.Date)]
    [Display(Name = "Açılma Tarihi")]
    public DateTime? OpenedDate { get; set; }

    [Required(ErrorMessage = "Saklama koşulu zorunludur.")]
    [Display(Name = "Saklama Koşulu")]
    public int StorageCondition { get; set; } = 2;

    [StringLength(500, ErrorMessage = "Not en fazla 500 karakter olabilir.")]
    [Display(Name = "Not")]
    public string? Note { get; set; }

    public List<SelectListItem> Products { get; set; } = new();
    public List<SelectListItem> Units { get; set; } = new();
    public List<SelectListItem> StorageConditions { get; set; } = new();
}
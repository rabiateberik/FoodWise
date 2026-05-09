// EditStockItemViewModel, Web MVC tarafında stok ürünü düzenleme formu için kullanılır.
// Ürün adı input olarak gösterilir; ürün sistemde yoksa API tarafında yeni ürün oluşturulabilir.

using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace FoodWise.Web.ViewModels.Stock;

public class EditStockItemViewModel
{
    public int Id { get; set; }

    // Mevcut ürün seçili geldiyse ProductId tutulabilir.
    // Kullanıcı ürün adını değiştirse bile asıl kontrol API tarafında ProductName üzerinden yapılır.
    public int? ProductId { get; set; }

    [Required(ErrorMessage = "Ürün adı zorunludur.")]
    [StringLength(150, ErrorMessage = "Ürün adı en fazla 150 karakter olabilir.")]
    [Display(Name = "Ürün Adı")]
    public string ProductName { get; set; } = string.Empty;

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

    [StringLength(500, ErrorMessage = "Not en fazla 500 karakter olabilir.")]
    [Display(Name = "Not")]
    public string? Note { get; set; }

    public List<SelectListItem> Products { get; set; } = new();

    public List<SelectListItem> Units { get; set; } = new();

    public List<SelectListItem> StorageConditions { get; set; } = new();
}
using FoodWise.Domain.Common;

namespace FoodWise.Domain.Entities;

public class Product : BaseEntity
{
    public int CategoryId { get; set; }

    public string Name { get; set; } = null!;

    public int DefaultShelfLifeDays { get; set; }

    public int? OpenedShelfLifeDays { get; set; }

    public decimal CarbonFactor { get; set; }

    public bool IsSensitiveFood { get; set; }

    // Ürün sistem tarafından seed data ile mi eklendi, yoksa kullanıcı tarafından mı oluşturuldu?
    public bool IsSystemDefined { get; set; } = true;

    // Admin paneli gelene kadar kullanıcıların eklediği ürünler otomatik onaylı kabul edilir.
    // İleride admin onay süreci eklenirse kullanıcı ürünleri false başlayabilir.
    public bool IsApproved { get; set; } = true;

    // Ürün aktif/pasif yönetimi için kullanılır.
    // Admin panelinde ürün pasife alınmak istenirse bu alan kullanılabilir.
    public bool IsActive { get; set; } = true;

    // Ürün kullanıcı tarafından eklendiyse, ekleyen kullanıcının Identity UserId bilgisi tutulur.
    // ApplicationUser Infrastructure katmanında olduğu için burada navigation property eklenmedi.
    public string? CreatedByUserId { get; set; }

    public Category Category { get; set; } = null!;

    public ICollection<StockItem> StockItems { get; set; } = new List<StockItem>();

    public ICollection<RecipeIngredient> RecipeIngredients { get; set; } = new List<RecipeIngredient>();
}
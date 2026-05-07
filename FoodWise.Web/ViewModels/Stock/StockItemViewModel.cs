// Bu ViewModel, API'den gelen stok ürünlerini Web arayüzünde göstermek için kullanılır.
// Kullanıcının stok listesi, risk seviyesi ve öneri bilgileri bu model üzerinden ekrana basılır.

namespace FoodWise.Web.ViewModels.Stock;

public class StockItemViewModel
{
    public int Id { get; set; }

    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;

    public int UnitId { get; set; }
    public string UnitName { get; set; } = string.Empty;

    public decimal Quantity { get; set; }

    public DateTime ExpirationDate { get; set; }
    public DateTime? OpenedDate { get; set; }

    public string StorageCondition { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;

    public int? RiskScore { get; set; }
    public string? RiskLevel { get; set; }
    public string? RecommendationText { get; set; }

    public string? Note { get; set; }
}
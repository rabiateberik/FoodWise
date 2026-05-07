using FoodWise.Domain.Common;
using FoodWise.Domain.Enums;

namespace FoodWise.Domain.Entities;

public class StockItem : BaseEntity
{
    public string UserId { get; set; } = null!;

    public int ProductId { get; set; }

    public int UnitId { get; set; }

    public decimal Quantity { get; set; }

    public DateTime ExpirationDate { get; set; }

    public DateTime? OpenedDate { get; set; }

    public StorageCondition StorageCondition { get; set; }

    public StockItemStatus Status { get; set; } = StockItemStatus.Active;

    public string? ImageUrl { get; set; }

    public string? Note { get; set; }

    public Product Product { get; set; } = null!;

    public Unit Unit { get; set; } = null!;

    public ICollection<WasteRiskPrediction> WasteRiskPredictions { get; set; } = new List<WasteRiskPrediction>();
    public ICollection<RecipeRecommendation> RecipeRecommendations { get; set; } = new List<RecipeRecommendation>();

    public ICollection<ShareListing> ShareListings { get; set; } = new List<ShareListing>();
}
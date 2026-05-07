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

    public Category Category { get; set; } = null!;

    public ICollection<StockItem> StockItems { get; set; } = new List<StockItem>();
    public ICollection<RecipeIngredient> RecipeIngredients { get; set; } = new List<RecipeIngredient>();
}
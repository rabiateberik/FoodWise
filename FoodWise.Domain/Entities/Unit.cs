using FoodWise.Domain.Common;

namespace FoodWise.Domain.Entities;

public class Unit : BaseEntity
{
    public string Name { get; set; } = null!;

    public string ShortName { get; set; } = null!;

    public ICollection<StockItem> StockItems { get; set; } = new List<StockItem>();
    public ICollection<RecipeIngredient> RecipeIngredients { get; set; } = new List<RecipeIngredient>();
}
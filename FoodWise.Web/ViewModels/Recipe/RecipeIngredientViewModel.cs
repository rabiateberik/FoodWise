// Bu ViewModel, tarif içinde kullanılan malzemeleri Web arayüzünde göstermek için kullanılır.

namespace FoodWise.Web.ViewModels.Recipe;

public class RecipeIngredientViewModel
{
    public int ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public decimal? Quantity { get; set; }

    public string? UnitName { get; set; }

    public bool IsRequired { get; set; }
}
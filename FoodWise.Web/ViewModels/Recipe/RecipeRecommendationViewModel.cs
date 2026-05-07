// Bu ViewModel, API'den gelen tarif önerilerini Web arayüzünde göstermek için kullanılır.
// Riskli stok ürünleri için önerilen tarifler bu model üzerinden ekrana basılır.

namespace FoodWise.Web.ViewModels.Recipe;

public class RecipeRecommendationViewModel
{
    public int RecipeId { get; set; }

    public string RecipeName { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string Instructions { get; set; } = string.Empty;

    public int PreparationTimeMinutes { get; set; }

    public string? ImageUrl { get; set; }

    public int MatchScore { get; set; }

    public string RecommendationReason { get; set; } = string.Empty;

    public List<RecipeIngredientViewModel> Ingredients { get; set; } = new();
}
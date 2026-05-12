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

    // Dataset'ten gelen tarif malzemelerini metin olarak göstermek için kullanılır.
    public string? IngredientsText { get; set; }

    // Kullanıcının stoklarıyla eşleşen malzemeler.
    public List<string> MatchedIngredients { get; set; } = new();

    // Tarifte olup kullanıcının stoklarında bulunmayan malzemeler.
    public List<string> MissingIngredients { get; set; } = new();

    // Eşleşen malzeme sayısı.
    public int MatchedIngredientCount { get; set; }

    // Tarifin toplam malzeme sayısı.
    public int TotalIngredientCount { get; set; }

    // Eski local RecipeIngredient yapısı için korunur.
    // Dataset tariflerinde bu liste boş gelebilir.
    public List<RecipeIngredientViewModel> Ingredients { get; set; } = new();
}
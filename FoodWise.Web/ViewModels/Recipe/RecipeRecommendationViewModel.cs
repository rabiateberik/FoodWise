// Bu ViewModel, API'den gelen tarif önerilerini Web arayüzünde göstermek için kullanılır.
// Riskli stok ürünleri ve genel stok önerileri için tarif kartları bu model üzerinden ekrana basılır.
// Kullanıcı etkileşimleri için RecipeId ve MatchScore bilgileri kullanılır.

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

    // Web arayüzünde eşleşme oranı gösterilirken kullanılır.
    public string MatchScoreText => $"%{MatchScore}";

    // Tarif kartında eşleşen malzeme alanının gösterilip gösterilmeyeceğini belirler.
    public bool HasMatchedIngredients => MatchedIngredients.Any();

    // Tarif kartında eksik malzeme alanının gösterilip gösterilmeyeceğini belirler.
    public bool HasMissingIngredients => MissingIngredients.Any();

    // Dataset malzeme metni veya eski ingredient listesi var mı kontrol eder.
    public bool HasIngredientInfo =>
        !string.IsNullOrWhiteSpace(IngredientsText) || Ingredients.Any();

    // Öneri sebebi boşsa arayüzde daha düzgün bir metin göstermek için kullanılır.
    public string SafeRecommendationReason =>
        string.IsNullOrWhiteSpace(RecommendationReason)
            ? "Bu tarif, stok durumun ve önceki tarif etkileşimlerin dikkate alınarak önerildi."
            : RecommendationReason;
}
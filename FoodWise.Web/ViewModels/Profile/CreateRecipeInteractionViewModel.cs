// CreateRecipeInteractionViewModel, kullanıcının tariflerle olan etkileşimini API'ye göndermek için kullanılır.
// Beğenme, kaydetme, yaptım, beğenmeme ve görüntüleme işlemleri bu model ile taşınır.

namespace FoodWise.Web.ViewModels.Recipe;

public class CreateRecipeInteractionViewModel
{
    public int RecipeId { get; set; }

    public int InteractionType { get; set; }

    public int? StockItemId { get; set; }

    public int? RecommendationScore { get; set; }
}
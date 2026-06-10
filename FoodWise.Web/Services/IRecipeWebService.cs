// Bu interface, FoodWise.Web projesinin Recipe API ile haberleşmesi için gereken metotları tanımlar.
// Controller doğrudan HttpClient kullanmaz; tarif listeleme, öneri ve kullanıcı etkileşim işlemleri bu servis üzerinden yapılır.

using FoodWise.Web.ViewModels.Recipe;

namespace FoodWise.Web.Services;

public interface IRecipeWebService
{
    Task<List<RecipeRecommendationViewModel>> GetAllRecipesAsync(string token);

    Task<List<RecipeRecommendationViewModel>> GetRecommendationsByStockItemAsync(int stockItemId, string token);

    Task<List<RecipeRecommendationViewModel>> GetGeneralRecommendationsAsync(string token);

    Task<List<RecipeRecommendationViewModel>> GetSavedRecipesAsync(string token);

    Task<List<RecipeRecommendationViewModel>> GetCookedRecipesAsync(string token);

    Task<bool> CreateRecipeInteractionAsync(CreateRecipeInteractionViewModel model, string token);
}
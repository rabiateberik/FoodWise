// Bu interface, FoodWise.Web projesinin Recipe API ile haberleşmesi için gereken metotları tanımlar.
// Controller doğrudan HttpClient kullanmaz; tarif işlemleri bu servis üzerinden yapılır.

using FoodWise.Web.ViewModels.Recipe;

namespace FoodWise.Web.Services;

public interface IRecipeWebService
{
    Task<List<RecipeRecommendationViewModel>> GetAllRecipesAsync(string token);

    Task<List<RecipeRecommendationViewModel>> GetRecommendationsByStockItemAsync(int stockItemId, string token);
    Task<List<RecipeRecommendationViewModel>> GetGeneralRecommendationsAsync(string token);
}
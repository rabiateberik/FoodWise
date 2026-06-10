using FoodWise.Application.DTOs.Recipe;
using FoodWise.Domain.Enums;

namespace FoodWise.Application.Interfaces;

// Tarif öneri işlemlerinin sözleşmesidir.
// Controller bu interface üzerinden servis katmanına erişir.
public interface IRecipeService
{
    Task<List<RecipeRecommendationDto>> GetRecommendationsByStockItemAsync(string userId, int stockItemId);

    Task<List<RecipeRecommendationDto>> GetRecipesByProductAsync(int productId);

    Task<List<RecipeRecommendationDto>> GetAllRecipesAsync();

    Task<List<RecipeRecommendationDto>> GetGeneralRecommendationsAsync(string userId);

    Task<bool> CreateRecipeInteractionAsync(string userId, CreateRecipeInteractionDto dto);
    Task<List<RecipeAiTrainingDataDto>> GetRecipeAiTrainingDataAsync();
    Task<List<RecipeRecommendationDto>> GetRecipesByInteractionTypeAsync(
    string userId,
    RecipeInteractionType interactionType

);

}
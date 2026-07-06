
using FoodWise.Application.DTOs.Recipe;
using FoodWise.Domain.Enums;

namespace FoodWise.Application.Interfaces;

// Tarif öneri işlemlerinin servis katmanında hangi metotlarla yapılacağını tanımlar.
public interface IRecipeService
{
    Task<List<RecipeRecommendationDto>> GetRecommendationsByStockItemAsync(string userId, int stockItemId);

  
    Task<List<RecipeRecommendationDto>> GetRecipesByProductAsync(int productId);

 
    Task<List<RecipeRecommendationDto>> GetAllRecipesAsync();


    Task<List<RecipeRecommendationDto>> GetGeneralRecommendationsAsync(string userId);


    Task<bool> CreateRecipeInteractionAsync(string userId, CreateRecipeInteractionDto dto);

    // AI/ML modeli için tarif etkileşim eğitim verisini getirir.
    Task<List<RecipeAiTrainingDataDto>> GetRecipeAiTrainingDataAsync();

    Task<List<RecipeRecommendationDto>> GetRecipesByInteractionTypeAsync(
        string userId,
        RecipeInteractionType interactionType);
}


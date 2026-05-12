using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Tarif öneri işlemlerinin sözleşmesidir.
// Controller bu interface üzerinden servis katmanına erişir.
using FoodWise.Application.DTOs.Recipe;

namespace FoodWise.Application.Interfaces;

public interface IRecipeService
{
    Task<List<RecipeRecommendationDto>> GetRecommendationsByStockItemAsync(string userId, int stockItemId);

    Task<List<RecipeRecommendationDto>> GetRecipesByProductAsync(int productId);

    Task<List<RecipeRecommendationDto>> GetAllRecipesAsync();
    Task<List<RecipeRecommendationDto>> GetGeneralRecommendationsAsync(string userId);
}

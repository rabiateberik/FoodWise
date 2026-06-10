// IMlRecipeRecommendationService, FoodWise API'nin Python tabanlı ML tarif öneri servisiyle haberleşmesini sağlar.

using FoodWise.Application.DTOs.RecipeRecommendation;

namespace FoodWise.Application.Interfaces;

public interface IMlRecipeRecommendationService
{
    Task<MlRecipeScorePredictionResponseDto?> PredictRecipeScoreAsync(
        MlRecipeScorePredictionRequestDto request);
}
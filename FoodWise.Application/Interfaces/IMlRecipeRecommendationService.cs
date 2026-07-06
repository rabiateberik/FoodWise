

using FoodWise.Application.DTOs.RecipeRecommendation;

namespace FoodWise.Application.Interfaces;

// Python tabanlı ML tarif öneri servisiyle haberleşecek metotları tanımlar.
public interface IMlRecipeRecommendationService
{
    // Tek bir tarif için ML servisinden öneri skoru alır.
    Task<MlRecipeScorePredictionResponseDto?> PredictRecipeScoreAsync(
        MlRecipeScorePredictionRequestDto request);

    // Birden fazla tarif için ML servisinden toplu öneri skoru alır.
    Task<List<MlRecipeScoreBatchPredictionItemResponseDto>> PredictRecipeScoresBatchAsync(
        List<MlRecipeScorePredictionRequestDto> requests);
}


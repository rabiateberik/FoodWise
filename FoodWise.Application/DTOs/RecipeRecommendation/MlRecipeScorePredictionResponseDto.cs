// MlRecipeScorePredictionResponseDto, Python ML servisinden dönen tarif öneri skoru sonucunu temsil eder.

namespace FoodWise.Application.DTOs.RecipeRecommendation;

public class MlRecipeScorePredictionResponseDto
{
    public double RecommendationScore { get; set; }

    public string RecommendationLabel { get; set; } = string.Empty;
}
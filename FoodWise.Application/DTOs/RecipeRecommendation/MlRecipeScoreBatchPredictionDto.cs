
namespace FoodWise.Application.DTOs.RecipeRecommendation;

public class MlRecipeScoreBatchPredictionRequestDto
{
    public List<MlRecipeScorePredictionRequestDto> Items { get; set; } = new();
}

public class MlRecipeScoreBatchPredictionItemResponseDto
{
    public string RecipeName { get; set; } = string.Empty;

    public double RecommendationScore { get; set; }

    public string RecommendationLabel { get; set; } = string.Empty;
}

public class MlRecipeScoreBatchPredictionResponseDto
{
    public List<MlRecipeScoreBatchPredictionItemResponseDto> Items { get; set; } = new();
}


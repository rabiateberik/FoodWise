using FoodWise.Domain.Enums;

namespace FoodWise.Application.DTOs.Recipe;

public class CreateRecipeInteractionDto
{
    public int RecipeId { get; set; }

    public RecipeInteractionType InteractionType { get; set; }

    public int? StockItemId { get; set; }

    public int? RecommendationScore { get; set; }
}
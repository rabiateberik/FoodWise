namespace FoodWise.Application.DTOs.Recipe;

public class RecipeAiTrainingDataDto
{
    public string UserId { get; set; } = null!;

    public int RecipeId { get; set; }

    public int InteractionType { get; set; }

    public float Label { get; set; }

    public int RecommendationScore { get; set; }

    public int PreparationTimeMinutes { get; set; }

    public int IngredientCount { get; set; }

    public bool HasStockContext { get; set; }

    public int UserLikedCount { get; set; }

    public int UserSavedCount { get; set; }

    public int UserCookedCount { get; set; }

    public int UserDislikedCount { get; set; }

    public int RecipeLikedCount { get; set; }

    public int RecipeSavedCount { get; set; }

    public int RecipeCookedCount { get; set; }

    public int RecipeDislikedCount { get; set; }
}
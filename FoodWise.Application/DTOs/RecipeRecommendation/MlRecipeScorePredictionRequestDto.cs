// MlRecipeScorePredictionRequestDto, ASP.NET Core API'nin Python ML servisine göndereceği tarif öneri skoru isteğini temsil eder.
// Bu model FastAPI tarafındaki /predict-recipe-score endpointinin beklediği alanlarla uyumludur.

namespace FoodWise.Application.DTOs.RecipeRecommendation;

public class MlRecipeScorePredictionRequestDto
{
    public string RecipeName { get; set; } = string.Empty;

    public string RecipeCategory { get; set; } = string.Empty;

    public string Difficulty { get; set; } = string.Empty;

    public int PreparationTimeMinutes { get; set; }

    public int TotalIngredientCount { get; set; }

    public int MatchedIngredientCount { get; set; }

    public int MissingIngredientCount { get; set; }

    public double MatchedIngredientRatio { get; set; }

    public int RiskyIngredientCount { get; set; }

    public int AverageDaysUntilExpiration { get; set; }

    public bool HasSensitiveIngredient { get; set; }

    public int UserLikedSimilarRecipes { get; set; }

    public int UserSavedSimilarRecipes { get; set; }

    public int UserCookedSimilarRecipes { get; set; }

    public int UserDislikedSimilarRecipes { get; set; }

    public int ViewedSimilarRecipes { get; set; }

    public string Season { get; set; } = string.Empty;
}
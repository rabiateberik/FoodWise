using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


// Kullanıcıya önerilecek tarif bilgilerini ve öneri sebebini taşır.
namespace FoodWise.Application.DTOs.Recipe;

public class RecipeRecommendationDto
{
    public int RecipeId { get; set; }

    public string RecipeName { get; set; } = null!;

    public string? Description { get; set; }

    public string Instructions { get; set; } = null!;

    public int PreparationTimeMinutes { get; set; }

    public string? ImageUrl { get; set; }

    public int MatchScore { get; set; }

    public string RecommendationReason { get; set; } = null!;

    public string? IngredientsText { get; set; }

    public List<string> MatchedIngredients { get; set; } = new();

    public List<string> MissingIngredients { get; set; } = new();

    public int MatchedIngredientCount { get; set; }

    public int TotalIngredientCount { get; set; }

    public List<RecipeIngredientDto> Ingredients { get; set; } = new();
}
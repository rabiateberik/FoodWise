using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FoodWise.Domain.Common;
using FoodWise.Domain.Enums;

namespace FoodWise.Domain.Entities;

public class Recipe : BaseEntity
{
    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string Instructions { get; set; } = null!;

    public int PreparationTimeMinutes { get; set; }

    public string? ImageUrl { get; set; }
    // Dataset'ten gelen ham malzeme metnini saklamak için kullanılır.
    public string? IngredientsText { get; set; }

    // Tarif öneri algoritmasında kullanılacak temizlenmiş malzeme metnidir.
    public string? NormalizedIngredientsText { get; set; }

    // Tarifin veri kaynağı veya linki varsa saklamak için kullanılır.
    public string? SourceUrl { get; set; }

    public RecipeSourceType SourceType { get; set; } = RecipeSourceType.Local;

    public string? ExternalApiId { get; set; }

    public ICollection<RecipeIngredient> RecipeIngredients { get; set; } = new List<RecipeIngredient>();

    public ICollection<RecipeRecommendation> RecipeRecommendations { get; set; } = new List<RecipeRecommendation>();
    public ICollection<UserRecipeInteraction> UserRecipeInteractions { get; set; } = new List<UserRecipeInteraction>();
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FoodWise.Domain.Common;

namespace FoodWise.Domain.Entities;

public class RecipeRecommendation : BaseEntity
{
    public string UserId { get; set; } = null!;

    public int StockItemId { get; set; }

    public int RecipeId { get; set; }

    public int MatchScore { get; set; }

    public string? RecommendationReason { get; set; }

    public DateTime RecommendedAt { get; set; } = DateTime.Now;

    public StockItem StockItem { get; set; } = null!;

    public Recipe Recipe { get; set; } = null!;
}
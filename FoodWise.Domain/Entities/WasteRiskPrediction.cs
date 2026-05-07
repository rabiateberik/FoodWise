using FoodWise.Domain.Common;
using FoodWise.Domain.Enums;

namespace FoodWise.Domain.Entities;

public class WasteRiskPrediction : BaseEntity
{
    public int StockItemId { get; set; }

    public int RiskScore { get; set; }

    public RiskLevel RiskLevel { get; set; }

    public DateTime? PredictedWasteDate { get; set; }

    public int DaysRemaining { get; set; }

    public RecommendationType RecommendationType { get; set; }

    public string? RecommendationText { get; set; }

    public DateTime CalculatedAt { get; set; } = DateTime.Now;

    public StockItem StockItem { get; set; } = null!;
}
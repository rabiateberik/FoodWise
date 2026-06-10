// MlRiskPredictionRequestDto, ASP.NET Core API'nin Python ML servisine göndereceği risk tahmin isteğini temsil eder.
// Bu model FastAPI tarafındaki /predict-risk endpointinin beklediği alanlarla uyumludur.

namespace FoodWise.Application.DTOs.RiskPrediction;

public class MlRiskPredictionRequestDto
{
    public string ProductName { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public string StorageCondition { get; set; } = string.Empty;

    public int DaysUntilExpiration { get; set; }

    public int DaysSinceOpened { get; set; }

    public bool IsOpened { get; set; }

    public bool IsSensitive { get; set; }

    public decimal Quantity { get; set; }

    public int PreviousWasteCount { get; set; }

    public int PreviousSharedCount { get; set; }

    public string Season { get; set; } = string.Empty;
}
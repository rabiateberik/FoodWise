// MlShareMatchPredictionRequestDto, ASP.NET Core API'nin Python ML servisine göndereceği
// akıllı paylaşım eşleştirme skoru isteğini temsil eder.
// Bu model FastAPI tarafındaki /predict-match-score endpointinin beklediği alanlarla uyumludur.

namespace FoodWise.Application.DTOs.ShareMatching;

public class MlShareMatchPredictionRequestDto
{
    public bool SameCity { get; set; }

    public bool SameDistrict { get; set; }

    public bool SameNeighborhood { get; set; }

    public int DistancePriority { get; set; }

    public int NeedScore { get; set; }

    public int ReliabilityScore { get; set; }

    public int CompletedDeliveryCount { get; set; }

    public int CancelledRequestCount { get; set; }

    public int PendingRequestCount { get; set; }

    public int PreviousSuccessfulRequests { get; set; }

    public string ProductRiskLevel { get; set; } = string.Empty;

    public int DaysUntilExpiration { get; set; }

    public bool IsSensitiveFood { get; set; }

    public int DonorPastShareCount { get; set; }

    public int RequesterPastReceiveCount { get; set; }

    public int RequestHour { get; set; }

    public string ProductCategory { get; set; } = string.Empty;
}
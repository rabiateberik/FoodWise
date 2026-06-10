// IMlRiskPredictionService, FoodWise API'nin Python tabanlı ML risk tahmin servisiyle haberleşmesini sağlar.
// Infrastructure katmanında HttpClient ile uygulanır.

using FoodWise.Application.DTOs.RiskPrediction;

namespace FoodWise.Application.Interfaces;

public interface IMlRiskPredictionService
{
    Task<MlRiskPredictionResponseDto?> PredictRiskAsync(MlRiskPredictionRequestDto request);
}
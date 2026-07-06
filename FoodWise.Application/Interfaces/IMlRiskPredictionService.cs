
using FoodWise.Application.DTOs.RiskPrediction;

namespace FoodWise.Application.Interfaces;

// Python tabanlı ML risk tahmin servisiyle haberleşecek metodu tanımlar.
// Gerçek HTTP bağlantısı Infrastructure katmanında uygulanır.
public interface IMlRiskPredictionService
{
    // Stok bilgilerine göre ML servisinden risk tahmini alır.
    Task<MlRiskPredictionResponseDto?> PredictRiskAsync(MlRiskPredictionRequestDto request);
}


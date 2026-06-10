// MlRiskPredictionResponseDto, Python ML servisinden dönen risk tahmin sonucunu temsil eder.
// RiskLabel ürünün tahmin edilen risk seviyesidir; Probabilities ise sınıf olasılıklarını taşır.

namespace FoodWise.Application.DTOs.RiskPrediction;

public class MlRiskPredictionResponseDto
{
    public string RiskLabel { get; set; } = string.Empty;

    public Dictionary<string, double> Probabilities { get; set; } = new();
}
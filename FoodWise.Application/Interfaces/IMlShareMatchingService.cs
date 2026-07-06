
using FoodWise.Application.DTOs.ShareMatching;

namespace FoodWise.Application.Interfaces;

// Python tabanlı ML paylaşım eşleştirme servisiyle haberleşecek metodu tanımlar.
public interface IMlShareMatchingService
{
    // Paylaşım talebi ve ilan bilgilerine göre ML servisinden eşleşme skoru alır.
    Task<MlShareMatchPredictionResponseDto?> PredictMatchScoreAsync(
        MlShareMatchPredictionRequestDto request);
}


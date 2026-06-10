// IMlShareMatchingService, FoodWise API'nin Python tabanlı ML paylaşım eşleştirme servisiyle haberleşmesini sağlar.

using FoodWise.Application.DTOs.ShareMatching;

namespace FoodWise.Application.Interfaces;

public interface IMlShareMatchingService
{
    Task<MlShareMatchPredictionResponseDto?> PredictMatchScoreAsync(
        MlShareMatchPredictionRequestDto request);
}
// MlShareMatchPredictionResponseDto, Python ML servisinden dönen akıllı eşleştirme sonucunu temsil eder.

namespace FoodWise.Application.DTOs.ShareMatching;

public class MlShareMatchPredictionResponseDto
{
    public double MatchScore { get; set; }

    public string MatchLabel { get; set; } = string.Empty;
}
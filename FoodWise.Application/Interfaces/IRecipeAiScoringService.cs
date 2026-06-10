// IRecipeAiScoringService, tarif önerilerine kişiselleştirilmiş AI skoru uygulamak için kullanılır.
// İlk aşamada kural tabanlı çalışır; ileride Python/ONNX modeli ile değiştirilebilir.

using FoodWise.Application.DTOs.Recipe;

namespace FoodWise.Application.Interfaces;

public interface IRecipeAiScoringService
{
    Task<List<RecipeRecommendationDto>> ApplyPersonalizedScoresAsync(
        string userId,
        List<RecipeRecommendationDto> recommendations
    );
}
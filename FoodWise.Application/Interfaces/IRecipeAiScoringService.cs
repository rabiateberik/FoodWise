
using FoodWise.Application.DTOs.Recipe;

namespace FoodWise.Application.Interfaces;

// Tarif önerilerine kişiselleştirilmiş skor uygulayacak metodu tanımlar.
// İlk aşamada kural tabanlı çalışır, ileride ML modeliyle genişletilebilir.
public interface IRecipeAiScoringService
{
    // Tarif önerilerini kullanıcının geçmiş etkileşimlerine göre yeniden skorlar.
    Task<List<RecipeRecommendationDto>> ApplyPersonalizedScoresAsync(
        string userId,
        List<RecipeRecommendationDto> recommendations
    );
}

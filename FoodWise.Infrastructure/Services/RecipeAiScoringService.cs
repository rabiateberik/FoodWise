
// RecipeAiScoringService, tarif önerilerini kullanıcının geçmiş tarif etkileşimlerine göre yeniden skorlar.
// Bu sınıf ileride Python/ONNX tabanlı model entegrasyonu için değiştirilmeden genişletilebilir.

using FoodWise.Application.DTOs.Recipe;
using FoodWise.Application.Interfaces;
using FoodWise.Domain.Enums;
using FoodWise.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FoodWise.Infrastructure.Services;

public class RecipeAiScoringService : IRecipeAiScoringService
{
    private readonly FoodWiseDbContext _context;

    public RecipeAiScoringService(FoodWiseDbContext context)
    {
        _context = context;
    }

    // Gelen tarif önerilerini kullanıcının geçmiş etkileşimlerine göre yeniden puanlar.
    // Kullanıcının daha önce beğendiği, kaydettiği veya yaptığı tarifler daha yüksek skor alabilir.
    public async Task<List<RecipeRecommendationDto>> ApplyPersonalizedScoresAsync(
        string userId,
        List<RecipeRecommendationDto> recommendations)
    {
        if (string.IsNullOrWhiteSpace(userId) || !recommendations.Any())
            return recommendations;

        // Sadece öneri listesinde bulunan tariflerin Id değerleri alınır.
        var recipeIds = recommendations
            .Select(x => x.RecipeId)
            .Distinct()
            .ToList();

        // Kullanıcının bu tariflerle ilgili daha önceki aktif etkileşimleri veritabanından çekilir.
        var userInteractions = await _context.UserRecipeInteractions
            .AsNoTracking()
            .Where(x =>
                x.IsActive &&
                x.UserId == userId &&
                recipeIds.Contains(x.RecipeId))
            .ToListAsync();

        foreach (var recommendation in recommendations)
        {
            // Her tarif için sadece o tarife ait kullanıcı etkileşimleri alınır.
            var recipeInteractions = userInteractions
                .Where(x => x.RecipeId == recommendation.RecipeId)
                .ToList();

            // Mevcut eşleşme skoru, kullanıcı davranışlarına göre yeniden hesaplanır.
            var personalizedScore = CalculatePersonalizedScore(
                recommendation.MatchScore,
                recipeInteractions
            );

            recommendation.MatchScore = personalizedScore;

            // Eğer kullanıcı bu tarifle daha önce etkileşime girdiyse öneri sebebine açıklama eklenir.
            if (recipeInteractions.Any())
            {
                recommendation.RecommendationReason =
                    $"{recommendation.RecommendationReason} Bu tarif, önceki tarif etkileşimlerine göre kişiselleştirilmiş skorla yeniden değerlendirildi.";
            }
        }

        // Skoru yüksek tarifler önce gösterilir.
        // Skor eşitse hazırlanma süresi kısa olan tarif öncelikli olur.
        return recommendations
            .OrderByDescending(x => x.MatchScore)
            .ThenBy(x => x.PreparationTimeMinutes)
            .ToList();
    }

    // Tarifin temel skorunu kullanıcı etkileşimlerine göre artırır veya azaltır.
    private static int CalculatePersonalizedScore(
        int baseScore,
        List<FoodWise.Domain.Entities.UserRecipeInteraction> interactions)
    {
        var score = baseScore;

        var viewedCount = interactions.Count(x =>
            x.InteractionType == RecipeInteractionType.Viewed);

        var hasLiked = interactions.Any(x =>
            x.InteractionType == RecipeInteractionType.Liked);

        var hasSaved = interactions.Any(x =>
            x.InteractionType == RecipeInteractionType.Saved);

        var hasCooked = interactions.Any(x =>
            x.InteractionType == RecipeInteractionType.Cooked);

        var hasDisliked = interactions.Any(x =>
            x.InteractionType == RecipeInteractionType.Disliked);

        // Görüntüleme küçük bir ilgi sinyali olarak değerlendirilir.
        // Çok fazla görüntüleme olsa bile skor artışı sınırlı tutulur.
        score += Math.Min(viewedCount * 2, 6);

        // Beğenme, kaydetme ve yapma işlemleri pozitif kullanıcı davranışı olarak skoru artırır.
        if (hasLiked)
            score += 6;

        if (hasSaved)
            score += 8;

        if (hasCooked)
            score += 10;

        // Kullanıcı tarifi beğenmediyse skor düşürülür.
        if (hasDisliked)
            score -= 18;

        // Skorun 0 ile 100 aralığında kalması sağlanır.
        return Math.Clamp(score, 0, 100);
    }
}


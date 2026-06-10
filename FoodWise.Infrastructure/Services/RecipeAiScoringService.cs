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

    public async Task<List<RecipeRecommendationDto>> ApplyPersonalizedScoresAsync(
        string userId,
        List<RecipeRecommendationDto> recommendations)
    {
        if (string.IsNullOrWhiteSpace(userId) || !recommendations.Any())
            return recommendations;

        var recipeIds = recommendations
            .Select(x => x.RecipeId)
            .Distinct()
            .ToList();

        var userInteractions = await _context.UserRecipeInteractions
            .AsNoTracking()
            .Where(x =>
                x.IsActive &&
                x.UserId == userId &&
                recipeIds.Contains(x.RecipeId))
            .ToListAsync();

        foreach (var recommendation in recommendations)
        {
            var recipeInteractions = userInteractions
                .Where(x => x.RecipeId == recommendation.RecipeId)
                .ToList();

            var personalizedScore = CalculatePersonalizedScore(
                recommendation.MatchScore,
                recipeInteractions
            );

            recommendation.MatchScore = personalizedScore;

            if (recipeInteractions.Any())
            {
                recommendation.RecommendationReason =
                    $"{recommendation.RecommendationReason} Bu tarif, önceki tarif etkileşimlerine göre kişiselleştirilmiş skorla yeniden değerlendirildi.";
            }
        }

        return recommendations
            .OrderByDescending(x => x.MatchScore)
            .ThenBy(x => x.PreparationTimeMinutes)
            .ToList();
    }

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
        score += Math.Min(viewedCount * 2, 6);

        if (hasLiked)
            score += 6;

        if (hasSaved)
            score += 8;

        if (hasCooked)
            score += 10;

        if (hasDisliked)
            score -= 18;

        return Math.Clamp(score, 0, 100);
    }
}
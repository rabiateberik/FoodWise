// RecipeService, kullanıcının stok ürünlerine göre tarif önerileri üretir.
// Tarif önerileri, normalize edilmiş malzeme metinleri, kullanıcı etkileşimleri ve ML skorları kullanılarak hesaplanır.

using FoodWise.Application.DTOs.Recipe;
using FoodWise.Application.DTOs.RecipeRecommendation;
using FoodWise.Application.Interfaces;
using FoodWise.Domain.Entities;
using FoodWise.Domain.Enums;
using FoodWise.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text.RegularExpressions;

namespace FoodWise.Infrastructure.Services;

public class RecipeService : IRecipeService
{
    private readonly FoodWiseDbContext _context;
    private readonly IRecipeAiScoringService _recipeAiScoringService;
    private readonly IMlRecipeRecommendationService _mlRecipeRecommendationService;

    public RecipeService(
        FoodWiseDbContext context,
        IRecipeAiScoringService recipeAiScoringService,
        IMlRecipeRecommendationService mlRecipeRecommendationService)
    {
        _context = context;
        _recipeAiScoringService = recipeAiScoringService;
        _mlRecipeRecommendationService = mlRecipeRecommendationService;
    }

    // Seçilen stok ürününü merkeze alarak tarif önerileri üretir.
    // Özellikle son kullanma tarihi yaklaşan veya riskli ürünleri değerlendirmek için kullanılır.
    public async Task<List<RecipeRecommendationDto>> GetRecommendationsByStockItemAsync(string userId, int stockItemId)
    {
        // Kullanıcının seçtiği aktif stok ürünü alınır.
        var selectedStockItem = await _context.StockItems
            .Include(x => x.Product)
            .FirstOrDefaultAsync(x =>
                x.Id == stockItemId &&
                x.UserId == userId &&
                x.IsActive &&
                x.Status == StockItemStatus.Active);

        if (selectedStockItem == null)
            return new List<RecipeRecommendationDto>();

        // Kullanıcının tüm stok ürünleri alınır.
        // Seçilen ürün dışında diğer stok ürünleri de tarif eşleşmesine dahil edilir.
        var userStockItems = await _context.StockItems
            .Include(x => x.Product)
            .Where(x =>
                x.UserId == userId &&
                x.IsActive &&
                x.Product != null)
            .ToListAsync();

        // Stoktaki ürün isimleri normalize edilerek eşleştirme için aday malzemelere çevrilir.
        var stockCandidates = userStockItems
            .Where(x => !string.IsNullOrWhiteSpace(x.Product.Name))
            .Select(x => new StockIngredientCandidate
            {
                ProductId = x.ProductId,
                ProductName = x.Product.Name,
                NormalizedName = NormalizeText(x.Product.Name),
                Tokens = GetMeaningfulTokens(x.Product.Name)
            })
            .Where(x => !string.IsNullOrWhiteSpace(x.NormalizedName))
            .GroupBy(x => x.NormalizedName)
            .Select(x => x.First())
            .ToList();

        // Seçilen ürün öneri hesaplamasında öncelikli ürün olarak işaretlenir.
        var selectedProductCandidate = new StockIngredientCandidate
        {
            ProductId = selectedStockItem.ProductId,
            ProductName = selectedStockItem.Product.Name,
            NormalizedName = NormalizeText(selectedStockItem.Product.Name),
            Tokens = GetMeaningfulTokens(selectedStockItem.Product.Name),
            IsRisky = true
        };

        // Normalize edilmiş malzeme metni olan aktif tarifler alınır.
        var recipes = await _context.Recipes
            .AsNoTracking()
            .Where(x =>
                x.IsActive &&
                !string.IsNullOrWhiteSpace(x.NormalizedIngredientsText))
            .ToListAsync();

        // Tarifler seçilen stok ürününe göre değerlendirilir ve en iyi 20 öneri alınır.
        var recommendations = recipes
            .Select(recipe => EvaluateRecipe(recipe, selectedProductCandidate, stockCandidates))
            .Where(result => result != null)
            .Select(result => result!)
            .OrderByDescending(x => x.MatchScore)
            .ThenBy(x => x.PreparationTimeMinutes)
            .Take(20)
            .ToList();

        // Kullanıcının geçmiş tarif etkileşimlerine göre skorlar kişiselleştirilir.
        recommendations = await _recipeAiScoringService.ApplyPersonalizedScoresAsync(
            userId,
            recommendations);

        // Python ML modeli üzerinden tarif skorları yeniden değerlendirilir.
        recommendations = await ApplyMlRecipeScoresAsync(
            userId,
            recommendations,
            userStockItems);

        recommendations = recommendations
            .OrderByDescending(x => x.MatchScore)
            .ThenBy(x => x.PreparationTimeMinutes)
            .Take(20)
            .ToList();

        // Üretilen öneriler geçmiş kayıt olarak saklanır.
        await SaveRecommendationHistoryAsync(userId, stockItemId, recommendations);

        return recommendations;
    }

    // Kullanıcının tüm stok ürünlerine göre genel tarif önerileri üretir.
    public async Task<List<RecipeRecommendationDto>> GetGeneralRecommendationsAsync(string userId)
    {
        // Kullanıcının stokları risk tahminleriyle birlikte alınır.
        var userStockItems = await _context.StockItems
            .Include(x => x.Product)
            .Include(x => x.WasteRiskPredictions)
            .Where(x =>
                x.UserId == userId &&
                x.IsActive &&
                x.Product != null)
            .ToListAsync();

        if (!userStockItems.Any())
            return new List<RecipeRecommendationDto>();

        // Stok ürünleri tarif malzemeleriyle karşılaştırılabilecek adaylara dönüştürülür.
        var stockCandidates = userStockItems
            .Where(x => !string.IsNullOrWhiteSpace(x.Product.Name))
            .Select(x => new StockIngredientCandidate
            {
                ProductId = x.ProductId,
                ProductName = x.Product.Name,
                NormalizedName = NormalizeText(x.Product.Name),
                Tokens = GetMeaningfulTokens(x.Product.Name),
                IsRisky = IsRiskyStockItem(x)
            })
            .Where(x => !string.IsNullOrWhiteSpace(x.NormalizedName))
            .GroupBy(x => x.NormalizedName)
            .Select(x => x.First())
            .ToList();

        if (!stockCandidates.Any())
            return new List<RecipeRecommendationDto>();

        var recipes = await _context.Recipes
            .AsNoTracking()
            .Where(x =>
                x.IsActive &&
                !string.IsNullOrWhiteSpace(x.NormalizedIngredientsText))
            .ToListAsync();

        // Tüm stoklara göre genel tarifler değerlendirilir.
        var recommendations = recipes
            .Select(recipe => EvaluateGeneralRecipe(recipe, stockCandidates))
            .Where(result => result != null)
            .Select(result => result!)
            .OrderByDescending(x => x.MatchScore)
            .ThenBy(x => x.PreparationTimeMinutes)
            .Take(30)
            .ToList();

        recommendations = await _recipeAiScoringService.ApplyPersonalizedScoresAsync(
            userId,
            recommendations);

        recommendations = await ApplyMlRecipeScoresAsync(
            userId,
            recommendations,
            userStockItems);

        return recommendations
            .OrderByDescending(x => x.MatchScore)
            .ThenBy(x => x.PreparationTimeMinutes)
            .Take(30)
            .ToList();
    }

    // Tarif önerilerine Python ML servisinden gelen skorları uygular.
    // ML servisi çalışmazsa mevcut kural tabanlı skorlar korunur.
    private async Task<List<RecipeRecommendationDto>> ApplyMlRecipeScoresAsync(
        string userId,
        List<RecipeRecommendationDto> recommendations,
        List<StockItem> userStockItems)
    {
        if (!recommendations.Any())
            return recommendations;

        // Kullanıcının tarif etkileşim geçmişi ML isteğinde özellik olarak kullanılır.
        var interactionSummary = await GetUserRecipeInteractionSummaryAsync(userId);

        // Her tarif önerisi, ML servisinin beklediği request DTO yapısına dönüştürülür.
        var mlRequests = recommendations
            .Select(recommendation => CreateMlRecipeScoreRequest(
                recommendation,
                userStockItems,
                interactionSummary))
            .ToList();

        // Tarifler Python FastAPI servisine toplu olarak gönderilir.
        var mlResults = await _mlRecipeRecommendationService.PredictRecipeScoresBatchAsync(mlRequests);

        if (mlResults == null || !mlResults.Any())
        {
            Console.WriteLine("Toplu tarif ML skoru alınamadı. Malzeme sınırı uygulanarak eski skorlar kullanılacak.");

            return ApplyIngredientScoreLimits(recommendations);
        }

        if (mlResults.Count != recommendations.Count)
        {
            Console.WriteLine($"Tarif ML sonucu sayısı eşleşmedi. Öneri: {recommendations.Count}, ML Sonuç: {mlResults.Count}");
        }

        var resultCount = Math.Min(recommendations.Count, mlResults.Count);

        for (var i = 0; i < resultCount; i++)
        {
            var recommendation = recommendations[i];
            var mlResult = mlResults[i];

            var currentScore = recommendation.MatchScore;
            var mlScore = (int)Math.Round(Math.Clamp(mlResult.RecommendationScore, 0, 100));

            // Son skor hem mevcut kural tabanlı skordan hem de ML skorundan etkilenir.
            // ML skoru daha ağırlıklı kullanılır.
            var finalScore = (int)Math.Round((currentScore * 0.35) + (mlScore * 0.65));

            recommendation.MatchScore = Math.Clamp(finalScore, 0, 100);

            Console.WriteLine($"Tarif ML skoru kullanıldı: {recommendation.RecipeName} - ML: {mlScore} - Final: {recommendation.MatchScore}");
        }

        return ApplyIngredientScoreLimits(recommendations);
    }

    // Eksik malzemesi olan tariflerin çok yüksek skor görünmesini engeller.
    private static List<RecipeRecommendationDto> ApplyIngredientScoreLimits(
        List<RecipeRecommendationDto> recommendations)
    {
        foreach (var recommendation in recommendations)
        {
            if (recommendation.TotalIngredientCount <= 0)
            {
                recommendation.MatchScore = Math.Clamp(recommendation.MatchScore, 0, 100);
                continue;
            }

            var totalIngredientCount = Math.Max(1, recommendation.TotalIngredientCount);
            var matchedIngredientCount = Math.Clamp(
                recommendation.MatchedIngredientCount,
                0,
                totalIngredientCount);

            var ingredientMatchRatio = matchedIngredientCount / (double)totalIngredientCount;

            var hasMissingIngredients =
                recommendation.MissingIngredients.Any() ||
                matchedIngredientCount < totalIngredientCount;

            var maxAllowedScore = 100;

            // Eksik malzeme varsa hiçbir tarif %100 görünmemeli.
            if (hasMissingIngredients)
                maxAllowedScore = 94;

            // Malzeme eşleşme oranına göre skor için üst sınır uygulanır.
            if (ingredientMatchRatio < 0.50)
                maxAllowedScore = Math.Min(maxAllowedScore, 75);
            else if (ingredientMatchRatio < 0.75)
                maxAllowedScore = Math.Min(maxAllowedScore, 85);
            else if (ingredientMatchRatio < 1.00)
                maxAllowedScore = Math.Min(maxAllowedScore, 94);

            recommendation.MatchScore = Math.Clamp(
                Math.Min(recommendation.MatchScore, maxAllowedScore),
                0,
                100);
        }

        return recommendations;
    }

    // Tarif önerisini Python ML modelinin beklediği request DTO yapısına dönüştürür.
    private MlRecipeScorePredictionRequestDto CreateMlRecipeScoreRequest(
        RecipeRecommendationDto recommendation,
        List<StockItem> userStockItems,
        RecipeInteractionSummary interactionSummary)
    {
        var matchedIngredientNames = recommendation.MatchedIngredients
            .Select(NormalizeText)
            .ToHashSet();

        // Öneride eşleşen malzemelerin kullanıcının hangi stok ürünlerine denk geldiği bulunur.
        var matchedStockItems = userStockItems
            .Where(x =>
                x.Product != null &&
                matchedIngredientNames.Contains(NormalizeText(x.Product.Name)))
            .ToList();

        var riskyIngredientCount = matchedStockItems.Count(IsRiskyStockItem);

        var averageDaysUntilExpiration = matchedStockItems.Any()
            ? (int)Math.Round(matchedStockItems.Average(x =>
                (x.ExpirationDate.Date - DateTime.Now.Date).Days))
            : 30;

        var hasSensitiveIngredient = matchedStockItems.Any(x =>
            x.Product != null &&
            x.Product.IsSensitiveFood);

        var totalIngredientCount = recommendation.TotalIngredientCount <= 0
            ? Math.Max(1, recommendation.MatchedIngredientCount + recommendation.MissingIngredients.Count)
            : recommendation.TotalIngredientCount;

        var matchedIngredientRatio = totalIngredientCount > 0
            ? recommendation.MatchedIngredientCount / (double)totalIngredientCount
            : 0;

        return new MlRecipeScorePredictionRequestDto
        {
            RecipeName = recommendation.RecipeName,
            RecipeCategory = InferRecipeCategory(recommendation.RecipeName),
            Difficulty = InferRecipeDifficulty(recommendation.PreparationTimeMinutes),
            PreparationTimeMinutes = recommendation.PreparationTimeMinutes,
            TotalIngredientCount = totalIngredientCount,
            MatchedIngredientCount = recommendation.MatchedIngredientCount,
            MissingIngredientCount = recommendation.MissingIngredients.Count,
            MatchedIngredientRatio = Math.Round(matchedIngredientRatio, 3),
            RiskyIngredientCount = riskyIngredientCount,
            AverageDaysUntilExpiration = averageDaysUntilExpiration,
            HasSensitiveIngredient = hasSensitiveIngredient,
            UserLikedSimilarRecipes = interactionSummary.LikedCount,
            UserSavedSimilarRecipes = interactionSummary.SavedCount,
            UserCookedSimilarRecipes = interactionSummary.CookedCount,
            UserDislikedSimilarRecipes = interactionSummary.DislikedCount,
            ViewedSimilarRecipes = interactionSummary.ViewedCount,
            Season = GetCurrentSeasonText()
        };
    }

    // Kullanıcının tarif etkileşim sayılarını ML modeli için özetler.
    private async Task<RecipeInteractionSummary> GetUserRecipeInteractionSummaryAsync(string userId)
    {
        var interactions = await _context.UserRecipeInteractions
            .AsNoTracking()
            .Where(x =>
                x.UserId == userId &&
                x.IsActive)
            .ToListAsync();

        return new RecipeInteractionSummary
        {
            LikedCount = interactions.Count(x => x.InteractionType == RecipeInteractionType.Liked),
            SavedCount = interactions.Count(x => x.InteractionType == RecipeInteractionType.Saved),
            CookedCount = interactions.Count(x => x.InteractionType == RecipeInteractionType.Cooked),
            DislikedCount = interactions.Count(x => x.InteractionType == RecipeInteractionType.Disliked),
            ViewedCount = interactions.Count(x => x.InteractionType == RecipeInteractionType.Viewed)
        };
    }

    // Hazırlanma süresine göre tarif zorluğunu tahmini olarak belirler.
    private static string InferRecipeDifficulty(int preparationTimeMinutes)
    {
        if (preparationTimeMinutes <= 20)
            return "Kolay";

        if (preparationTimeMinutes <= 40)
            return "Orta";

        return "Zor";
    }

    // Tarif adına göre kategori tahmini yapar.
    // Dataset içinde kategori yoksa ML isteği için yaklaşık kategori üretilir.
    private static string InferRecipeCategory(string recipeName)
    {
        var normalizedName = NormalizeText(recipeName);

        if (normalizedName.Contains("omlet") ||
            normalizedName.Contains("tost") ||
            normalizedName.Contains("menemen") ||
            normalizedName.Contains("yulaf"))
            return "Kahvaltı";

        if (normalizedName.Contains("corba"))
            return "Çorba";

        if (normalizedName.Contains("salata"))
            return "Salata";

        if (normalizedName.Contains("smoothie") ||
            normalizedName.Contains("sandvic") ||
            normalizedName.Contains("rulo") ||
            normalizedName.Contains("pizza"))
            return "Atıştırmalık";

        if (normalizedName.Contains("tatli") ||
            normalizedName.Contains("pankek") ||
            normalizedName.Contains("sutlac") ||
            normalizedName.Contains("meyve"))
            return "Tatlı";

        return "Ana Yemek";
    }

    // ML modeline mevsim bilgisini özellik olarak göndermek için mevcut mevsimi döndürür.
    private static string GetCurrentSeasonText()
    {
        var month = DateTime.Now.Month;

        return month switch
        {
            3 or 4 or 5 => "İlkbahar",
            6 or 7 or 8 => "Yaz",
            9 or 10 or 11 => "Sonbahar",
            _ => "Kış"
        };
    }

    // Belirli bir ürüne göre tarifleri listeler.
    public async Task<List<RecipeRecommendationDto>> GetRecipesByProductAsync(int productId)
    {
        var product = await _context.Products
            .FirstOrDefaultAsync(x => x.Id == productId && x.IsActive);

        if (product == null)
            return new List<RecipeRecommendationDto>();

        var productCandidate = new StockIngredientCandidate
        {
            ProductId = product.Id,
            ProductName = product.Name,
            NormalizedName = NormalizeText(product.Name),
            Tokens = GetMeaningfulTokens(product.Name)
        };

        var recipes = await _context.Recipes
            .AsNoTracking()
            .Where(x =>
                x.IsActive &&
                !string.IsNullOrWhiteSpace(x.NormalizedIngredientsText))
            .ToListAsync();

        return recipes
            .Select(recipe => EvaluateRecipe(
                recipe,
                productCandidate,
                new List<StockIngredientCandidate> { productCandidate }))
            .Where(result => result != null)
            .Select(result => result!)
            .OrderByDescending(x => x.MatchScore)
            .ThenBy(x => x.PreparationTimeMinutes)
            .Take(20)
            .ToList();
    }

    // Sistemdeki aktif tarifleri temel bilgilerle listeler.
    public async Task<List<RecipeRecommendationDto>> GetAllRecipesAsync()
    {
        var recipes = await _context.Recipes
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Take(100)
            .ToListAsync();

        return recipes
            .Select(MapToBasicRecipeDto)
            .ToList();
    }

    // Kullanıcının tarif etkileşimini kaydeder.
    // Beğenme, kaydetme, yaptım, beğenmedim ve görüntüleme işlemleri bu metotla tutulur.
    public async Task<bool> CreateRecipeInteractionAsync(string userId, CreateRecipeInteractionDto dto)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return false;

        if (!Enum.IsDefined(typeof(RecipeInteractionType), dto.InteractionType))
            return false;

        var recipeExists = await _context.Recipes
            .AnyAsync(x => x.Id == dto.RecipeId && x.IsActive);

        if (!recipeExists)
            return false;

        // Etkileşim stok ürünüyle ilişkiliyse stok ürününün kullanıcıya ait olduğu kontrol edilir.
        if (dto.StockItemId.HasValue)
        {
            var stockItemExists = await _context.StockItems
                .AnyAsync(x =>
                    x.Id == dto.StockItemId.Value &&
                    x.UserId == userId &&
                    x.IsActive);

            if (!stockItemExists)
                return false;
        }

        int? recommendationScore = dto.RecommendationScore.HasValue
            ? Math.Clamp(dto.RecommendationScore.Value, 0, 100)
            : null;

        // Görüntüleme dışındaki etkileşimlerde aynı türden kayıt varsa güncellenir.
        // Böylece aynı tarif için tekrar tekrar aynı beğeni/kaydetme kaydı oluşmaz.
        if (dto.InteractionType != RecipeInteractionType.Viewed)
        {
            var existingInteraction = await _context.UserRecipeInteractions
                .FirstOrDefaultAsync(x =>
                    x.IsActive &&
                    x.UserId == userId &&
                    x.RecipeId == dto.RecipeId &&
                    x.InteractionType == dto.InteractionType);

            if (existingInteraction != null)
            {
                existingInteraction.StockItemId = dto.StockItemId;
                existingInteraction.RecommendationScore = recommendationScore;
                existingInteraction.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();
                return true;
            }
        }

        var interaction = new UserRecipeInteraction
        {
            UserId = userId,
            RecipeId = dto.RecipeId,
            InteractionType = dto.InteractionType,
            StockItemId = dto.StockItemId,
            RecommendationScore = recommendationScore,
            CreatedAt = DateTime.Now,
            IsActive = true
        };

        await _context.UserRecipeInteractions.AddAsync(interaction);
        await _context.SaveChangesAsync();

        return true;
    }

    // Kullanıcının belirli bir etkileşim türüne sahip tariflerini listeler.
    public async Task<List<RecipeRecommendationDto>> GetRecipesByInteractionTypeAsync(
        string userId,
        RecipeInteractionType interactionType)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return new List<RecipeRecommendationDto>();

        if (!Enum.IsDefined(typeof(RecipeInteractionType), interactionType))
            return new List<RecipeRecommendationDto>();

        var interactions = await _context.UserRecipeInteractions
            .AsNoTracking()
            .Include(x => x.Recipe)
            .Where(x =>
                x.IsActive &&
                x.UserId == userId &&
                x.InteractionType == interactionType &&
                x.Recipe.IsActive)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        return interactions
            .GroupBy(x => x.RecipeId)
            .Select(group =>
            {
                // Aynı tarif için birden fazla kayıt varsa en son etkileşim dikkate alınır.
                var latestInteraction = group
                    .OrderByDescending(x => x.CreatedAt)
                    .First();

                var dto = MapToBasicRecipeDto(latestInteraction.Recipe);

                dto.MatchScore = latestInteraction.RecommendationScore ?? 0;
                dto.RecommendationReason = interactionType switch
                {
                    RecipeInteractionType.Saved => "Bu tarif daha önce kaydedildiği için listeleniyor.",
                    RecipeInteractionType.Cooked => "Bu tarif daha önce yapıldı olarak işaretlendiği için listeleniyor.",
                    RecipeInteractionType.Liked => "Bu tarif daha önce beğenildiği için listeleniyor.",
                    RecipeInteractionType.Disliked => "Bu tarif daha önce beğenilmedi olarak işaretlendiği için listeleniyor.",
                    RecipeInteractionType.Viewed => "Bu tarif daha önce görüntülendiği için listeleniyor.",
                    _ => "Kullanıcı tarif geçmişine göre listeleniyor."
                };

                return dto;
            })
            .ToList();
    }

    // Kullanıcı tarif etkileşimlerinden AI/ML eğitim verisi üretir.
    public async Task<List<RecipeAiTrainingDataDto>> GetRecipeAiTrainingDataAsync()
    {
        var interactions = await _context.UserRecipeInteractions
            .AsNoTracking()
            .Include(x => x.Recipe)
            .Where(x =>
                x.IsActive &&
                x.Recipe.IsActive)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync();

        if (!interactions.Any())
            return new List<RecipeAiTrainingDataDto>();

        var trainingData = new List<RecipeAiTrainingDataDto>();

        foreach (var interaction in interactions)
        {
            // Her etkileşim için sadece o etkileşimden önceki kullanıcı ve tarif geçmişi dikkate alınır.
            var previousUserInteractions = interactions
                .Where(x =>
                    x.UserId == interaction.UserId &&
                    x.CreatedAt < interaction.CreatedAt)
                .ToList();

            var previousRecipeInteractions = interactions
                .Where(x =>
                    x.RecipeId == interaction.RecipeId &&
                    x.CreatedAt < interaction.CreatedAt)
                .ToList();

            trainingData.Add(new RecipeAiTrainingDataDto
            {
                UserId = interaction.UserId,
                RecipeId = interaction.RecipeId,
                InteractionType = (int)interaction.InteractionType,
                Label = GetInteractionLabel(interaction.InteractionType),
                RecommendationScore = interaction.RecommendationScore ?? 0,
                PreparationTimeMinutes = interaction.Recipe.PreparationTimeMinutes,
                IngredientCount = GetRecipeIngredientCount(interaction.Recipe),
                HasStockContext = interaction.StockItemId.HasValue,

                UserLikedCount = previousUserInteractions.Count(x =>
                    x.InteractionType == RecipeInteractionType.Liked),

                UserSavedCount = previousUserInteractions.Count(x =>
                    x.InteractionType == RecipeInteractionType.Saved),

                UserCookedCount = previousUserInteractions.Count(x =>
                    x.InteractionType == RecipeInteractionType.Cooked),

                UserDislikedCount = previousUserInteractions.Count(x =>
                    x.InteractionType == RecipeInteractionType.Disliked),

                RecipeLikedCount = previousRecipeInteractions.Count(x =>
                    x.InteractionType == RecipeInteractionType.Liked),

                RecipeSavedCount = previousRecipeInteractions.Count(x =>
                    x.InteractionType == RecipeInteractionType.Saved),

                RecipeCookedCount = previousRecipeInteractions.Count(x =>
                    x.InteractionType == RecipeInteractionType.Cooked),

                RecipeDislikedCount = previousRecipeInteractions.Count(x =>
                    x.InteractionType == RecipeInteractionType.Disliked)
            });
        }

        return trainingData;
    }

    // Seçilen stok ürünü merkeze alınarak tek bir tarifin uygunluk skoru hesaplanır.
    private static RecipeRecommendationDto? EvaluateRecipe(
        Recipe recipe,
        StockIngredientCandidate selectedProduct,
        List<StockIngredientCandidate> userStockCandidates)
    {
        var normalizedIngredientsText = recipe.NormalizedIngredientsText ?? string.Empty;
        var recipeTokens = GetMeaningfulTokens(normalizedIngredientsText);

        if (!recipeTokens.Any())
            return null;

        // Tarif, seçilen ürünü içermiyorsa bu özel öneri listesine alınmaz.
        var selectedProductMatched = IsIngredientMatched(
            normalizedIngredientsText,
            recipeTokens,
            selectedProduct);

        if (!selectedProductMatched)
            return null;

        // Kullanıcının stokundaki hangi ürünlerin tarifle eşleştiği bulunur.
        var matchedStockIngredients = userStockCandidates
            .Where(stock => IsIngredientMatched(normalizedIngredientsText, recipeTokens, stock))
            .GroupBy(stock => stock.NormalizedName)
            .Select(group => group.First())
            .ToList();

        var totalIngredientCount = GetRecipeIngredientCount(recipe);
        var matchedIngredientCount = matchedStockIngredients.Count;

        var missingIngredients = GetMissingIngredients(recipe, userStockCandidates)
            .Take(8)
            .ToList();

        var coverageRatio = totalIngredientCount > 0
            ? matchedIngredientCount / (double)totalIngredientCount
            : 0;

        var coverageScore = (int)Math.Round(coverageRatio * 35);
        coverageScore = Math.Clamp(coverageScore, 0, 35);

        var selectedProductScore = 35;

        var otherMatchedIngredientCount = matchedStockIngredients
            .Count(x => !x.ProductName.Equals(
                selectedProduct.ProductName,
                StringComparison.CurrentCultureIgnoreCase));

        var stockMatchScore = Math.Min(25, otherMatchedIngredientCount * 7);
        var missingPenalty = Math.Min(15, missingIngredients.Count * 2);

        var simpleRecipeBonus = totalIngredientCount > 0 &&
                                totalIngredientCount <= 5 &&
                                coverageRatio >= 0.60
            ? 5
            : 0;

        var finalScore = selectedProductScore
                         + stockMatchScore
                         + coverageScore
                         + simpleRecipeBonus
                         - missingPenalty;

        // Sadece seçilen ürün eşleşmişse skor fazla yükselmesin diye sınır uygulanır.
        if (otherMatchedIngredientCount == 0)
            finalScore = Math.Min(finalScore, 65);

        // Eksik malzeme varsa tarif %100 görünmez.
        if (totalIngredientCount > 0 && matchedIngredientCount < totalIngredientCount)
            finalScore = Math.Min(finalScore, 94);

        finalScore = Math.Clamp(finalScore, 0, 100);

        return new RecipeRecommendationDto
        {
            RecipeId = recipe.Id,
            RecipeName = recipe.Name,
            Description = recipe.Description,
            Instructions = recipe.Instructions,
            PreparationTimeMinutes = recipe.PreparationTimeMinutes,
            ImageUrl = recipe.ImageUrl,
            MatchScore = finalScore,
            RecommendationReason = CreateRecommendationReason(
                selectedProduct.ProductName,
                matchedStockIngredients.Select(x => x.ProductName).ToList()),
            IngredientsText = recipe.IngredientsText,
            MatchedIngredients = matchedStockIngredients
                .Select(x => x.ProductName)
                .Distinct()
                .ToList(),
            MissingIngredients = missingIngredients,
            MatchedIngredientCount = matchedIngredientCount,
            TotalIngredientCount = totalIngredientCount,
            Ingredients = new List<RecipeIngredientDto>()
        };
    }

    // Etkileşim türünü ML eğitiminde kullanılacak sayısal etikete çevirir.
    private static float GetInteractionLabel(RecipeInteractionType interactionType)
    {
        return interactionType switch
        {
            RecipeInteractionType.Disliked => 0.0f,
            RecipeInteractionType.Viewed => 0.35f,
            RecipeInteractionType.Liked => 0.75f,
            RecipeInteractionType.Saved => 0.85f,
            RecipeInteractionType.Cooked => 1.0f,
            _ => 0.0f
        };
    }

    // Kullanıcının tüm stoklarına göre genel tarif skorunu hesaplar.
    private static RecipeRecommendationDto? EvaluateGeneralRecipe(
        Recipe recipe,
        List<StockIngredientCandidate> userStockCandidates)
    {
        var normalizedIngredientsText = recipe.NormalizedIngredientsText ?? string.Empty;
        var recipeTokens = GetMeaningfulTokens(normalizedIngredientsText);

        if (!recipeTokens.Any())
            return null;

        var matchedStockIngredients = userStockCandidates
            .Where(stock => IsIngredientMatched(normalizedIngredientsText, recipeTokens, stock))
            .GroupBy(stock => stock.NormalizedName)
            .Select(group => group.First())
            .ToList();

        if (!matchedStockIngredients.Any())
            return null;

        var riskyMatchedIngredients = matchedStockIngredients
            .Where(x => x.IsRisky)
            .ToList();

        var totalIngredientCount = GetRecipeIngredientCount(recipe);
        var matchedIngredientCount = matchedStockIngredients.Count;

        var missingIngredients = GetMissingIngredients(recipe, userStockCandidates)
            .Take(8)
            .ToList();

        var coverageRatio = totalIngredientCount > 0
            ? matchedIngredientCount / (double)totalIngredientCount
            : 0;

        var coverageScore = (int)Math.Round(coverageRatio * 40);
        coverageScore = Math.Clamp(coverageScore, 0, 40);

        var stockMatchScore = Math.Min(30, matchedIngredientCount * 7);
        var riskyBonus = Math.Min(20, riskyMatchedIngredients.Count * 10);
        var missingPenalty = Math.Min(18, missingIngredients.Count * 2);

        var simpleRecipeBonus = totalIngredientCount > 0 &&
                                totalIngredientCount <= 5 &&
                                coverageRatio >= 0.60
            ? 6
            : 0;

        var finalScore = stockMatchScore
                         + coverageScore
                         + riskyBonus
                         + simpleRecipeBonus
                         - missingPenalty;

        // Riskli ürün içermeyen tariflerin skoru belirli bir seviyede sınırlandırılır.
        if (!riskyMatchedIngredients.Any())
            finalScore = Math.Min(finalScore, 82);

        if (totalIngredientCount > 0 && matchedIngredientCount < totalIngredientCount)
            finalScore = Math.Min(finalScore, 94);

        finalScore = Math.Clamp(finalScore, 0, 100);

        // Çok düşük skorlu tarifler öneri listesine dahil edilmez.
        if (finalScore < 35)
            return null;

        return new RecipeRecommendationDto
        {
            RecipeId = recipe.Id,
            RecipeName = recipe.Name,
            Description = recipe.Description,
            Instructions = recipe.Instructions,
            PreparationTimeMinutes = recipe.PreparationTimeMinutes,
            ImageUrl = recipe.ImageUrl,
            MatchScore = finalScore,
            RecommendationReason = CreateGeneralRecommendationReason(
                matchedStockIngredients.Select(x => x.ProductName).ToList(),
                riskyMatchedIngredients.Select(x => x.ProductName).ToList()),
            IngredientsText = recipe.IngredientsText,
            MatchedIngredients = matchedStockIngredients
                .Select(x => x.ProductName)
                .Distinct()
                .ToList(),
            MissingIngredients = missingIngredients,
            MatchedIngredientCount = matchedIngredientCount,
            TotalIngredientCount = totalIngredientCount,
            Ingredients = new List<RecipeIngredientDto>()
        };
    }

    // Recipe entity'sini temel tarif DTO yapısına dönüştürür.
    private static RecipeRecommendationDto MapToBasicRecipeDto(Recipe recipe)
    {
        return new RecipeRecommendationDto
        {
            RecipeId = recipe.Id,
            RecipeName = recipe.Name,
            Description = recipe.Description,
            Instructions = recipe.Instructions,
            PreparationTimeMinutes = recipe.PreparationTimeMinutes,
            ImageUrl = recipe.ImageUrl,
            MatchScore = 0,
            RecommendationReason = "Bu tarif FoodWise tarif veri setinde yer almaktadır.",
            IngredientsText = recipe.IngredientsText,
            MatchedIngredients = new List<string>(),
            MissingIngredients = new List<string>(),
            MatchedIngredientCount = 0,
            TotalIngredientCount = GetRecipeIngredientCount(recipe),
            Ingredients = new List<RecipeIngredientDto>()
        };
    }

    // Stok ürününün tarif malzemeleri içinde geçip geçmediğini kontrol eder.
    private static bool IsIngredientMatched(
        string normalizedIngredientsText,
        HashSet<string> recipeTokens,
        StockIngredientCandidate stock)
    {
        if (string.IsNullOrWhiteSpace(stock.NormalizedName))
            return false;

        // Önce tam ifade eşleşmesi kontrol edilir.
        if (ContainsExactPhrase(normalizedIngredientsText, stock.NormalizedName))
            return true;

        if (!stock.Tokens.Any())
            return false;

        // Birden fazla kelimeli ürünlerde tüm tokenların tarif içinde geçmesi beklenir.
        if (stock.Tokens.Count > 1 && stock.Tokens.All(recipeTokens.Contains))
            return true;

        // Tek kelimeli ürünlerde token eşleşmesi yeterli kabul edilir.
        if (stock.Tokens.Count == 1 && recipeTokens.Contains(stock.Tokens.First()))
            return true;

        return false;
    }

    // Bir kelime veya ifadenin metin içinde bağımsız şekilde geçip geçmediğini kontrol eder.
    private static bool ContainsExactPhrase(string text, string phrase)
    {
        if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(phrase))
            return false;

        var pattern = $@"(^|\s){Regex.Escape(phrase)}(\s|$)";

        return Regex.IsMatch(text, pattern, RegexOptions.IgnoreCase);
    }

    // Tarifin malzeme sayısını hesaplar.
    private static int GetRecipeIngredientCount(Recipe recipe)
    {
        if (!string.IsNullOrWhiteSpace(recipe.IngredientsText))
        {
            return recipe.IngredientsText
                .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
                .Count(line => !string.IsNullOrWhiteSpace(line));
        }

        if (!string.IsNullOrWhiteSpace(recipe.NormalizedIngredientsText))
            return GetMeaningfulTokens(recipe.NormalizedIngredientsText).Count;

        return 0;
    }

    // Kullanıcının stoğunda olmayan tarif malzemelerini bulur.
    private static List<string> GetMissingIngredients(
        Recipe recipe,
        List<StockIngredientCandidate> userStockCandidates)
    {
        if (string.IsNullOrWhiteSpace(recipe.IngredientsText))
            return new List<string>();

        var missingIngredients = new List<string>();

        var ingredientLines = recipe.IngredientsText
            .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();

        foreach (var line in ingredientLines)
        {
            var normalizedLine = NormalizeText(line);
            var lineTokens = GetMeaningfulTokens(normalizedLine);

            var existsInStock = userStockCandidates.Any(stock =>
                IsIngredientMatched(normalizedLine, lineTokens, stock));

            if (!existsInStock)
                missingIngredients.Add(CleanIngredientLineForDisplay(line));
        }

        return missingIngredients
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .ToList();
    }

    // Eksik malzeme metnini ekranda daha temiz göstermek için miktar ve birim ifadelerini temizler.
    private static string CleanIngredientLineForDisplay(string line)
    {
        var text = line.Trim();

        text = Regex.Replace(text, @"^\d+([.,]\d+)?\s*", string.Empty);

        text = Regex.Replace(
            text,
            @"\b(adet|gr|g|gram|kg|kilogram|ml|lt|litre|su bardağı|yemek kaşığı|tatlı kaşığı|çay kaşığı|paket|tutam)\b",
            string.Empty,
            RegexOptions.IgnoreCase);

        text = Regex.Replace(text, @"\s+", " ").Trim();

        return string.IsNullOrWhiteSpace(text) ? line.Trim() : text;
    }

    // Seçilen ürün odaklı öneri açıklaması üretir.
    private static string CreateRecommendationReason(string selectedProductName, List<string> matchedIngredients)
    {
        var distinctMatchedIngredients = matchedIngredients
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .ToList();

        if (distinctMatchedIngredients.Count <= 1)
            return $"{selectedProductName} ürününü öncelikli değerlendirmek için önerildi.";

        var otherIngredients = distinctMatchedIngredients
            .Where(x => !x.Equals(selectedProductName, StringComparison.CurrentCultureIgnoreCase))
            .Take(4)
            .ToList();

        if (!otherIngredients.Any())
            return $"{selectedProductName} ürününü öncelikli değerlendirmek için önerildi.";

        return $"{selectedProductName} ürününü değerlendirmek için önerildi. Ayrıca stokundaki {string.Join(", ", otherIngredients)} ürünleriyle de eşleşiyor.";
    }

    // Genel tarif önerileri için kullanıcıya açıklama metni üretir.
    private static string CreateGeneralRecommendationReason(
        List<string> matchedIngredients,
        List<string> riskyMatchedIngredients)
    {
        var distinctMatchedIngredients = matchedIngredients
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .Take(5)
            .ToList();

        var distinctRiskyIngredients = riskyMatchedIngredients
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .Take(3)
            .ToList();

        if (distinctRiskyIngredients.Any())
        {
            return $"Stokundaki riskli {string.Join(", ", distinctRiskyIngredients)} ürünlerini değerlendirmek için önerildi. Ayrıca {string.Join(", ", distinctMatchedIngredients)} ürünleriyle eşleşiyor.";
        }

        if (distinctMatchedIngredients.Any())
            return $"Stokundaki {string.Join(", ", distinctMatchedIngredients)} ürünleriyle eşleştiği için önerildi.";

        return "Stok ürünlerine göre önerildi.";
    }

    // Stok ürününün son risk tahminine göre riskli kabul edilip edilmeyeceğini belirler.
    private static bool IsRiskyStockItem(StockItem stockItem)
    {
        if (stockItem.WasteRiskPredictions == null || !stockItem.WasteRiskPredictions.Any())
            return false;

        var latestPrediction = stockItem.WasteRiskPredictions
            .OrderByDescending(x => GetDateTimePropertyValue(x, "CreatedAt") ?? DateTime.MinValue)
            .FirstOrDefault();

        if (latestPrediction == null)
            return false;

        var riskLevelValue = GetPropertyValue(latestPrediction, "RiskLevel")?.ToString();

        if (string.IsNullOrWhiteSpace(riskLevelValue))
            return false;

        return riskLevelValue.Equals("Critical", StringComparison.OrdinalIgnoreCase) ||
               riskLevelValue.Equals("High", StringComparison.OrdinalIgnoreCase) ||
               riskLevelValue.Equals("Medium", StringComparison.OrdinalIgnoreCase) ||
               riskLevelValue.Equals("Kritik", StringComparison.OrdinalIgnoreCase) ||
               riskLevelValue.Equals("Yüksek", StringComparison.OrdinalIgnoreCase) ||
               riskLevelValue.Equals("Yuksek", StringComparison.OrdinalIgnoreCase) ||
               riskLevelValue.Equals("Orta", StringComparison.OrdinalIgnoreCase);
    }

    // Nesne içindeki property değerini reflection ile okur.
    private static object? GetPropertyValue(object source, string propertyName)
    {
        return source
            .GetType()
            .GetProperty(propertyName)?
            .GetValue(source);
    }

    // Nesne içindeki DateTime tipindeki property değerini okur.
    private static DateTime? GetDateTimePropertyValue(object source, string propertyName)
    {
        var value = GetPropertyValue(source, propertyName);

        if (value is DateTime dateTime)
            return dateTime;

        return null;
    }

    // Türkçe karakterleri sadeleştirerek metni eşleştirme için normalize eder.
    private static string NormalizeText(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        var text = input.Trim().ToLower(new CultureInfo("tr-TR"));

        text = text
            .Replace("ç", "c")
            .Replace("ğ", "g")
            .Replace("ı", "i")
            .Replace("ö", "o")
            .Replace("ş", "s")
            .Replace("ü", "u");

        text = Regex.Replace(text, @"[^a-z0-9\s]", " ");
        text = Regex.Replace(text, @"\s+", " ").Trim();

        return text;
    }

    // Malzeme eşleştirmesi için anlamlı kelimeleri token listesine çevirir.
    private static HashSet<string> GetMeaningfulTokens(string text)
    {
        var normalizedText = NormalizeText(text);

        if (string.IsNullOrWhiteSpace(normalizedText))
            return new HashSet<string>();

        // Ölçü, bağlaç ve tarif eşleştirmesinde anlam taşımayan kelimeler çıkarılır.
        var stopWords = new HashSet<string>
        {
            "ve", "ile", "icin", "uzere", "bir", "iki", "uc", "dort",
            "taze", "orta", "buyuk", "kucuk", "boy", "ince", "kalin",
            "yagli", "yagsiz", "dolu", "dolusu", "yarim", "az", "cok",
            "istege", "bagli", "arzuya", "gore", "aldigi", "kadar"
        };

        return normalizedText
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(token => !stopWords.Contains(token))
            .Where(token => token.Length >= 2)
            .ToHashSet();
    }

    // Stok ürününe göre üretilen tarif önerilerini geçmiş tablosuna kaydeder.
    private async Task SaveRecommendationHistoryAsync(
        string userId,
        int stockItemId,
        List<RecipeRecommendationDto> recommendations)
    {
        if (!recommendations.Any())
            return;

        var recommendedRecipeIds = recommendations
            .Select(x => x.RecipeId)
            .ToList();

        // Daha önce aynı kullanıcı, stok ürünü ve tarif için kayıt varsa tekrar eklenmez.
        var existingRecipeIds = await _context.RecipeRecommendations
            .Where(x =>
                x.UserId == userId &&
                x.StockItemId == stockItemId &&
                recommendedRecipeIds.Contains(x.RecipeId))
            .Select(x => x.RecipeId)
            .ToListAsync();

        var existingRecipeIdSet = existingRecipeIds.ToHashSet();

        var historiesToAdd = recommendations
            .Where(x => !existingRecipeIdSet.Contains(x.RecipeId))
            .Select(x => new RecipeRecommendation
            {
                UserId = userId,
                StockItemId = stockItemId,
                RecipeId = x.RecipeId,
                MatchScore = x.MatchScore,
                RecommendationReason = x.RecommendationReason,
                RecommendedAt = DateTime.Now,
                CreatedAt = DateTime.Now,
                IsActive = true
            })
            .ToList();

        if (!historiesToAdd.Any())
            return;

        await _context.RecipeRecommendations.AddRangeAsync(historiesToAdd);
        await _context.SaveChangesAsync();
    }

    // Stok ürününü tarif malzemeleriyle eşleştirmek için kullanılan iç modeldir.
    private class StockIngredientCandidate
    {
        public int ProductId { get; set; }

        public string ProductName { get; set; } = string.Empty;

        public string NormalizedName { get; set; } = string.Empty;

        public HashSet<string> Tokens { get; set; } = new();

        public bool IsRisky { get; set; }
    }

    // Kullanıcının tarif etkileşim geçmişini özetlemek için kullanılan iç modeldir.
    private class RecipeInteractionSummary
    {
        public int LikedCount { get; set; }

        public int SavedCount { get; set; }

        public int CookedCount { get; set; }

        public int DislikedCount { get; set; }

        public int ViewedCount { get; set; }
    }
}


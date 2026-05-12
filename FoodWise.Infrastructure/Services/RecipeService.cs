// RecipeService, kullanıcının stok ürünlerine göre tarif önerileri üretir.
// Tarif önerileri, dataset üzerinden aktarılan normalize edilmiş malzeme metinleri kullanılarak hesaplanır.

using FoodWise.Application.DTOs.Recipe;
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

    public RecipeService(FoodWiseDbContext context)
    {
        _context = context;
    }

    public async Task<List<RecipeRecommendationDto>> GetRecommendationsByStockItemAsync(string userId, int stockItemId)
    {
        // Giriş yapan kullanıcıya ait seçilen stok ürünü alınır.
        // Bu ürün genellikle son tüketim tarihi yaklaşan / riskli ürün olur.
        var selectedStockItem = await _context.StockItems
            .Include(x => x.Product)
           .FirstOrDefaultAsync(x =>
    x.Id == stockItemId &&
    x.UserId == userId &&
    x.IsActive &&
    x.Status == StockItemStatus.Active);

        if (selectedStockItem == null)
            return new List<RecipeRecommendationDto>();

        // Kullanıcının tüm aktif stokları alınır.
        // Seçilen riskli ürün ana önceliktir, diğer stok ürünleri ise skoru artırır.
        var userStockItems = await _context.StockItems
            .Include(x => x.Product)
            .Where(x =>
                x.UserId == userId &&
                x.IsActive &&
                x.Product != null)
            .ToListAsync();

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

        var selectedProductCandidate = new StockIngredientCandidate
        {
            ProductId = selectedStockItem.ProductId,
            ProductName = selectedStockItem.Product.Name,
            NormalizedName = NormalizeText(selectedStockItem.Product.Name),
            Tokens = GetMeaningfulTokens(selectedStockItem.Product.Name),
            IsRisky = true
        };

        var recipes = await _context.Recipes
            .AsNoTracking()
            .Where(x =>
                x.IsActive &&
                !string.IsNullOrWhiteSpace(x.NormalizedIngredientsText))
            .ToListAsync();

        var recommendations = recipes
            .Select(recipe => EvaluateRecipe(recipe, selectedProductCandidate, stockCandidates))
            .Where(result => result != null)
            .Select(result => result!)
            .OrderByDescending(x => x.MatchScore)
            .ThenBy(x => x.PreparationTimeMinutes)
            .Take(20)
            .ToList();

        await SaveRecommendationHistoryAsync(userId, stockItemId, recommendations);

        return recommendations;
    }

    public async Task<List<RecipeRecommendationDto>> GetGeneralRecommendationsAsync(string userId)
    {
        // Menüdeki Tarif Önerileri sayfası için kullanıcının tüm stoklarına göre genel öneri üretir.
        // Riskli ürünlerle eşleşen tarifler daha yüksek öncelik alır.
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

        var recommendations = recipes
            .Select(recipe => EvaluateGeneralRecipe(recipe, stockCandidates))
            .Where(result => result != null)
            .Select(result => result!)
            .OrderByDescending(x => x.MatchScore)
            .ThenBy(x => x.PreparationTimeMinutes)
            .Take(30)
            .ToList();

        return recommendations;
    }

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

    public async Task<List<RecipeRecommendationDto>> GetAllRecipesAsync()
    {
        // Dataset üzerinden çok sayıda tarif geldiği için genel liste sınırlı tutulur.
        // Asıl öneri akışı GetGeneralRecommendationsAsync ve GetRecommendationsByStockItemAsync üzerinden çalışır.
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

    private static RecipeRecommendationDto? EvaluateRecipe(
        Recipe recipe,
        StockIngredientCandidate selectedProduct,
        List<StockIngredientCandidate> userStockCandidates)
    {
        var normalizedIngredientsText = recipe.NormalizedIngredientsText ?? string.Empty;
        var recipeTokens = GetMeaningfulTokens(normalizedIngredientsText);

        if (!recipeTokens.Any())
            return null;

        var selectedProductMatched = IsIngredientMatched(
            normalizedIngredientsText,
            recipeTokens,
            selectedProduct);

        // Ana mantık: Seçilen/riskli ürün tarifte yoksa öneri olarak gösterilmez.
        if (!selectedProductMatched)
            return null;

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

        // Seçilen riskli ürün tarifte geçtiği için temel puan verilir.
        var selectedProductScore = 35;

        // Seçilen ürün dışındaki stok eşleşmeleri ayrıca puan kazandırır.
        var otherMatchedIngredientCount = matchedStockIngredients
            .Count(x => !x.ProductName.Equals(
                selectedProduct.ProductName,
                StringComparison.CurrentCultureIgnoreCase));

        var stockMatchScore = Math.Min(25, otherMatchedIngredientCount * 7);

        // Eksik malzeme çoksa skor düşürülür.
        var missingPenalty = Math.Min(15, missingIngredients.Count * 2);

        // Az malzemeli ve stok uyumu yüksek tariflere küçük bonus verilir.
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

        // Sadece seçilen ürün eşleştiyse skor fazla yükselmesin.
        if (otherMatchedIngredientCount == 0)
            finalScore = Math.Min(finalScore, 65);

        // Tüm malzemeler eşleşmiyorsa %100 verilmesin.
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

        // Kullanıcının stoklarından hiçbir ürün tarifle eşleşmiyorsa öneri olarak gösterilmez.
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

        // Stoktan eşleşen her ürün skoru artırır.
        var stockMatchScore = Math.Min(30, matchedIngredientCount * 7);

        // Riskli ürünleri içeren tariflere ekstra öncelik verilir.
        var riskyBonus = Math.Min(20, riskyMatchedIngredients.Count * 10);

        // Eksik malzeme çoksa skor düşürülür.
        var missingPenalty = Math.Min(18, missingIngredients.Count * 2);

        // Az malzemeli ve stok uyumu yüksek tariflere küçük bonus verilir.
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

        // Riskli ürün eşleşmesi yoksa skor fazla yükselmesin.
        if (!riskyMatchedIngredients.Any())
            finalScore = Math.Min(finalScore, 82);

        // Tüm malzemeler eşleşmiyorsa %100 verilmesin.
        if (totalIngredientCount > 0 && matchedIngredientCount < totalIngredientCount)
            finalScore = Math.Min(finalScore, 94);

        finalScore = Math.Clamp(finalScore, 0, 100);

        // Çok düşük eşleşmeler kullanıcıya gösterilmez.
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

    private static bool IsIngredientMatched(
        string normalizedIngredientsText,
        HashSet<string> recipeTokens,
        StockIngredientCandidate stock)
    {
        if (string.IsNullOrWhiteSpace(stock.NormalizedName))
            return false;

        // Ürün adı birebir ifade olarak geçiyorsa güçlü eşleşme kabul edilir.
        if (ContainsExactPhrase(normalizedIngredientsText, stock.NormalizedName))
            return true;

        if (!stock.Tokens.Any())
            return false;

        // Çok kelimeli ürünlerde tüm anlamlı kelimeler tarif içinde geçiyorsa eşleşme kabul edilir.
        if (stock.Tokens.Count > 1 && stock.Tokens.All(recipeTokens.Contains))
            return true;

        // Tek kelimeli ürünlerde token bazlı eşleşme yapılır.
        if (stock.Tokens.Count == 1 && recipeTokens.Contains(stock.Tokens.First()))
            return true;

        return false;
    }

    private static bool ContainsExactPhrase(string text, string phrase)
    {
        if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(phrase))
            return false;

        var pattern = $@"(^|\s){Regex.Escape(phrase)}(\s|$)";

        return Regex.IsMatch(text, pattern, RegexOptions.IgnoreCase);
    }

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

    private static object? GetPropertyValue(object source, string propertyName)
    {
        return source
            .GetType()
            .GetProperty(propertyName)?
            .GetValue(source);
    }

    private static DateTime? GetDateTimePropertyValue(object source, string propertyName)
    {
        var value = GetPropertyValue(source, propertyName);

        if (value is DateTime dateTime)
            return dateTime;

        return null;
    }

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

    private static HashSet<string> GetMeaningfulTokens(string text)
    {
        var normalizedText = NormalizeText(text);

        if (string.IsNullOrWhiteSpace(normalizedText))
            return new HashSet<string>();

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

    private class StockIngredientCandidate
    {
        public int ProductId { get; set; }

        public string ProductName { get; set; } = string.Empty;

        public string NormalizedName { get; set; } = string.Empty;

        public HashSet<string> Tokens { get; set; } = new();

        public bool IsRisky { get; set; }
    }
}
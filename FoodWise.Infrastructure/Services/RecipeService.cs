using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// RecipeService, riskli stok ürünlerine göre tarif önerileri üretir.
// Sistem ilk aşamada local veritabanındaki tarifleri kullanır.
using FoodWise.Application.DTOs.Recipe;
using FoodWise.Application.Interfaces;
using FoodWise.Domain.Entities;
using FoodWise.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

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
        // Giriş yapan kullanıcıya ait stok ürünü kontrol edilir.
        // Başka kullanıcının stok ürünü üzerinden öneri alınması engellenir.
        var stockItem = await _context.StockItems
            .Include(x => x.Product)
            .Include(x => x.WasteRiskPredictions)
            .FirstOrDefaultAsync(x => x.Id == stockItemId && x.UserId == userId);

        if (stockItem == null)
            return new List<RecipeRecommendationDto>();

        // Stoktaki ürünün ProductId değerini içeren tarifler bulunur.
        var recipes = await _context.Recipes
            .Include(x => x.RecipeIngredients)
                .ThenInclude(x => x.Product)
            .Include(x => x.RecipeIngredients)
                .ThenInclude(x => x.Unit)
            .Where(x => x.IsActive &&
                        x.RecipeIngredients.Any(i => i.ProductId == stockItem.ProductId))
            .ToListAsync();

        var recommendations = recipes
            .Select(recipe => MapToRecommendationDto(recipe, stockItem.Product.Name))
            .ToList();

        // Öneriler RecipeRecommendations tablosuna geçmiş kayıt olarak eklenir.
        await SaveRecommendationHistoryAsync(userId, stockItemId, recommendations);

        return recommendations;
    }

    public async Task<List<RecipeRecommendationDto>> GetRecipesByProductAsync(int productId)
    {
        // Belirli bir ürünü içeren tüm tarifler listelenir.
        var product = await _context.Products.FirstOrDefaultAsync(x => x.Id == productId);

        if (product == null)
            return new List<RecipeRecommendationDto>();

        var recipes = await _context.Recipes
            .Include(x => x.RecipeIngredients)
                .ThenInclude(x => x.Product)
            .Include(x => x.RecipeIngredients)
                .ThenInclude(x => x.Unit)
            .Where(x => x.IsActive &&
                        x.RecipeIngredients.Any(i => i.ProductId == productId))
            .ToListAsync();

        return recipes
            .Select(recipe => MapToRecommendationDto(recipe, product.Name))
            .ToList();
    }

    public async Task<List<RecipeRecommendationDto>> GetAllRecipesAsync()
    {
        // Sistemdeki tüm aktif local tarifler listelenir.
        var recipes = await _context.Recipes
            .Include(x => x.RecipeIngredients)
                .ThenInclude(x => x.Product)
            .Include(x => x.RecipeIngredients)
                .ThenInclude(x => x.Unit)
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .ToListAsync();

        return recipes
            .Select(recipe => MapToRecommendationDto(recipe, null))
            .ToList();
    }

    private RecipeRecommendationDto MapToRecommendationDto(Recipe recipe, string? matchedProductName)
    {
        // Eşleşen ürün varsa öneri sebebi daha anlamlı hale getirilir.
        var reason = matchedProductName == null
            ? "Bu tarif FoodWise tarif havuzunda yer almaktadır."
            : $"{matchedProductName} ürününü değerlendirmek için uygun bir tarif önerisidir.";

        return new RecipeRecommendationDto
        {
            RecipeId = recipe.Id,
            RecipeName = recipe.Name,
            Description = recipe.Description,
            Instructions = recipe.Instructions,
            PreparationTimeMinutes = recipe.PreparationTimeMinutes,
            ImageUrl = recipe.ImageUrl,
            MatchScore = matchedProductName == null ? 60 : 90,
            RecommendationReason = reason,

            Ingredients = recipe.RecipeIngredients.Select(i => new RecipeIngredientDto
            {
                ProductId = i.ProductId,
                ProductName = i.Product.Name,
                Quantity = i.Quantity,
                UnitName = i.Unit?.ShortName,
                IsRequired = i.IsRequired
            }).ToList()
        };
    }

    private async Task SaveRecommendationHistoryAsync(
        string userId,
        int stockItemId,
        List<RecipeRecommendationDto> recommendations)
    {
        // Aynı stok ürünü için aynı tarif önerisi daha önce kaydedildiyse tekrar eklenmez.
        foreach (var recommendation in recommendations)
        {
            var exists = await _context.RecipeRecommendations.AnyAsync(x =>
                x.UserId == userId &&
                x.StockItemId == stockItemId &&
                x.RecipeId == recommendation.RecipeId);

            if (exists)
                continue;

            var recommendationHistory = new RecipeRecommendation
            {
                UserId = userId,
                StockItemId = stockItemId,
                RecipeId = recommendation.RecipeId,
                MatchScore = recommendation.MatchScore,
                RecommendationReason = recommendation.RecommendationReason,
                RecommendedAt = DateTime.Now,
                CreatedAt = DateTime.Now
            };

            await _context.RecipeRecommendations.AddAsync(recommendationHistory);
        }

        await _context.SaveChangesAsync();
    }
}

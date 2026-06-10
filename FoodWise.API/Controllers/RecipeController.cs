// RecipeController, tarif listeleme ve stok ürününe göre tarif önerme endpointlerini içerir.
// Riskli ürünler için önerilen tarifler bu controller üzerinden alınır.
// Kullanıcı tarif etkileşimleri ve AI eğitim verisi dışa aktarımı da bu controller üzerinden yönetilir.

using FoodWise.Application.DTOs.Recipe;
using FoodWise.Application.Interfaces;
using FoodWise.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Security.Claims;
using System.Text;

namespace FoodWise.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class RecipeController : ControllerBase
{
    private readonly IRecipeService _recipeService;
    private readonly IRecipeDatasetImportService _recipeDatasetImportService;
    private readonly IWebHostEnvironment _environment;

    public RecipeController(
        IRecipeService recipeService,
        IRecipeDatasetImportService recipeDatasetImportService,
        IWebHostEnvironment environment)
    {
        _recipeService = recipeService;
        _recipeDatasetImportService = recipeDatasetImportService;
        _environment = environment;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllRecipes()
    {
        var result = await _recipeService.GetAllRecipesAsync();

        return Ok(result);
    }

    [HttpGet("by-product/{productId}")]
    public async Task<IActionResult> GetRecipesByProduct(int productId)
    {
        var result = await _recipeService.GetRecipesByProductAsync(productId);

        return Ok(result);
    }

    [HttpGet("recommendations/{stockItemId}")]
    public async Task<IActionResult> GetRecommendationsByStockItem(int stockItemId)
    {
        var userId = GetUserId();

        var result = await _recipeService.GetRecommendationsByStockItemAsync(userId, stockItemId);

        return Ok(result);
    }

    [HttpGet("recommendations")]
    public async Task<IActionResult> GetGeneralRecommendations()
    {
        var userId = GetUserId();

        var result = await _recipeService.GetGeneralRecommendationsAsync(userId);

        return Ok(result);
    }

    [HttpPost("interactions")]
    public async Task<IActionResult> CreateInteraction([FromBody] CreateRecipeInteractionDto dto)
    {
        var userId = GetUserId();

        var result = await _recipeService.CreateRecipeInteractionAsync(userId, dto);

        if (!result)
        {
            return BadRequest(new
            {
                Message = "Tarif etkileşimi kaydedilemedi. Tarif, stok ürünü veya etkileşim tipi geçersiz olabilir."
            });
        }

        return Ok(new
        {
            Message = "Tarif etkileşimi başarıyla kaydedildi."
        });
    }

    [HttpGet("interactions/saved")]
    public async Task<IActionResult> GetSavedRecipes()
    {
        var userId = GetUserId();

        var result = await _recipeService.GetRecipesByInteractionTypeAsync(
            userId,
            RecipeInteractionType.Saved
        );

        return Ok(result);
    }

    [HttpGet("interactions/cooked")]
    public async Task<IActionResult> GetCookedRecipes()
    {
        var userId = GetUserId();

        var result = await _recipeService.GetRecipesByInteractionTypeAsync(
            userId,
            RecipeInteractionType.Cooked
        );

        return Ok(result);
    }

    [HttpGet("ai-training-data")]
    public async Task<IActionResult> ExportAiTrainingData()
    {
        var trainingData = await _recipeService.GetRecipeAiTrainingDataAsync();

        var csvBuilder = new StringBuilder();

        csvBuilder.AppendLine(
            "UserId,RecipeId,InteractionType,Label,RecommendationScore,PreparationTimeMinutes,IngredientCount,HasStockContext,UserLikedCount,UserSavedCount,UserCookedCount,UserDislikedCount,RecipeLikedCount,RecipeSavedCount,RecipeCookedCount,RecipeDislikedCount"
        );

        foreach (var item in trainingData)
        {
            csvBuilder.AppendLine(string.Join(",", new[]
            {
                EscapeCsv(item.UserId),
                item.RecipeId.ToString(CultureInfo.InvariantCulture),
                item.InteractionType.ToString(CultureInfo.InvariantCulture),
                item.Label.ToString(CultureInfo.InvariantCulture),
                item.RecommendationScore.ToString(CultureInfo.InvariantCulture),
                item.PreparationTimeMinutes.ToString(CultureInfo.InvariantCulture),
                item.IngredientCount.ToString(CultureInfo.InvariantCulture),
                item.HasStockContext ? "1" : "0",
                item.UserLikedCount.ToString(CultureInfo.InvariantCulture),
                item.UserSavedCount.ToString(CultureInfo.InvariantCulture),
                item.UserCookedCount.ToString(CultureInfo.InvariantCulture),
                item.UserDislikedCount.ToString(CultureInfo.InvariantCulture),
                item.RecipeLikedCount.ToString(CultureInfo.InvariantCulture),
                item.RecipeSavedCount.ToString(CultureInfo.InvariantCulture),
                item.RecipeCookedCount.ToString(CultureInfo.InvariantCulture),
                item.RecipeDislikedCount.ToString(CultureInfo.InvariantCulture)
            }));
        }

        var fileBytes = Encoding.UTF8.GetBytes(csvBuilder.ToString());

        return File(
            fileBytes,
            "text/csv",
            "recipe_ai_training_data.csv"
        );
    }

    [HttpPost("import-dataset")]
    public async Task<IActionResult> ImportDataset()
    {
        var filePath = Path.Combine(
            _environment.ContentRootPath,
            "Data",
            "Recipes",
            "recipes_groq_cleaned.json");

        if (!System.IO.File.Exists(filePath))
            return NotFound("Tarif veri seti dosyası bulunamadı.");

        var importedCount = await _recipeDatasetImportService.ImportFromJsonAsync(filePath);

        return Ok(new
        {
            Message = $"{importedCount} tarif veritabanına aktarıldı.",
            ImportedCount = importedCount
        });
    }

    private string GetUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    }

    private static string EscapeCsv(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";

        return value;
    }
}
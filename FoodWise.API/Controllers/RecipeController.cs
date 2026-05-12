// RecipeController, tarif listeleme ve stok ürününe göre tarif önerme endpointlerini içerir.
// Riskli ürünler için önerilen tarifler bu controller üzerinden alınır.
using System.Security.Claims;
using FoodWise.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodWise.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class RecipeController : ControllerBase
{
    private readonly IRecipeService _recipeService;
    private readonly IRecipeDatasetImportService _recipeDatasetImportService;
    private readonly IWebHostEnvironment _environment;
    public RecipeController(IRecipeService recipeService, IRecipeDatasetImportService recipeDatasetImportService, IWebHostEnvironment environment)
    {
        _recipeService = recipeService;
        _recipeDatasetImportService = recipeDatasetImportService;
        _environment = environment;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllRecipes()
    {
        // Sistemdeki tüm aktif tarifleri listeler.
        var result = await _recipeService.GetAllRecipesAsync();

        return Ok(result);
    }

    [HttpGet("by-product/{productId}")]
    public async Task<IActionResult> GetRecipesByProduct(int productId)
    {
        // Belirli bir ürünü içeren tarifleri listeler.
        var result = await _recipeService.GetRecipesByProductAsync(productId);

        return Ok(result);
    }

    [HttpGet("recommendations/{stockItemId}")]
    public async Task<IActionResult> GetRecommendationsByStockItem(int stockItemId)
    {
        // Token içindeki kullanıcı Id bilgisi alınır.
        var userId = GetUserId();

        // Giriş yapan kullanıcının stok ürününe göre tarif önerisi üretilir.
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
    private string GetUserId()
    {
        // JWT token içindeki NameIdentifier claim'i Identity kullanıcı Id bilgisini taşır.
        return User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    }
    // Tarif veri setindeki JSON verilerini veritabanına aktarmak için kullanılan endpoint.
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
}
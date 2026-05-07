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

    public RecipeController(IRecipeService recipeService)
    {
        _recipeService = recipeService;
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

    private string GetUserId()
    {
        // JWT token içindeki NameIdentifier claim'i Identity kullanıcı Id bilgisini taşır.
        return User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    }
}
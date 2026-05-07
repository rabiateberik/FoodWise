// RecipeController, Web arayüzünde tarif listeleme ve stok ürününe göre tarif önerisi gösterme işlemlerini yönetir.
// API ile doğrudan değil, IRecipeWebService üzerinden haberleşir.

using FoodWise.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace FoodWise.Web.Controllers;

public class RecipeController : Controller
{
    private readonly IRecipeWebService _recipeWebService;

    public RecipeController(IRecipeWebService recipeWebService)
    {
        _recipeWebService = recipeWebService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (string.IsNullOrWhiteSpace(token))
            return RedirectToAction("Login", "Auth");

        var recipes = await _recipeWebService.GetAllRecipesAsync(token);

        ViewBag.FullName = HttpContext.Session.GetString("FullName");
        ViewBag.Email = HttpContext.Session.GetString("Email");

        return View("Recommendations", recipes);
    }

    [HttpGet]
    public async Task<IActionResult> Recommendations(int stockItemId, string? productName)
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (string.IsNullOrWhiteSpace(token))
            return RedirectToAction("Login", "Auth");

        var recommendations = await _recipeWebService.GetRecommendationsByStockItemAsync(stockItemId, token);

        ViewBag.StockItemId = stockItemId;
        ViewBag.ProductName = productName;
        ViewBag.FullName = HttpContext.Session.GetString("FullName");
        ViewBag.Email = HttpContext.Session.GetString("Email");

        return View(recommendations);
    }
}
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

        // Kullanıcının tüm stoklarına göre genel tarif önerileri alınır.
        var recipes = await _recipeWebService.GetGeneralRecommendationsAsync(token);

        ViewBag.ProductName = null;
        ViewBag.IsGeneralRecipeList = true;

        return View("Recommendations", recipes);
    }


    [HttpGet]
    public async Task<IActionResult> Recommendations(int? stockItemId, string? productName)
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (string.IsNullOrWhiteSpace(token))
            return RedirectToAction("Login", "Auth");

        // Stok ürünü seçilmeden tarif önerisi istenirse kullanıcı riskli ürünler sayfasına yönlendirilir.
        if (!stockItemId.HasValue || stockItemId.Value <= 0)
        {
            TempData["ErrorMessage"] = "Tarif önerisi almak için önce bir stok ürünü seçmelisin.";
            return RedirectToAction("Risky", "Stock");
        }

        // Seçilen stok ürününe göre dataset tabanlı tarif önerileri alınır.
        var recommendations = await _recipeWebService.GetRecommendationsByStockItemAsync(
            stockItemId.Value,
            token);

        ViewBag.StockItemId = stockItemId.Value;
        ViewBag.ProductName = productName;
        ViewBag.IsGeneralRecipeList = false;

        return View(recommendations);
    }
}
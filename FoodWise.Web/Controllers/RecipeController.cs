// RecipeController, Web arayüzünde tarif listeleme, stok ürününe göre tarif önerisi
// ve kullanıcı tarif etkileşimlerini yönetir.
// API ile doğrudan değil, IRecipeWebService üzerinden haberleşir.

using FoodWise.Web.Services;
using FoodWise.Web.ViewModels.Recipe;
using Microsoft.AspNetCore.Mvc;

namespace FoodWise.Web.Controllers;

public class RecipeController : Controller
{
    private readonly IRecipeWebService _recipeWebService;

    private const int InteractionViewed = 1;
    private const int InteractionLiked = 2;
    private const int InteractionSaved = 3;
    private const int InteractionCooked = 4;
    private const int InteractionDisliked = 5;

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
        ViewBag.StockItemId = null;
        ViewBag.IsGeneralRecipeList = true;
        ViewBag.ActiveRecipeTab = "Recommendations";

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

        // Seçilen stok ürününe göre tarif önerileri alınır.
        var recommendations = await _recipeWebService.GetRecommendationsByStockItemAsync(
            stockItemId.Value,
            token);

        ViewBag.StockItemId = stockItemId.Value;
        ViewBag.ProductName = productName;
        ViewBag.IsGeneralRecipeList = false;
        ViewBag.ActiveRecipeTab = "Recommendations";

        return View(recommendations);
    }

    [HttpGet]
    public async Task<IActionResult> Saved()
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (string.IsNullOrWhiteSpace(token))
            return RedirectToAction("Login", "Auth");

        // Kullanıcının kaydettiği tarifler alınır.
        var recipes = await _recipeWebService.GetSavedRecipesAsync(token);

        ViewBag.ProductName = null;
        ViewBag.StockItemId = null;
        ViewBag.IsGeneralRecipeList = true;
        ViewBag.ActiveRecipeTab = "Saved";

        return View("Recommendations", recipes);
    }

    [HttpGet]
    public async Task<IActionResult> Cooked()
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (string.IsNullOrWhiteSpace(token))
            return RedirectToAction("Login", "Auth");

        // Kullanıcının "Yaptım" olarak işaretlediği tarifler alınır.
        var recipes = await _recipeWebService.GetCookedRecipesAsync(token);

        ViewBag.ProductName = null;
        ViewBag.StockItemId = null;
        ViewBag.IsGeneralRecipeList = true;
        ViewBag.ActiveRecipeTab = "Cooked";

        return View("Recommendations", recipes);
    }
    [HttpGet]
    public async Task<IActionResult> Detail(
    int recipeId,
    int? stockItemId,
    int? recommendationScore,
    string? source,
    string? returnUrl)
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (string.IsNullOrWhiteSpace(token))
            return RedirectToAction("Login", "Auth");

        List<RecipeRecommendationViewModel> recipes;

        if (string.Equals(source, "Saved", StringComparison.OrdinalIgnoreCase))
        {
            recipes = await _recipeWebService.GetSavedRecipesAsync(token);
        }
        else if (string.Equals(source, "Cooked", StringComparison.OrdinalIgnoreCase))
        {
            recipes = await _recipeWebService.GetCookedRecipesAsync(token);
        }
        else if (stockItemId.HasValue && stockItemId.Value > 0)
        {
            recipes = await _recipeWebService.GetRecommendationsByStockItemAsync(stockItemId.Value, token);
        }
        else
        {
            recipes = await _recipeWebService.GetGeneralRecommendationsAsync(token);
        }

        var recipe = recipes.FirstOrDefault(x => x.RecipeId == recipeId);

        if (recipe == null)
        {
            TempData["ErrorMessage"] = "Tarif detayı bulunamadı.";
            return RedirectToSafeUrl(returnUrl);
        }

        // Kullanıcı tarif detayını açtığında Viewed etkileşimi kaydedilir.
        await _recipeWebService.CreateRecipeInteractionAsync(new CreateRecipeInteractionViewModel
        {
            RecipeId = recipeId,
            InteractionType = InteractionViewed,
            StockItemId = stockItemId,
            RecommendationScore = recommendationScore
        }, token);

        ViewBag.StockItemId = stockItemId;
        ViewBag.RecommendationScore = recommendationScore ?? recipe.MatchScore;
        ViewBag.ReturnUrl = returnUrl;
        ViewBag.Source = source;

        return View(recipe);
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Like(
        int recipeId,
        int? stockItemId,
        int? recommendationScore,
        string? returnUrl)
    {
        return await CreateInteractionAndRedirectAsync(
            recipeId,
            InteractionLiked,
            stockItemId,
            recommendationScore,
            "Tarif beğenildi.",
            returnUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(
        int recipeId,
        int? stockItemId,
        int? recommendationScore,
        string? returnUrl)
    {
        return await CreateInteractionAndRedirectAsync(
            recipeId,
            InteractionSaved,
            stockItemId,
            recommendationScore,
            "Tarif kaydedildi.",
            returnUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cook(
        int recipeId,
        int? stockItemId,
        int? recommendationScore,
        string? returnUrl)
    {
        return await CreateInteractionAndRedirectAsync(
            recipeId,
            InteractionCooked,
            stockItemId,
            recommendationScore,
            "Tarif yaptıkların listesine eklendi.",
            returnUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Dislike(
        int recipeId,
        int? stockItemId,
        int? recommendationScore,
        string? returnUrl)
    {
        return await CreateInteractionAndRedirectAsync(
            recipeId,
            InteractionDisliked,
            stockItemId,
            recommendationScore,
            "Tarif önerilerinde daha az gösterilecek.",
            returnUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ViewRecipe(
        int recipeId,
        int? stockItemId,
        int? recommendationScore,
        string? returnUrl)
    {
        return await CreateInteractionAndRedirectAsync(
            recipeId,
            InteractionViewed,
            stockItemId,
            recommendationScore,
            null,
            returnUrl);
    }

    private async Task<IActionResult> CreateInteractionAndRedirectAsync(
        int recipeId,
        int interactionType,
        int? stockItemId,
        int? recommendationScore,
        string? successMessage,
        string? returnUrl)
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (string.IsNullOrWhiteSpace(token))
            return RedirectToAction("Login", "Auth");

        if (recipeId <= 0)
        {
            TempData["ErrorMessage"] = "Tarif bilgisi bulunamadı.";
            return RedirectToSafeUrl(returnUrl);
        }

        var model = new CreateRecipeInteractionViewModel
        {
            RecipeId = recipeId,
            InteractionType = interactionType,
            StockItemId = stockItemId,
            RecommendationScore = recommendationScore
        };

        var result = await _recipeWebService.CreateRecipeInteractionAsync(model, token);

        if (result)
        {
            if (!string.IsNullOrWhiteSpace(successMessage))
                TempData["SuccessMessage"] = successMessage;
        }
        else
        {
            TempData["ErrorMessage"] = "Tarif etkileşimi kaydedilemedi.";
        }

        return RedirectToSafeUrl(returnUrl);
    }

    private IActionResult RedirectToSafeUrl(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction(nameof(Index));
    }
}
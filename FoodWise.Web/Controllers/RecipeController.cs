
// RecipeController, Web arayüzünde tarif listeleme ve tarif önerisi ekranlarını yönetir.
// Genel tarif önerileri, stok ürününe göre öneriler, kaydedilen/yapılan tarifler
// ve kullanıcı tarif etkileşimleri bu controller üzerinden karşılanır.
// Controller tarif önerisini kendisi hesaplamaz; tüm işlemleri IRecipeWebService üzerinden FoodWise.API'ye gönderir.

using FoodWise.Web.Services;
using FoodWise.Web.ViewModels.Recipe;
using Microsoft.AspNetCore.Mvc;

namespace FoodWise.Web.Controllers;

public class RecipeController : Controller
{
    private readonly IRecipeWebService _recipeWebService;

    // API tarafında tarif etkileşim türlerini temsil eden sabit değerlerdir.
    private const int InteractionViewed = 1;
    private const int InteractionLiked = 2;
    private const int InteractionSaved = 3;
    private const int InteractionCooked = 4;
    private const int InteractionDisliked = 5;

    public RecipeController(IRecipeWebService recipeWebService)
    {
        _recipeWebService = recipeWebService;
    }

    // Kullanıcının tüm stoklarına göre genel tarif önerilerini listeler.
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (string.IsNullOrWhiteSpace(token))
            return RedirectToAction("Login", "Auth");

        // Kullanıcının stok durumuna göre genel tarif önerileri API'den alınır.
        var recipes = await _recipeWebService.GetGeneralRecommendationsAsync(token);

        ViewBag.ProductName = null;
        ViewBag.StockItemId = null;
        ViewBag.IsGeneralRecipeList = true;
        ViewBag.ActiveRecipeTab = "Recommendations";

        return View("Recommendations", recipes);
    }

    // Seçilen stok ürününe göre tarif önerilerini listeler.
    [HttpGet]
    public async Task<IActionResult> Recommendations(int? stockItemId, string? productName)
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (string.IsNullOrWhiteSpace(token))
            return RedirectToAction("Login", "Auth");

        // Stok ürünü seçilmeden öneri istenirse kullanıcı riskli ürünler sayfasına yönlendirilir.
        if (!stockItemId.HasValue || stockItemId.Value <= 0)
        {
            TempData["ErrorMessage"] = "Tarif önerisi almak için önce bir stok ürünü seçmelisin.";
            return RedirectToAction("Risky", "Stock");
        }

        // Seçilen stok ürününe göre tarif önerileri API'den alınır.
        var recommendations = await _recipeWebService.GetRecommendationsByStockItemAsync(
            stockItemId.Value,
            token);

        ViewBag.StockItemId = stockItemId.Value;
        ViewBag.ProductName = productName;
        ViewBag.IsGeneralRecipeList = false;
        ViewBag.ActiveRecipeTab = "Recommendations";

        return View(recommendations);
    }

    // Kullanıcının kaydettiği tarifleri listeler.
    [HttpGet]
    public async Task<IActionResult> Saved()
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (string.IsNullOrWhiteSpace(token))
            return RedirectToAction("Login", "Auth");

        var recipes = await _recipeWebService.GetSavedRecipesAsync(token);

        ViewBag.ProductName = null;
        ViewBag.StockItemId = null;
        ViewBag.IsGeneralRecipeList = true;
        ViewBag.ActiveRecipeTab = "Saved";

        return View("Recommendations", recipes);
    }

    // Kullanıcının yaptığı olarak işaretlediği tarifleri listeler.
    [HttpGet]
    public async Task<IActionResult> Cooked()
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (string.IsNullOrWhiteSpace(token))
            return RedirectToAction("Login", "Auth");

        var recipes = await _recipeWebService.GetCookedRecipesAsync(token);

        ViewBag.ProductName = null;
        ViewBag.StockItemId = null;
        ViewBag.IsGeneralRecipeList = true;
        ViewBag.ActiveRecipeTab = "Cooked";

        return View("Recommendations", recipes);
    }

    // Seçilen tarifin detay sayfasını açar.
    // Tarif detayı açıldığında Viewed etkileşimi API'ye kaydedilir.
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

        // Detay sayfası hangi sekmeden açıldıysa tarif o liste içinden aranır.
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

        // Kullanıcı tarif detayını açtığında görüntüleme etkileşimi kaydedilir.
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

    // Kullanıcının tarifi beğenme etkileşimini kaydeder.
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

    // Kullanıcının tarifi kaydetme etkileşimini kaydeder.
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

    // Kullanıcının tarifi yaptım olarak işaretleme etkileşimini kaydeder.
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

    // Kullanıcının tarifi beğenmediğini API'ye bildirir.
    // Bu bilgi sonraki önerilerde kişiselleştirme için kullanılabilir.
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

    // Tarif görüntüleme etkileşimini manuel olarak kaydetmek için kullanılır.
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

    // Beğenme, kaydetme, yaptım, beğenmedim ve görüntüleme işlemleri için ortak etkileşim kaydetme metodudur.
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

    // Kullanıcıyı işlem yaptığı önceki sayfaya güvenli şekilde geri yönlendirir.
    // Local olmayan URL'lere yönlendirme yapılmayarak açık yönlendirme riski engellenir.
    private IActionResult RedirectToSafeUrl(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction(nameof(Index));
    }
}


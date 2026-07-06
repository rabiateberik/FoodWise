
// HomeController, giriş yapan kullanıcı için Dashboard ekranını yönetir.
// Dashboard kartlarında gösterilecek stok, riskli ürün, karbon tasarrufu ve eco puan bilgileri
// ilgili Web servisleri üzerinden FoodWise.API'den alınır.

using FoodWise.Web.Services;
using FoodWise.Web.ViewModels.Home;
using Microsoft.AspNetCore.Mvc;

namespace FoodWise.Web.Controllers;

public class HomeController : Controller
{
    private readonly IStockWebService _stockWebService;
    private readonly ICarbonReportWebService _carbonReportWebService;
    private readonly IEcoPointWebService _ecoPointWebService;

    public HomeController(
        IStockWebService stockWebService,
        ICarbonReportWebService carbonReportWebService,
        IEcoPointWebService ecoPointWebService)
    {
        _stockWebService = stockWebService;
        _carbonReportWebService = carbonReportWebService;
        _ecoPointWebService = ecoPointWebService;
    }

    // Giriş yapan kullanıcının Dashboard sayfasını açar.
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var token = HttpContext.Session.GetString("JWToken");

        // Token yoksa kullanıcı giriş yapmamış kabul edilir ve login sayfasına yönlendirilir.
        if (string.IsNullOrWhiteSpace(token))
            return RedirectToAction("Login", "Auth");

        // Dashboard kartlarında gösterilecek veriler API servislerinden alınır.
        var stockItems = await _stockWebService.GetMyStockAsync(token);
        var riskyStockItems = await _stockWebService.GetRiskyStockAsync(token);
        var carbonSummary = await _carbonReportWebService.GetSummaryAsync(token);
        var ecoPointSummary = await _ecoPointWebService.GetSummaryAsync(token);

        // Farklı servislerden gelen veriler tek DashboardViewModel içinde birleştirilir.
        var model = new DashboardViewModel
        {
            FullName = HttpContext.Session.GetString("FullName") ?? "Kullanıcı",
            Email = HttpContext.Session.GetString("Email") ?? string.Empty,
            TotalStockCount = stockItems.Count,
            RiskyStockCount = riskyStockItems.Count,
            CarbonSavedKg = carbonSummary.TotalEstimatedCarbonSaved,
            EcoPoint = ecoPointSummary.TotalPoint,
            EcoPointLevelName = ecoPointSummary.LevelName
        };

        return View(model);
    }
}

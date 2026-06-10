// HomeController, giriş yapan kullanıcıyı Dashboard ekranına yönlendirir.
// Dashboard kartları, eco puan ve karbon özeti için gerekli verileri API servislerinden alır.

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

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (string.IsNullOrWhiteSpace(token))
            return RedirectToAction("Login", "Auth");

        var stockItems = await _stockWebService.GetMyStockAsync(token);
        var riskyStockItems = await _stockWebService.GetRiskyStockAsync(token);
        var carbonSummary = await _carbonReportWebService.GetSummaryAsync(token);
        var ecoPointSummary = await _ecoPointWebService.GetSummaryAsync(token);

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
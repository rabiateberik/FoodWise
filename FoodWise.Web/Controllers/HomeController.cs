// HomeController, giriş yapan kullanıcıyı Dashboard ekranına yönlendirir.
// Dashboard kartları, üst bildirim alanı ve karbon özeti için gerekli verileri API servislerinden alır.

using FoodWise.Web.Services;
using FoodWise.Web.ViewModels.Home;
using Microsoft.AspNetCore.Mvc;

namespace FoodWise.Web.Controllers;

public class HomeController : Controller
{
    private readonly IStockWebService _stockWebService;
    private readonly INotificationWebService _notificationWebService;
    private readonly ICarbonReportWebService _carbonReportWebService;
    private readonly IEcoPointWebService _ecoPointWebService;

    public HomeController(
        IStockWebService stockWebService,
        INotificationWebService notificationWebService,
        ICarbonReportWebService carbonReportWebService,
        IEcoPointWebService ecoPointWebService)
    {
        _stockWebService = stockWebService;
        _notificationWebService = notificationWebService;
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
        var unreadNotificationCount = await _notificationWebService.GetUnreadCountAsync(token);
        var notifications = await _notificationWebService.GetMyNotificationsAsync(token);
        var carbonSummary = await _carbonReportWebService.GetSummaryAsync(token);
        var ecoPointSummary = await _ecoPointWebService.GetSummaryAsync(token);

        var model = new DashboardViewModel
        {
            FullName = HttpContext.Session.GetString("FullName") ?? "Kullanıcı",
            Email = HttpContext.Session.GetString("Email") ?? string.Empty,
            TotalStockCount = stockItems.Count,
            RiskyStockCount = riskyStockItems.Count,
            UnreadNotificationCount = unreadNotificationCount,
            CarbonSavedKg = carbonSummary.TotalEstimatedCarbonSaved,
            RecentNotifications = notifications
                .OrderByDescending(notification => notification.CreatedAt)
                .Take(5)
                .ToList(),
            EcoPoint = ecoPointSummary.TotalPoint,
            EcoPointLevelName = ecoPointSummary.LevelName
        };

        return View(model);
    }
}

// EcoPointController, Web arayüzünde kullanıcının eco puan sayfasını yönetir.
// Eco puan özeti ve puan geçmişi doğrudan burada hesaplanmaz;
// IEcoPointWebService üzerinden FoodWise.API'den alınır.

using FoodWise.Web.Services;
using FoodWise.Web.ViewModels.EcoPoint;
using Microsoft.AspNetCore.Mvc;

namespace FoodWise.Web.Controllers;

public class EcoPointController : Controller
{
    private readonly IEcoPointWebService _ecoPointWebService;

    public EcoPointController(IEcoPointWebService ecoPointWebService)
    {
        _ecoPointWebService = ecoPointWebService;
    }

    // Kullanıcının eco puan özetini ve puan geçmişini gösteren sayfayı açar.
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (string.IsNullOrWhiteSpace(token))
            return RedirectToAction("Login", "Auth");

        // Kullanıcının toplam eco puanı, seviyesi ve puan geçmişi API üzerinden alınır.
        var summary = await _ecoPointWebService.GetSummaryAsync(token);
        var history = await _ecoPointWebService.GetHistoryAsync(token);

        // Sayfada kullanılacak özet ve geçmiş bilgileri tek ViewModel içinde birleştirilir.
        var model = new EcoPointPageViewModel
        {
            Summary = summary,
            History = history
        };

        return View(model);
    }
}


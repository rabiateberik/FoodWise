// EcoPointController, Web arayüzünde kullanıcının eco puan özetini ve puan geçmişini gösterir.

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

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (string.IsNullOrWhiteSpace(token))
            return RedirectToAction("Login", "Auth");

        // Kullanıcının toplam eco puanı, seviyesi ve puan geçmişi API üzerinden alınır.
        var summary = await _ecoPointWebService.GetSummaryAsync(token);
        var history = await _ecoPointWebService.GetHistoryAsync(token);

        var model = new EcoPointPageViewModel
        {
            Summary = summary,
            History = history
        };

        return View(model);
    }
}
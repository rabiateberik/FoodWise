// ProfileController, Web arayüzünde giriş yapan kullanıcının profil bilgilerini gösterir.
// Profil ekranında kullanıcı bilgilerine ek olarak eco puan özeti de gösterilir.

using FoodWise.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace FoodWise.Web.Controllers;

public class ProfileController : Controller
{
    private readonly IProfileWebService _profileWebService;
    private readonly IEcoPointWebService _ecoPointWebService;

    public ProfileController(
        IProfileWebService profileWebService,
        IEcoPointWebService ecoPointWebService)
    {
        _profileWebService = profileWebService;
        _ecoPointWebService = ecoPointWebService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (string.IsNullOrWhiteSpace(token))
            return RedirectToAction("Login", "Auth");

        // Giriş yapan kullanıcının profil bilgileri API üzerinden alınır.
        var profile = await _profileWebService.GetMyProfileAsync(token);

        if (profile == null)
        {
            TempData["ErrorMessage"] = "Profil bilgileri alınamadı.";
            return RedirectToAction("Index", "Home");
        }

        // Profil bilgilerine ek olarak kullanıcının eco puan özeti alınır.
        var ecoPointSummary = await _ecoPointWebService.GetSummaryAsync(token);

        profile.EcoPoint = ecoPointSummary.TotalPoint;
        profile.EcoPointLevelName = ecoPointSummary.LevelName;
        profile.EcoPointHistoryCount = ecoPointSummary.HistoryCount;

        return View(profile);
    }
}
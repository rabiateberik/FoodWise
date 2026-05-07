// ProfileController, Web arayüzünde giriş yapan kullanıcının profil bilgilerini gösterir.

using FoodWise.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace FoodWise.Web.Controllers;

public class ProfileController : Controller
{
    private readonly IProfileWebService _profileWebService;

    public ProfileController(IProfileWebService profileWebService)
    {
        _profileWebService = profileWebService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (string.IsNullOrWhiteSpace(token))
            return RedirectToAction("Login", "Auth");

        var profile = await _profileWebService.GetMyProfileAsync(token);

        if (profile == null)
        {
            TempData["ErrorMessage"] = "Profil bilgileri alınamadı.";
            return RedirectToAction("Index", "Home");
        }

        return View(profile);
    }
}
// Bu controller, FoodWise.Web tarafındaki kullanıcı giriş, kayıt ve çıkış işlemlerini yönetir.
// Kullanıcı bilgilerini AuthWebService aracılığıyla FoodWise.API'ye gönderir ve başarılı girişte JWT token bilgisini Session içinde saklar.

using FoodWise.Web.Services;
using FoodWise.Web.ViewModels.Auth;
using Microsoft.AspNetCore.Mvc;

namespace FoodWise.Web.Controllers;

public class AuthController : Controller
{
    private readonly IAuthWebService _authWebService;

    private const string TokenSessionKey = "JWToken";
    private const string UserIdSessionKey = "UserId";
    private const string FullNameSessionKey = "FullName";
    private const string EmailSessionKey = "Email";

    public AuthController(IAuthWebService authWebService)
    {
        _authWebService = authWebService;
    }

    [HttpGet]
    public IActionResult Login()
    {
        // Kullanıcı zaten giriş yaptıysa tekrar login ekranına dönmesin.
        var token = HttpContext.Session.GetString(TokenSessionKey);

        if (!string.IsNullOrWhiteSpace(token))
        {
            return RedirectToAction("Index", "Home");
        }

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _authWebService.LoginAsync(model);

        if (result == null || !result.Success || string.IsNullOrWhiteSpace(result.Token))
        {
            ModelState.AddModelError(string.Empty, result?.Message ?? "Giriş işlemi başarısız oldu.");
            return View(model);
        }

        // API'den gelen JWT token ve temel kullanıcı bilgileri Session içinde saklanır.
        SaveUserSession(result);

        TempData["SuccessMessage"] = "Giriş başarılı.";

        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult Register()
    {
        // Kullanıcı zaten giriş yaptıysa kayıt ekranına erişmesine gerek yoktur.
        var token = HttpContext.Session.GetString(TokenSessionKey);

        if (!string.IsNullOrWhiteSpace(token))
        {
            return RedirectToAction("Index", "Home");
        }

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _authWebService.RegisterAsync(model);

        if (result == null || !result.Success || string.IsNullOrWhiteSpace(result.Token))
        {
            ModelState.AddModelError(string.Empty, result?.Message ?? "Kayıt işlemi başarısız oldu.");
            return View(model);
        }

        // Kayıt başarılı olursa kullanıcı tekrar login yaptırılmadan oturumu başlatılır.
        SaveUserSession(result);

        TempData["SuccessMessage"] = "Kayıt başarılı. FoodWise'a hoş geldiniz.";

        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        // Kullanıcı çıkış yaptığında Session içindeki tüm bilgiler temizlenir.
        HttpContext.Session.Clear();

        TempData["SuccessMessage"] = "Çıkış işlemi başarılı.";

        return RedirectToAction("Login", "Auth");
    }

    private void SaveUserSession(AuthResponseViewModel result)
    {
        HttpContext.Session.SetString(TokenSessionKey, result.Token ?? string.Empty);
        HttpContext.Session.SetString(UserIdSessionKey, result.UserId ?? string.Empty);
        HttpContext.Session.SetString(FullNameSessionKey, result.FullName ?? string.Empty);
        HttpContext.Session.SetString(EmailSessionKey, result.Email ?? string.Empty);
    }
}
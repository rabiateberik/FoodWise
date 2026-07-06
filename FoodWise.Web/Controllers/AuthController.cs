
// AuthController, FoodWise.Web tarafındaki kullanıcı giriş, kayıt ve çıkış işlemlerini yönetir.
// Kullanıcı bilgileri doğrudan burada doğrulanmaz; AuthWebService aracılığıyla FoodWise.API'ye gönderilir.
// Başarılı giriş veya kayıt işleminden sonra API'den gelen JWT token Session içinde saklanır.

using FoodWise.Web.Services;
using FoodWise.Web.ViewModels.Auth;
using Microsoft.AspNetCore.Mvc;

namespace FoodWise.Web.Controllers;

public class AuthController : Controller
{
    private readonly IAuthWebService _authWebService;

    // Session içinde kullanılacak anahtarlar sabit olarak tutulur.
    // Böylece string değerlerin farklı yerlerde hatalı yazılması engellenir.
    private const string TokenSessionKey = "JWToken";
    private const string UserIdSessionKey = "UserId";
    private const string FullNameSessionKey = "FullName";
    private const string EmailSessionKey = "Email";

    public AuthController(IAuthWebService authWebService)
    {
        _authWebService = authWebService;
    }

    // Login sayfasını açar.
    // Kullanıcı zaten giriş yaptıysa tekrar login ekranına gönderilmez.
    [HttpGet]
    public IActionResult Login()
    {
        var token = HttpContext.Session.GetString(TokenSessionKey);

        if (!string.IsNullOrWhiteSpace(token))
        {
            return RedirectToAction("Index", "Home");
        }

        return View();
    }

    // Login formundan gelen bilgileri API'ye gönderir.
    // API başarılı cevap dönerse JWT token ve kullanıcı bilgileri Session içine kaydedilir.
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

    // Register sayfasını açar.
    // Kullanıcı zaten giriş yaptıysa kayıt ekranına erişmesine gerek yoktur.
    [HttpGet]
    public IActionResult Register()
    {
        var token = HttpContext.Session.GetString(TokenSessionKey);

        if (!string.IsNullOrWhiteSpace(token))
        {
            return RedirectToAction("Index", "Home");
        }

        return View();
    }

    // Kayıt formundan gelen bilgileri API'ye gönderir.
    // Kayıt başarılı olursa kullanıcı tekrar login yaptırılmadan oturum başlatılır.
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

        SaveUserSession(result);

        TempData["SuccessMessage"] = "Kayıt başarılı. FoodWise'a hoş geldiniz.";

        return RedirectToAction("Index", "Home");
    }

    // Kullanıcı çıkış yaptığında Session içindeki tüm bilgiler temizlenir.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();

        TempData["SuccessMessage"] = "Çıkış işlemi başarılı.";

        return RedirectToAction("Login", "Auth");
    }

    // API'den dönen kimlik bilgilerini Session içine kaydeder.
    // Diğer controller ve servisler JWT token'a buradan erişerek korumalı API endpointlerine istek atar.
    private void SaveUserSession(AuthResponseViewModel result)
    {
        HttpContext.Session.SetString(TokenSessionKey, result.Token ?? string.Empty);
        HttpContext.Session.SetString(UserIdSessionKey, result.UserId ?? string.Empty);
        HttpContext.Session.SetString(FullNameSessionKey, result.FullName ?? string.Empty);
        HttpContext.Session.SetString(EmailSessionKey, result.Email ?? string.Empty);
    }
}

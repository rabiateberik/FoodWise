
// ProfileController, Web arayüzünde giriş yapan kullanıcının profil sayfasını yönetir.
// Profil görüntüleme, profil güncelleme, şifre değiştirme ve hesap silme işlemleri burada karşılanır.
// Controller API'ye doğrudan gitmez; profil işlemleri IProfileWebService üzerinden FoodWise.API'ye gönderilir.

using FoodWise.Web.Services;
using FoodWise.Web.ViewModels.Profile;
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

    // Giriş yapan kullanıcının profil bilgilerini gösteren sayfayı açar.
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var token = HttpContext.Session.GetString("JWToken");

        // Token yoksa kullanıcı giriş yapmamış kabul edilir.
        if (string.IsNullOrWhiteSpace(token))
            return RedirectToAction("Login", "Auth");

        // Kullanıcının profil bilgileri API üzerinden alınır.
        var profile = await _profileWebService.GetMyProfileAsync(token);

        if (profile == null)
        {
            TempData["ErrorMessage"] = "Profil bilgileri alınamadı.";
            return RedirectToAction("Index", "Home");
        }

        // Profil ekranında gösterilecek eco puan özeti ayrıca API'den alınır.
        var ecoPointSummary = await _ecoPointWebService.GetSummaryAsync(token);

        // Eco puan bilgileri profil ViewModel'ine eklenerek sayfaya gönderilir.
        profile.EcoPoint = ecoPointSummary.TotalPoint;
        profile.EcoPointLevelName = ecoPointSummary.LevelName;
        profile.EcoPointHistoryCount = ecoPointSummary.HistoryCount;

        return View(profile);
    }

    // Kullanıcının profil bilgilerini güncelleme isteğini API'ye gönderir.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProfile(UpdateProfileViewModel model)
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (string.IsNullOrWhiteSpace(token))
            return RedirectToAction("Login", "Auth");

        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Profil bilgileri geçerli değil.";
            return RedirectToAction("Index");
        }

        var result = await _profileWebService.UpdateProfileAsync(model, token);

        if (!result)
        {
            TempData["ErrorMessage"] = "Profil bilgileri güncellenemedi.";
            return RedirectToAction("Index");
        }

        TempData["SuccessMessage"] = "Profil bilgileri başarıyla güncellendi.";
        return RedirectToAction("Index");
    }

    // Kullanıcının şifre değiştirme isteğini API'ye gönderir.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (string.IsNullOrWhiteSpace(token))
            return RedirectToAction("Login", "Auth");

        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Şifre bilgileri geçerli değil.";
            return RedirectToAction("Index");
        }

        var result = await _profileWebService.ChangePasswordAsync(model, token);

        if (!result)
        {
            TempData["ErrorMessage"] = "Şifre değiştirilemedi. Mevcut şifreni kontrol et.";
            return RedirectToAction("Index");
        }

        TempData["SuccessMessage"] = "Şifre başarıyla değiştirildi.";
        return RedirectToAction("Index");
    }

    // Kullanıcının hesabını pasif hale getirme isteğini API'ye gönderir.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAccount(DeleteAccountViewModel model)
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (string.IsNullOrWhiteSpace(token))
            return RedirectToAction("Login", "Auth");

        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Hesap silme bilgileri geçerli değil.";
            return RedirectToAction("Index");
        }

        // Yanlışlıkla hesap silmeyi önlemek için kullanıcının özel onay metnini yazması istenir.
        if (model.ConfirmText.Trim() != "HESABIMI SİL")
        {
            TempData["ErrorMessage"] = "Hesabı silmek için onay metnini doğru yazmalısın.";
            return RedirectToAction("Index");
        }

        var result = await _profileWebService.DeleteAccountAsync(model, token);

        if (!result)
        {
            TempData["ErrorMessage"] = "Hesap silinemedi. Mevcut şifreni kontrol et.";
            return RedirectToAction("Index");
        }

        // Hesap pasif hale getirildikten sonra kullanıcı oturumu temizlenir.
        HttpContext.Session.Clear();

        TempData["SuccessMessage"] = "Hesabın başarıyla pasif hale getirildi.";
        return RedirectToAction("Login", "Auth");
    }
}


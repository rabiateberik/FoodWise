
// NotificationController, Web arayüzünde kullanıcı bildirimleri sayfasını yönetir.
// Bildirim listeleme, okundu yapma, tümünü okundu yapma, silme ve test bildirimi oluşturma işlemleri burada karşılanır.
// Controller API'ye doğrudan gitmez; tüm bildirim işlemleri INotificationWebService üzerinden FoodWise.API'ye gönderilir.

using FoodWise.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace FoodWise.Web.Controllers;

public class NotificationController : Controller
{
    private readonly INotificationWebService _notificationWebService;

    public NotificationController(INotificationWebService notificationWebService)
    {
        _notificationWebService = notificationWebService;
    }

    // Kullanıcının bildirim listesini gösteren sayfayı açar.
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var token = HttpContext.Session.GetString("JWToken");

        // Token yoksa kullanıcı giriş yapmamış kabul edilir.
        if (string.IsNullOrWhiteSpace(token))
            return RedirectToAction("Login", "Auth");

        // Bildirim listesi ve okunmamış bildirim sayısı API'den alınır.
        var notifications = await _notificationWebService.GetMyNotificationsAsync(token);
        var unreadCount = await _notificationWebService.GetUnreadCountAsync(token);

        // Sayfada gösterilecek ek kullanıcı ve bildirim bilgileri ViewBag ile taşınır.
        ViewBag.UnreadCount = unreadCount;
        ViewBag.FullName = HttpContext.Session.GetString("FullName");
        ViewBag.Email = HttpContext.Session.GetString("Email");

        return View(notifications);
    }

    // Seçilen bildirimi okundu olarak işaretleme isteğini API'ye gönderir.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (string.IsNullOrWhiteSpace(token))
            return RedirectToAction("Login", "Auth");

        await _notificationWebService.MarkAsReadAsync(id, token);

        return RedirectToAction(nameof(Index));
    }

    // Kullanıcının tüm bildirimlerini okundu olarak işaretleme isteğini API'ye gönderir.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (string.IsNullOrWhiteSpace(token))
            return RedirectToAction("Login", "Auth");

        await _notificationWebService.MarkAllAsReadAsync(token);

        return RedirectToAction(nameof(Index));
    }

    // Seçilen bildirimi silme isteğini API'ye gönderir.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (string.IsNullOrWhiteSpace(token))
            return RedirectToAction("Login", "Auth");

        await _notificationWebService.DeleteAsync(id, token);

        return RedirectToAction(nameof(Index));
    }

    // Geliştirme ve test amacıyla örnek bildirim oluşturma isteğini API'ye gönderir.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateTest()
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (string.IsNullOrWhiteSpace(token))
            return RedirectToAction("Login", "Auth");

        var result = await _notificationWebService.CreateTestNotificationAsync(token);

        TempData["SuccessMessage"] = result
            ? "Test bildirimi başarıyla oluşturuldu."
            : "Test bildirimi oluşturulurken bir hata oluştu.";

        return RedirectToAction(nameof(Index));
    }
}


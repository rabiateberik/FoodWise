// NotificationController, Web arayüzünde kullanıcı bildirimlerini listeleme, okundu yapma ve silme işlemlerini yönetir.
// API ile doğrudan değil, INotificationWebService üzerinden haberleşir.

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

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (string.IsNullOrWhiteSpace(token))
            return RedirectToAction("Login", "Auth");

        var notifications = await _notificationWebService.GetMyNotificationsAsync(token);
        var unreadCount = await _notificationWebService.GetUnreadCountAsync(token);

        ViewBag.UnreadCount = unreadCount;
        ViewBag.FullName = HttpContext.Session.GetString("FullName");
        ViewBag.Email = HttpContext.Session.GetString("Email");

        return View(notifications);
    }

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
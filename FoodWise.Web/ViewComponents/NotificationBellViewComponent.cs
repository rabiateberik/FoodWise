// NotificationBellViewComponent, layout üzerinde kullanılan bildirim zilini hazırlar.
// Okunmamış bildirimleri öncelikli gösterir; okunmamış azsa son okunan bildirimlerle listeyi tamamlar.

using FoodWise.Web.Services;
using FoodWise.Web.ViewModels.Notification;
using Microsoft.AspNetCore.Mvc;

namespace FoodWise.Web.ViewComponents;

public class NotificationBellViewComponent : ViewComponent
{
    private readonly INotificationWebService _notificationWebService;

    public NotificationBellViewComponent(INotificationWebService notificationWebService)
    {
        _notificationWebService = notificationWebService;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (string.IsNullOrWhiteSpace(token))
        {
            return View(new NotificationBellViewModel());
        }

        // Kullanıcının tüm aktif bildirimleri API üzerinden alınır.
        var notifications = await _notificationWebService.GetMyNotificationsAsync(token);

        var unreadNotifications = notifications
            .Where(notification => !notification.IsRead)
            .OrderByDescending(notification => notification.CreatedAt)
            .ToList();

        // Okunmamış bildirim sayısı 5'ten azsa dropdown boş kalmasın diye son okunan bildirimlerle tamamlanır.
        var remainingCount = Math.Max(0, 5 - unreadNotifications.Count);

        var recentReadNotifications = notifications
            .Where(notification => notification.IsRead)
            .OrderByDescending(notification => notification.CreatedAt)
            .Take(remainingCount)
            .ToList();

        var dropdownNotifications = unreadNotifications
            .Concat(recentReadNotifications)
            .OrderByDescending(notification => notification.CreatedAt)
            .ToList();

        var model = new NotificationBellViewModel
        {
            UnreadCount = unreadNotifications.Count,
            RecentNotifications = dropdownNotifications
        };

        return View(model);
    }
}
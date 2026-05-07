// NotificationBellViewComponent, layout üzerinde kullanılan bildirim zilini hazırlar.
// Her sayfada ayrı ayrı bildirim verisi çekmek yerine, okunmamış bildirim sayısını ve son bildirimleri tek yerden alır.

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

        var unreadCount = await _notificationWebService.GetUnreadCountAsync(token);
        var notifications = await _notificationWebService.GetMyNotificationsAsync(token);

        var model = new NotificationBellViewModel
        {
            UnreadCount = unreadCount,
            RecentNotifications = notifications
                .OrderByDescending(notification => notification.CreatedAt)
                .Take(5)
                .ToList()
        };

        return View(model);
    }
}
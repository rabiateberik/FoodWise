
// NotificationBellViewComponent, layout üzerinde kullanılan bildirim zili alanını hazırlar.
// Kullanıcının bildirimlerini API'den alır ve dropdown içinde gösterilecek listeyi oluşturur.
// Okunmamış bildirimler öncelikli gösterilir; okunmamış bildirim azsa liste son okunan bildirimlerle tamamlanır.

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

    // Layout içinde bildirim zili çağrıldığında çalışır.
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var token = HttpContext.Session.GetString("JWToken");

        // Kullanıcı giriş yapmamışsa boş bildirim modeli döndürülür.
        if (string.IsNullOrWhiteSpace(token))
        {
            return View(new NotificationBellViewModel());
        }

        // Kullanıcının aktif bildirimleri API üzerinden alınır.
        var notifications = await _notificationWebService.GetMyNotificationsAsync(token);

        // Okunmamış bildirimler en yeniden eskiye doğru sıralanır.
        var unreadNotifications = notifications
            .Where(notification => !notification.IsRead)
            .OrderByDescending(notification => notification.CreatedAt)
            .ToList();

        // Dropdown içinde en fazla 5 bildirim göstermek için eksik kalan sayı hesaplanır.
        var remainingCount = Math.Max(0, 5 - unreadNotifications.Count);

        // Okunmamış bildirim sayısı 5'ten azsa liste son okunan bildirimlerle tamamlanır.
        var recentReadNotifications = notifications
            .Where(notification => notification.IsRead)
            .OrderByDescending(notification => notification.CreatedAt)
            .Take(remainingCount)
            .ToList();

        // Okunmamış ve son okunan bildirimler tek listede birleştirilir.
        var dropdownNotifications = unreadNotifications
            .Concat(recentReadNotifications)
            .OrderByDescending(notification => notification.CreatedAt)
            .ToList();

        // Bildirim zili üzerinde gösterilecek sayı ve dropdown listesi hazırlanır.
        var model = new NotificationBellViewModel
        {
            UnreadCount = unreadNotifications.Count,
            RecentNotifications = dropdownNotifications
        };

        return View(model);
    }
}

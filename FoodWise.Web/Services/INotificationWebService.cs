// Bu interface, FoodWise.Web projesinin Notification API ile haberleşmesi için gereken metotları tanımlar.
// Controller doğrudan HttpClient kullanmaz; bildirim işlemleri bu servis üzerinden yapılır.

using FoodWise.Web.ViewModels.Notification;

namespace FoodWise.Web.Services;

public interface INotificationWebService
{
    Task<List<NotificationViewModel>> GetMyNotificationsAsync(string token);

    Task<int> GetUnreadCountAsync(string token);

    Task<bool> MarkAsReadAsync(int notificationId, string token);

    Task<bool> MarkAllAsReadAsync(string token);

    Task<bool> DeleteAsync(int notificationId, string token);

    Task<bool> CreateTestNotificationAsync(string token);
}
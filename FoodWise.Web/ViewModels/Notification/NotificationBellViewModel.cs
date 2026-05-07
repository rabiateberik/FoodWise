// Bu ViewModel, layout üzerinde gösterilen bildirim zili için gerekli verileri taşır.
// Okunmamış bildirim sayısı ve son bildirimler burada tutulur.

namespace FoodWise.Web.ViewModels.Notification;

public class NotificationBellViewModel
{
    public int UnreadCount { get; set; }

    public List<NotificationViewModel> RecentNotifications { get; set; } = new();
}
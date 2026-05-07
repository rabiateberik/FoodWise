// Bu ViewModel, Dashboard ekranında gösterilecek özet verileri taşır.
// Stok, riskli ürün ve okunmamış bildirim sayıları bu model üzerinden ekrana basılır.

using FoodWise.Web.ViewModels.Notification;

namespace FoodWise.Web.ViewModels.Home;

public class DashboardViewModel
{
    public string FullName { get; set; } = "Kullanıcı";

    public string Email { get; set; } = string.Empty;

    public int TotalStockCount { get; set; }

    public int RiskyStockCount { get; set; }

    public int UnreadNotificationCount { get; set; }

    public decimal CarbonSavedKg { get; set; }
    public List<NotificationViewModel> RecentNotifications { get; set; } = new();
}

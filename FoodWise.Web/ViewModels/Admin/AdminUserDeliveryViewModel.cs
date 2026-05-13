// AdminUserDeliveryViewModel, admin panelinde kullanıcının teslimat geçmişini göstermek için kullanılır.

namespace FoodWise.Web.ViewModels.Admin;

public class AdminUserDeliveryViewModel
{
    public int Id { get; set; }

    public string Role { get; set; } = string.Empty;

    public string ProductName { get; set; } = string.Empty;

    public decimal Quantity { get; set; }

    public string UnitName { get; set; } = string.Empty;

    public string? DeliveryPointName { get; set; }

    public string? BoxCode { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }

    public DateTime? DroppedOffAt { get; set; }

    public DateTime? DeliveredAt { get; set; }

    public DateTime CreatedAt { get; set; }
}
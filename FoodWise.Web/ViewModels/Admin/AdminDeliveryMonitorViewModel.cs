// AdminDeliveryMonitorViewModel, admin panelinde sistem genelindeki teslimatları göstermek için kullanılır.

namespace FoodWise.Web.ViewModels.Admin;

public class AdminDeliveryMonitorViewModel
{
    public int Id { get; set; }

    public string DonorUserId { get; set; } = string.Empty;

    public string DonorFullName { get; set; } = string.Empty;

    public string DonorEmail { get; set; } = string.Empty;

    public string ReceiverUserId { get; set; } = string.Empty;

    public string ReceiverFullName { get; set; } = string.Empty;

    public string ReceiverEmail { get; set; } = string.Empty;

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
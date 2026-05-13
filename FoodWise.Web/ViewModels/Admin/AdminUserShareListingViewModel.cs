// AdminUserShareListingViewModel, admin panelinde kullanıcının paylaşım ilanlarını göstermek için kullanılır.

namespace FoodWise.Web.ViewModels.Admin;

public class AdminUserShareListingViewModel
{
    public int Id { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public decimal Quantity { get; set; }

    public string UnitName { get; set; } = string.Empty;

    public string? DeliveryPointName { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}
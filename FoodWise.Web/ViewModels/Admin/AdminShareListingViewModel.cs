// AdminShareListingViewModel, admin panelinde sistem genelindeki paylaşım ilanlarını göstermek için kullanılır.

namespace FoodWise.Web.ViewModels.Admin;

public class AdminShareListingViewModel
{
    public int Id { get; set; }

    public string DonorUserId { get; set; } = string.Empty;

    public string DonorFullName { get; set; } = string.Empty;

    public string DonorEmail { get; set; } = string.Empty;

    public string ProductName { get; set; } = string.Empty;

    public decimal Quantity { get; set; }

    public string UnitName { get; set; } = string.Empty;

    public string? DeliveryPointName { get; set; }

    public string? City { get; set; }

    public string? District { get; set; }

    public string Status { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }
}
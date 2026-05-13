// AdminUserViewModel, admin panelinde kullanıcı bilgilerini göstermek için kullanılır.

namespace FoodWise.Web.ViewModels.Admin;

public class AdminUserViewModel
{
    public string Id { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? City { get; set; }

    public string? District { get; set; }

    public string? Neighborhood { get; set; }

    public int NeedScore { get; set; }

    public int ReliabilityScore { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public List<string> Roles { get; set; } = new();

    public int StockItemCount { get; set; }

    public int ShareListingCount { get; set; }

    public int ActiveShareListingCount { get; set; }

    public int DonatedDeliveryCount { get; set; }

    public int ReceivedDeliveryCount { get; set; }

    public int CompletedDonatedDeliveryCount { get; set; }

    public int CompletedReceivedDeliveryCount { get; set; }

    public int ExpiredDeliveryCount { get; set; }

    public int TotalEcoPoint { get; set; }

    public decimal TotalCarbonSavedKg { get; set; }
}
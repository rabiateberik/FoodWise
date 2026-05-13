// AdminUserStockViewModel, admin panelinde bir kullanıcıya ait stokları göstermek için kullanılır.

namespace FoodWise.Web.ViewModels.Admin;

public class AdminUserStockViewModel
{
    public int Id { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public decimal Quantity { get; set; }

    public string UnitName { get; set; } = string.Empty;

    public DateTime ExpirationDate { get; set; }

    public string Status { get; set; } = string.Empty;

    public string? RiskLevel { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }
}
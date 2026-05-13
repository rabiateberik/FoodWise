// AdminUserStockDto, admin panelinde bir kullanıcıya ait stok kayıtlarını göstermek için kullanılır.

namespace FoodWise.Application.DTOs.Admin;

public class AdminUserStockDto
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
// AdminUserShareListingDto, admin panelinde bir kullanıcının oluşturduğu paylaşım ilanlarını göstermek için kullanılır.

namespace FoodWise.Application.DTOs.Admin;

public class AdminUserShareListingDto
{
    public int Id { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public decimal Quantity { get; set; }

    public string UnitName { get; set; } = string.Empty;

    public string? DeliveryPointName { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}
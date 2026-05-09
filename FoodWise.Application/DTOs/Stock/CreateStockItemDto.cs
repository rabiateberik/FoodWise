// Kullanıcının stoğa yeni ürün eklerken göndereceği verileri temsil eder.
// Ürün sistemde varsa ProductId kullanılır.
// Ürün sistemde yoksa ProductName ile yeni ürün otomatik oluşturulur.

using FoodWise.Domain.Enums;

namespace FoodWise.Application.DTOs.Stock;

public class CreateStockItemDto
{
    // Ürün sistemde varsa mevcut ProductId gönderilir.
    // Kullanıcı yeni ürün yazdıysa null veya 0 gelebilir.
    public int? ProductId { get; set; }

    // Kullanıcı listede olmayan bir ürün yazarsa bu alan kullanılır.
    public string? ProductName { get; set; }

    public int UnitId { get; set; }

    public decimal Quantity { get; set; }

    public DateTime ExpirationDate { get; set; }

    public DateTime? OpenedDate { get; set; }

    public StorageCondition StorageCondition { get; set; }

    public string? ImageUrl { get; set; }

    public string? Note { get; set; }
}
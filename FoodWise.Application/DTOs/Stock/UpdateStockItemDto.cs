// Kullanıcının stok ürününü güncellerken göndereceği verileri temsil eder.
// Ürün sistemde varsa ProductName üzerinden bulunur, yoksa yeni ürün olarak oluşturulabilir.

using FoodWise.Domain.Enums;

namespace FoodWise.Application.DTOs.Stock;

public class UpdateStockItemDto
{
    public int? ProductId { get; set; }

    public string? ProductName { get; set; }

    public int UnitId { get; set; }

    public decimal Quantity { get; set; }

    public DateTime ExpirationDate { get; set; }

    public DateTime? OpenedDate { get; set; }

    public StorageCondition StorageCondition { get; set; }

    public string? ImageUrl { get; set; }

    public string? Note { get; set; }
}
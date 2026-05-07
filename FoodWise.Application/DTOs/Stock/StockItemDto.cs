using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Stok ürünlerini API cevabı olarak döndürmek için kullanılan modeldir.
// Ürün, birim ve risk bilgilerini kullanıcıya okunabilir şekilde taşır.
namespace FoodWise.Application.DTOs.Stock;

public class StockItemDto
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public string ProductName { get; set; } = null!;

    public int UnitId { get; set; }

    public string UnitName { get; set; } = null!;

    public decimal Quantity { get; set; }

    public DateTime ExpirationDate { get; set; }

    public DateTime? OpenedDate { get; set; }

    public string StorageCondition { get; set; } = null!;

    public string Status { get; set; } = null!;

    public int? RiskScore { get; set; }

    public string? RiskLevel { get; set; }

    public string? RecommendationText { get; set; }

    public string? Note { get; set; }
}
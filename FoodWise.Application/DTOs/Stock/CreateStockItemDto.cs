using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Kullanıcının stoğa yeni ürün eklerken göndereceği verileri temsil eder.
using FoodWise.Domain.Enums;

namespace FoodWise.Application.DTOs.Stock;

public class CreateStockItemDto
{
    public int ProductId { get; set; }

    public int UnitId { get; set; }

    public decimal Quantity { get; set; }

    public DateTime ExpirationDate { get; set; }

    public DateTime? OpenedDate { get; set; }

    public StorageCondition StorageCondition { get; set; }

    public string? ImageUrl { get; set; }

    public string? Note { get; set; }
}
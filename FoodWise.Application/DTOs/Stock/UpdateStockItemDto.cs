using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Kullanıcının mevcut stok ürününü güncellerken göndereceği verileri temsil eder.
using FoodWise.Domain.Enums;

namespace FoodWise.Application.DTOs.Stock;

public class UpdateStockItemDto
{
    public decimal Quantity { get; set; }

    public DateTime ExpirationDate { get; set; }

    public DateTime? OpenedDate { get; set; }

    public StorageCondition StorageCondition { get; set; }

    public string? ImageUrl { get; set; }

    public string? Note { get; set; }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Kullanıcının stokundaki bir ürünü paylaşıma açarken göndereceği verileri temsil eder.
namespace FoodWise.Application.DTOs.Sharing;

public class CreateShareListingDto
{
    public int StockItemId { get; set; }

    public int DeliveryPointId { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public decimal Quantity { get; set; }

    public DateTime PickupStartTime { get; set; }

    public DateTime PickupEndTime { get; set; }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Paylaşım ilanlarını API cevabı olarak göstermek için kullanılan modeldir.
namespace FoodWise.Application.DTOs.Sharing;

public class ShareListingDto
{
    public int Id { get; set; }

    public int StockItemId { get; set; }

    public string ProductName { get; set; } = null!;

    public decimal Quantity { get; set; }

    public string UnitName { get; set; } = null!;

    public string DonorUserId { get; set; } = null!;

    public int DeliveryPointId { get; set; }

    public string DeliveryPointName { get; set; } = null!;

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime PickupStartTime { get; set; }

    public DateTime PickupEndTime { get; set; }

    public string Status { get; set; } = null!;

    public int RequestCount { get; set; }
}
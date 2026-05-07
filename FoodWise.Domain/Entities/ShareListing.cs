using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FoodWise.Domain.Common;
using FoodWise.Domain.Enums;

namespace FoodWise.Domain.Entities;

public class ShareListing : BaseEntity
{
    public int StockItemId { get; set; }

    public string DonorUserId { get; set; } = null!;

    public int DeliveryPointId { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public decimal Quantity { get; set; }

    public DateTime PickupStartTime { get; set; }

    public DateTime PickupEndTime { get; set; }

    public ShareListingStatus Status { get; set; } = ShareListingStatus.Available;

    public StockItem StockItem { get; set; } = null!;

    public DeliveryPoint DeliveryPoint { get; set; } = null!;

    public ICollection<ShareRequest> ShareRequests { get; set; } = new List<ShareRequest>();

    public Delivery? Delivery { get; set; }
}
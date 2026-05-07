using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FoodWise.Domain.Common;

namespace FoodWise.Domain.Entities;

public class DeliveryPoint : BaseEntity
{
    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string? Neighborhood { get; set; }

    public double? Latitude { get; set; }

    public double? Longitude { get; set; }

    public string? WorkingHours { get; set; }

    public string? StorageType { get; set; }

    public ICollection<ShareListing> ShareListings { get; set; } = new List<ShareListing>();

    public ICollection<Delivery> Deliveries { get; set; } = new List<Delivery>();
    // Bu teslim noktasına ait kutu/bölme kayıtlarını tutar.
public ICollection<DeliveryBox> DeliveryBoxes { get; set; } = new List<DeliveryBox>();
}

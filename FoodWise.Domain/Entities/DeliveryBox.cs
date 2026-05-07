using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// DeliveryBox, kontrollü teslim noktasında bulunan fiziksel kutu/bölmeyi temsil eder.
// Her kutunun sabit bir QR değeri vardır ve alıcı bu QR ile teslimatı doğrular.
using FoodWise.Domain.Common;

namespace FoodWise.Domain.Entities;

public class DeliveryBox : BaseEntity
{
    public int DeliveryPointId { get; set; }

    public string BoxCode { get; set; } = null!;

    public string QrCodeValue { get; set; } = null!;

    public string? Description { get; set; }

    public bool IsOccupied { get; set; } = false;

    public DeliveryPoint DeliveryPoint { get; set; } = null!;

    public ICollection<Delivery> Deliveries { get; set; } = new List<Delivery>();
}
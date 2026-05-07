using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Teslimat bilgilerini API cevabı olarak döndürmek için kullanılan modeldir.
// Ürün, teslim noktası, kutu ve teslimat durumu bilgilerini taşır.
namespace FoodWise.Application.DTOs.Delivery;

public class DeliveryDto
{
    public int Id { get; set; }

    public int ShareListingId { get; set; }

    public int ShareRequestId { get; set; }

    public string DonorUserId { get; set; } = null!;

    public string ReceiverUserId { get; set; } = null!;

    public int DeliveryPointId { get; set; }

    public string DeliveryPointName { get; set; } = null!;

    public int? DeliveryBoxId { get; set; }

    public string? BoxCode { get; set; }

    public string? BoxQrCodeValue { get; set; }

    public string ProductName { get; set; } = null!;

    public decimal Quantity { get; set; }

    public string UnitName { get; set; } = null!;

    public string Status { get; set; } = null!;

    public DateTime? DroppedOffAt { get; set; }

    public DateTime? PickedUpAt { get; set; }

    public DateTime? DeliveredAt { get; set; }

    public DateTime ExpiresAt { get; set; }

    public string? DropOffImageUrl { get; set; }
}
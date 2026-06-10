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
    // Giriş yapan kullanıcının bu ilana aktif talebi varsa true olur.
    public bool HasCurrentUserRequest { get; set; }

    // Giriş yapan kullanıcının talep Id bilgisidir.
    public int? CurrentUserRequestId { get; set; }

    // Giriş yapan kullanıcının talep durumudur.
    public string? CurrentUserRequestStatus { get; set; }
    // Teslim noktasının bölgesel konum bilgileri.
    // Açık adres tutulmaz; sadece şehir, ilçe ve mahalle bilgisi gönderilir.
    public string? City { get; set; }

    public string? District { get; set; }

    public string? Neighborhood { get; set; }

    // Giriş yapan kullanıcının konumuna göre teslim noktasının yakınlık önceliğidir.
    // 1: Aynı mahalle, 2: Aynı ilçe, 3: Aynı şehir, 99: Diğer bölge
    public int LocationPriority { get; set; } = 99;

    public string LocationMatchText { get; set; } = "Diğer bölge";
}
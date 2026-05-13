// DeliveryPoint, FoodWise sistemindeki güvenli teslim noktalarını temsil eder.
// Açık adres tutulmaz; şehir, ilçe ve mahalle bazlı konum bilgisi saklanır.

using FoodWise.Domain.Common;

namespace FoodWise.Domain.Entities;

public class DeliveryPoint : BaseEntity
{
    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    // Teslim noktasının bulunduğu şehir.
    // Örnek: Ankara, İstanbul, Kayseri
    public string? City { get; set; }

    // Teslim noktasının bulunduğu ilçe.
    // Örnek: Çankaya, Melikgazi, Kadıköy
    public string? District { get; set; }

    // Teslim noktasının mahalle veya bölge bilgisi.
    // Açık adres yerine bölgesel bilgi kullanılır.
    public string? Neighborhood { get; set; }

    public double? Latitude { get; set; }

    public double? Longitude { get; set; }

    public string? WorkingHours { get; set; }

    public string? StorageType { get; set; }

    public ICollection<ShareListing> ShareListings { get; set; } = new List<ShareListing>();

    public ICollection<Delivery> Deliveries { get; set; } = new List<Delivery>();

    // Bu teslim noktasına ait QR destekli teslim kutularını tutar.
    public ICollection<DeliveryBox> DeliveryBoxes { get; set; } = new List<DeliveryBox>();
}
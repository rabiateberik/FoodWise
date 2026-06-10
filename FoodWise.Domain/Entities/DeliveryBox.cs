// DeliveryBox, kontrollü teslim noktasında bulunan QR destekli ortak teslim kutusunu/bölmesini temsil eder.
// Bir teslim kutusu tek ürünlük değildir; aynı kutuya birden fazla aktif teslimat atanabilir.
// Alıcı, kutunun QR değerini doğrulayarak kendisine ait teslimatı teslim alır.

using FoodWise.Domain.Common;

namespace FoodWise.Domain.Entities;

public class DeliveryBox : BaseEntity
{
    public int DeliveryPointId { get; set; }

    public string BoxCode { get; set; } = null!;

    public string QrCodeValue { get; set; } = null!;

    public string? Description { get; set; }

    // Eski dolu/boş kutu mantığı için tutulmuş alandır.
    // FoodWise teslimat akışında artık kutular ortak kullanım mantığıyla çalışır.
    // Bu nedenle teslimat oluştururken IsOccupied kontrolü yapılmaz.
    public bool IsOccupied { get; set; } = false;

    public DeliveryPoint DeliveryPoint { get; set; } = null!;

    public ICollection<Delivery> Deliveries { get; set; } = new List<Delivery>();
}
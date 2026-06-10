// AdminDeliveryBoxViewModel, admin panelinde QR destekli teslim kutularını listelemek için kullanılır.
// FoodWise teslim kutuları ortak kullanım mantığıyla çalışır;
// yani bir kutuya birden fazla aktif teslimat atanabilir.

namespace FoodWise.Web.ViewModels.Admin;

public class AdminDeliveryBoxViewModel
{
    public int Id { get; set; }

    public int DeliveryPointId { get; set; }

    public string DeliveryPointName { get; set; } = string.Empty;

    public string? City { get; set; }

    public string? District { get; set; }

    public string? Neighborhood { get; set; }

    public string BoxCode { get; set; } = string.Empty;

    public string QrCodeValue { get; set; } = string.Empty;

    public string? Description { get; set; }

    // Eski dolu/boş kutu mantığından kalan alandır.
    // Yeni teslimat akışında kutular ortak QR bölmesi gibi çalıştığı için
    // teslimat oluştururken bu alan kullanılmaz.
    public bool IsOccupied { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public string LocationText
    {
        get
        {
            var values = new[]
            {
                Neighborhood,
                District,
                City
            }
            .Where(x => !string.IsNullOrWhiteSpace(x));

            var result = string.Join(" / ", values);

            return string.IsNullOrWhiteSpace(result)
                ? "Konum bilgisi yok"
                : result;
        }
    }

    public string ActiveStatusText =>
        IsActive ? "Aktif" : "Pasif";

    public string UsageTypeText =>
        "Ortak QR kutusu";

    public string SafeDescription =>
        string.IsNullOrWhiteSpace(Description)
            ? "Açıklama bulunmuyor."
            : Description;
}
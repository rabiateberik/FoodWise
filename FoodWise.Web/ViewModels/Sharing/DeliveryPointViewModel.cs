// DeliveryPointViewModel, paylaşım ilanı oluştururken seçilecek güvenli teslim noktasını temsil eder.
// API'den gelen yakınlık bilgileri sayesinde kullanıcıya aynı mahalle / aynı ilçe / aynı şehir önceliği gösterilir.

namespace FoodWise.Web.ViewModels.Sharing;

public class DeliveryPointViewModel
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? City { get; set; }

    public string? District { get; set; }

    public string? Neighborhood { get; set; }

    public string? WorkingHours { get; set; }

    public string? StorageType { get; set; }

    public double? Latitude { get; set; }

    public double? Longitude { get; set; }

    // 1: Aynı mahalle, 2: Aynı ilçe, 3: Aynı şehir, 99: Diğer bölge
    public int LocationPriority { get; set; } = 99;

    // Kullanıcıya gösterilecek yakınlık metni.
    // Örnek: Aynı mahalle, Aynı ilçe, Aynı şehir
    public string LocationMatchText { get; set; } = "Diğer bölge";

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

    public string DetailText
    {
        get
        {
            var values = new List<string>();

            if (!string.IsNullOrWhiteSpace(WorkingHours))
                values.Add($"Saat: {WorkingHours}");

            if (!string.IsNullOrWhiteSpace(StorageType))
                values.Add($"Saklama: {StorageType}");

            return values.Any()
                ? string.Join(" • ", values)
                : "Detay bilgisi yok";
        }
    }
}
// Bu ViewModel, kullanıcının oluşturduğu ve görüntülediği paylaşım ilanlarını
// Web arayüzünde göstermek için kullanılır.
// Teslim noktası konum bilgileri sayesinde ilan kartlarında
// aynı mahalle / aynı ilçe / aynı şehir etiketi gösterilebilir.

namespace FoodWise.Web.ViewModels.Sharing;

public class ShareListingViewModel
{
    public int Id { get; set; }

    public int StockItemId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public decimal Quantity { get; set; }

    public string UnitName { get; set; } = string.Empty;

    public string DonorUserId { get; set; } = string.Empty;

    public string? DonorFullName { get; set; }

    public int DeliveryPointId { get; set; }

    public string DeliveryPointName { get; set; } = string.Empty;

    // Teslim noktasının bölgesel konum bilgileri.
    // Açık adres tutulmaz; sadece şehir, ilçe ve mahalle bilgisi gösterilir.
    public string? City { get; set; }

    public string? District { get; set; }

    public string? Neighborhood { get; set; }

    // 1: Aynı mahalle, 2: Aynı ilçe, 3: Aynı şehir, 99: Diğer bölge
    public int LocationPriority { get; set; } = 99;

    public string LocationMatchText { get; set; } = "Diğer bölge";

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTime PickupStartTime { get; set; }

    public DateTime PickupEndTime { get; set; }

    public string Status { get; set; } = string.Empty;

    public int RequestCount { get; set; }

    // Kullanıcı bu ilana daha önce aktif talep gönderdiyse true olur.
    public bool HasCurrentUserRequest { get; set; }

    // Kullanıcının aktif talep Id bilgisidir.
    public int? CurrentUserRequestId { get; set; }

    // Kullanıcının aktif talep durumudur.
    public string? CurrentUserRequestStatus { get; set; }

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

    public string SafeLocationMatchText
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(LocationMatchText))
                return LocationMatchText;

            return LocationPriority switch
            {
                1 => "Aynı mahalle",
                2 => "Aynı ilçe",
                3 => "Aynı şehir",
                _ => "Diğer bölge"
            };
        }
    }

    public string QuantityText => $"{Quantity:0.##} {UnitName}";

    public bool HasCurrentUserPendingRequest =>
        HasCurrentUserRequest &&
        string.Equals(CurrentUserRequestStatus, "Pending", StringComparison.OrdinalIgnoreCase);

    public bool HasCurrentUserApprovedRequest =>
        HasCurrentUserRequest &&
        string.Equals(CurrentUserRequestStatus, "Approved", StringComparison.OrdinalIgnoreCase);
}
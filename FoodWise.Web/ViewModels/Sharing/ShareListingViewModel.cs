// Bu ViewModel, kullanıcının oluşturduğu paylaşım ilanlarını Web arayüzünde göstermek için kullanılır.

namespace FoodWise.Web.ViewModels.Sharing;

public class ShareListingViewModel
{
    public int Id { get; set; }

    public int StockItemId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public decimal Quantity { get; set; }

    public string UnitName { get; set; } = string.Empty;

    public string DonorUserId { get; set; } = string.Empty;

    public int DeliveryPointId { get; set; }

    public string DeliveryPointName { get; set; } = string.Empty;

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
}
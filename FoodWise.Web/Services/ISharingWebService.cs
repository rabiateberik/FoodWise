// Bu interface, FoodWise.Web projesinin Sharing API ile haberleşmesi için gereken metotları tanımlar.
// Controller doğrudan HttpClient kullanmaz; paylaşım ve teslim noktası işlemleri bu servis üzerinden yapılır.

using FoodWise.Web.ViewModels.Sharing;

namespace FoodWise.Web.Services;

public interface ISharingWebService
{
    Task<bool> CreateListingAsync(CreateShareListingViewModel model, string token);

    Task<List<ShareListingViewModel>> GetMyListingsAsync(string token);

    Task<List<ShareListingViewModel>> GetAvailableListingsAsync(string token);

    Task<bool> CreateRequestAsync(int listingId, string token);

    Task<List<ShareRequestViewModel>> GetRequestsForListingAsync(int listingId, string token);

    Task<bool> ApproveRequestAsync(int requestId, string token);

    Task<bool> RejectRequestAsync(int requestId, string token);

    // Kullanıcının kendi paylaşım ilanını iptal etmesi için kullanılır.
    Task<bool> CancelListingAsync(int listingId, string token);

    // Kullanıcının kendi bekleyen talebini iptal etmesi için kullanılır.
    Task<bool> CancelRequestAsync(int requestId, string token);

    // Kullanıcının kayıtlı konumuna göre yakın teslim noktalarını getirir.
    // Arama metni gönderilirse teslim noktası adı/açıklama/konum bilgisine göre filtreleme yapılır.
    Task<List<DeliveryPointViewModel>> GetDeliveryPointsAsync(string token, string? search = null);
}

// SharingWebService, FoodWise.Web ile FoodWise.API arasındaki paylaşım işlemleri bağlantısını yönetir.
// Paylaşım ilanı oluşturma, ilan listeleme, talep gönderme, talep onaylama/reddetme ve teslim noktası listeleme işlemleri API üzerinden yapılır.

using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FoodWise.Web.ViewModels.Sharing;

namespace FoodWise.Web.Services;

public class SharingWebService : ISharingWebService
{
    private readonly HttpClient _httpClient;

    public SharingWebService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;

        // Backend API adresi appsettings.json içindeki ApiSettings:BaseUrl değerinden okunur.
        var apiBaseUrl = configuration["ApiSettings:BaseUrl"];

        if (string.IsNullOrWhiteSpace(apiBaseUrl))
            throw new InvalidOperationException("ApiSettings:BaseUrl appsettings.json içinde tanımlı olmalıdır.");

        // HttpClient isteklerinin FoodWise.API adresine gönderilmesi sağlanır.
        _httpClient.BaseAddress = new Uri(apiBaseUrl.TrimEnd('/') + "/");
    }

    // Kullanıcının stok ürününü paylaşım ilanı olarak oluşturma isteğini API'ye gönderir.
    public async Task<bool> CreateListingAsync(CreateShareListingViewModel model, string token)
    {
        SetBearerToken(token);

        // API'nin beklediği alanlar istek modeli olarak hazırlanır.
        var requestModel = new
        {
            model.StockItemId,
            model.DeliveryPointId,
            model.Title,
            model.Description,
            model.Quantity,
            model.PickupStartTime,
            model.PickupEndTime
        };

        var response = await _httpClient.PostAsJsonAsync("api/sharing/listings", requestModel);

        return response.IsSuccessStatusCode;
    }

    // Kullanıcının kendi paylaşım ilanlarını API'den getirir.
    public async Task<List<ShareListingViewModel>> GetMyListingsAsync(string token)
    {
        SetBearerToken(token);

        var response = await _httpClient.GetAsync("api/sharing/listings/my");

        if (!response.IsSuccessStatusCode)
            return new List<ShareListingViewModel>();

        var result = await response.Content.ReadFromJsonAsync<List<ShareListingViewModel>>(GetJsonOptions());

        return result ?? new List<ShareListingViewModel>();
    }

    // Kullanıcının görüntüleyebileceği aktif paylaşım ilanlarını API'den getirir.
    public async Task<List<ShareListingViewModel>> GetAvailableListingsAsync(string token)
    {
        SetBearerToken(token);

        var response = await _httpClient.GetAsync("api/sharing/listings/available");

        if (!response.IsSuccessStatusCode)
            return new List<ShareListingViewModel>();

        var result = await response.Content.ReadFromJsonAsync<List<ShareListingViewModel>>(GetJsonOptions());

        return result ?? new List<ShareListingViewModel>();
    }

    // Seçilen paylaşım ilanına talep gönderme isteğini API'ye iletir.
    public async Task<bool> CreateRequestAsync(int listingId, string token)
    {
        SetBearerToken(token);

        var response = await _httpClient.PostAsync($"api/sharing/listings/{listingId}/request", null);

        return response.IsSuccessStatusCode;
    }

    // İlan sahibinin kendi ilanına gelen talepleri API'den getirir.
    public async Task<List<ShareRequestViewModel>> GetRequestsForListingAsync(int listingId, string token)
    {
        SetBearerToken(token);

        var response = await _httpClient.GetAsync($"api/sharing/listings/{listingId}/requests");

        if (!response.IsSuccessStatusCode)
            return new List<ShareRequestViewModel>();

        var result = await response.Content.ReadFromJsonAsync<List<ShareRequestViewModel>>(GetJsonOptions());

        return result ?? new List<ShareRequestViewModel>();
    }

    // İlan sahibinin gelen paylaşım talebini onaylaması için API'ye istek gönderir.
    public async Task<bool> ApproveRequestAsync(int requestId, string token)
    {
        SetBearerToken(token);

        var response = await _httpClient.PostAsync($"api/sharing/requests/{requestId}/approve", null);

        return response.IsSuccessStatusCode;
    }

    // İlan sahibinin gelen paylaşım talebini reddetmesi için API'ye istek gönderir.
    public async Task<bool> RejectRequestAsync(int requestId, string token)
    {
        SetBearerToken(token);

        var response = await _httpClient.PostAsync($"api/sharing/requests/{requestId}/reject", null);

        return response.IsSuccessStatusCode;
    }

    // Kullanıcının kendi paylaşım ilanını iptal etmesi için API'ye DELETE isteği gönderir.
    public async Task<bool> CancelListingAsync(int listingId, string token)
    {
        SetBearerToken(token);

        var response = await _httpClient.DeleteAsync($"api/sharing/listings/{listingId}");

        if (!response.IsSuccessStatusCode)
        {
            var errorMessage = await response.Content.ReadAsStringAsync();

            // Geliştirme aşamasında iptal hatasını terminal/Output ekranında görmek için yazdırılır.
            Console.WriteLine($"Sharing cancel failed. Status: {response.StatusCode}, Error: {errorMessage}");

            return false;
        }

        return true;
    }

    // Talep sahibinin kendi bekleyen talebini iptal etmesi için API'ye istek gönderir.
    public async Task<bool> CancelRequestAsync(int requestId, string token)
    {
        SetBearerToken(token);

        var response = await _httpClient.PostAsync($"api/sharing/requests/{requestId}/cancel", null);

        return response.IsSuccessStatusCode;
    }

    // Paylaşım ilanı oluştururken seçilecek teslim noktalarını API'den getirir.
    public async Task<List<DeliveryPointViewModel>> GetDeliveryPointsAsync(string token, string? search = null)
    {
        SetBearerToken(token);

        // Arama metni boşsa kullanıcıya yakın teslim noktaları getirilir.
        // Arama metni varsa API tarafında teslim noktası adı ve konum bilgilerine göre filtreleme yapılır.
        var endpoint = string.IsNullOrWhiteSpace(search)
            ? "api/DeliveryPoint/nearby"
            : $"api/DeliveryPoint/nearby?search={Uri.EscapeDataString(search.Trim())}";

        var response = await _httpClient.GetAsync(endpoint);

        if (!response.IsSuccessStatusCode)
            return new List<DeliveryPointViewModel>();

        var result = await response.Content.ReadFromJsonAsync<List<DeliveryPointViewModel>>(GetJsonOptions());

        return result ?? new List<DeliveryPointViewModel>();
    }

    // JWT token, korumalı Sharing ve DeliveryPoint API endpointlerine erişebilmek için Authorization header içine eklenir.
    private void SetBearerToken(string token)
    {
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
    }

    // API'den gelen JSON verilerinin ViewModel sınıflarına çevrilmesi için ortak JSON ayarıdır.
    private static JsonSerializerOptions GetJsonOptions()
    {
        return new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }
}


// Bu servis, FoodWise.Web ile FoodWise.API arasındaki paylaşım ilanı bağlantısını yönetir.
// JWT token ile korunan Sharing API endpointlerine istek gönderir.

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

        var apiBaseUrl = configuration["ApiSettings:BaseUrl"];

        if (string.IsNullOrWhiteSpace(apiBaseUrl))
            throw new InvalidOperationException("ApiSettings:BaseUrl appsettings.json içinde tanımlı olmalıdır.");

        _httpClient.BaseAddress = new Uri(apiBaseUrl.TrimEnd('/') + "/");
    }
    public async Task<List<ShareListingViewModel>> GetAvailableListingsAsync(string token)
    {
        SetBearerToken(token);

        var response = await _httpClient.GetAsync("api/sharing/listings/available");

        if (!response.IsSuccessStatusCode)
            return new List<ShareListingViewModel>();

        var result = await response.Content.ReadFromJsonAsync<List<ShareListingViewModel>>(GetJsonOptions());

        return result ?? new List<ShareListingViewModel>();
    }

    public async Task<bool> CreateRequestAsync(int listingId, string token)
    {
        SetBearerToken(token);

        var response = await _httpClient.PostAsync($"api/sharing/listings/{listingId}/request", null);

        return response.IsSuccessStatusCode;
    }

    public async Task<List<ShareRequestViewModel>> GetRequestsForListingAsync(int listingId, string token)
    {
        SetBearerToken(token);

        var response = await _httpClient.GetAsync($"api/sharing/listings/{listingId}/requests");

        if (!response.IsSuccessStatusCode)
            return new List<ShareRequestViewModel>();

        var result = await response.Content.ReadFromJsonAsync<List<ShareRequestViewModel>>(GetJsonOptions());

        return result ?? new List<ShareRequestViewModel>();
    }

    public async Task<bool> ApproveRequestAsync(int requestId, string token)
    {
        SetBearerToken(token);

        var response = await _httpClient.PostAsync($"api/sharing/requests/{requestId}/approve", null);

        return response.IsSuccessStatusCode;
    }

    public async Task<bool> RejectRequestAsync(int requestId, string token)
    {
        SetBearerToken(token);

        var response = await _httpClient.PostAsync($"api/sharing/requests/{requestId}/reject", null);

        return response.IsSuccessStatusCode;
    }
    public async Task<bool> CreateListingAsync(CreateShareListingViewModel model, string token)
    {
        SetBearerToken(token);

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

    public async Task<List<ShareListingViewModel>> GetMyListingsAsync(string token)
    {
        SetBearerToken(token);

        var response = await _httpClient.GetAsync("api/sharing/listings/my");

        if (!response.IsSuccessStatusCode)
            return new List<ShareListingViewModel>();

        var result = await response.Content.ReadFromJsonAsync<List<ShareListingViewModel>>(GetJsonOptions());

        return result ?? new List<ShareListingViewModel>();
    }

    private void SetBearerToken(string token)
    {
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
    }

    private static JsonSerializerOptions GetJsonOptions()
    {
        return new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }
}
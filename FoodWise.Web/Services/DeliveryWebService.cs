// Bu servis, FoodWise.Web ile FoodWise.API arasındaki teslimat ve QR işlemleri bağlantısını yönetir.
// JWT token ile korunan Delivery API endpointlerine istek gönderir.

using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FoodWise.Web.ViewModels.Delivery;

namespace FoodWise.Web.Services;

public class DeliveryWebService : IDeliveryWebService
{
    private readonly HttpClient _httpClient;

    public DeliveryWebService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;

        var apiBaseUrl = configuration["ApiSettings:BaseUrl"];

        if (string.IsNullOrWhiteSpace(apiBaseUrl))
            throw new InvalidOperationException("ApiSettings:BaseUrl appsettings.json içinde tanımlı olmalıdır.");

        _httpClient.BaseAddress = new Uri(apiBaseUrl.TrimEnd('/') + "/");
    }

    public async Task<DeliveryViewModel?> CreateDeliveryAsync(int shareRequestId, string token)
    {
        SetBearerToken(token);

        var response = await _httpClient.PostAsync($"api/delivery/create/{shareRequestId}", null);

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<DeliveryViewModel>(GetJsonOptions());
    }

    public async Task<List<DeliveryViewModel>> GetMyDonatedDeliveriesAsync(string token)
    {
        SetBearerToken(token);

        var response = await _httpClient.GetAsync("api/delivery/my-donated");

        if (!response.IsSuccessStatusCode)
            return new List<DeliveryViewModel>();

        var result = await response.Content.ReadFromJsonAsync<List<DeliveryViewModel>>(GetJsonOptions());

        return result ?? new List<DeliveryViewModel>();
    }

    public async Task<List<DeliveryViewModel>> GetMyReceivedDeliveriesAsync(string token)
    {
        SetBearerToken(token);

        var response = await _httpClient.GetAsync("api/delivery/my-received");

        if (!response.IsSuccessStatusCode)
            return new List<DeliveryViewModel>();

        var result = await response.Content.ReadFromJsonAsync<List<DeliveryViewModel>>(GetJsonOptions());

        return result ?? new List<DeliveryViewModel>();
    }

    public async Task<DeliveryViewModel?> MarkAsDroppedOffAsync(DropOffDeliveryViewModel model, string token)
    {
        SetBearerToken(token);

        var requestModel = new
        {
            model.DropOffImageUrl
        };

        var response = await _httpClient.PostAsJsonAsync($"api/delivery/{model.DeliveryId}/drop-off", requestModel);

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<DeliveryViewModel>(GetJsonOptions());
    }

    public async Task<DeliveryViewModel?> ScanBoxQrAsync(ScanDeliveryBoxViewModel model, string token)
    {
        SetBearerToken(token);

        var response = await _httpClient.PostAsJsonAsync("api/delivery/scan-box", model);

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<DeliveryViewModel>(GetJsonOptions());
    }

    public async Task<DeliveryViewModel?> CompleteDeliveryAsync(int deliveryId, string token)
    {
        SetBearerToken(token);

        var response = await _httpClient.PostAsync($"api/delivery/{deliveryId}/complete", null);

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<DeliveryViewModel>(GetJsonOptions());
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
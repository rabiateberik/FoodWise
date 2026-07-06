
// DeliveryWebService, FoodWise.Web ile FoodWise.API arasındaki teslimat ve QR işlemleri bağlantısını yönetir.
// Web tarafındaki teslimat ekranları, teslimat oluşturma, kutuya bırakma, QR okutma ve teslimatı tamamlama işlemlerini bu servis üzerinden API'ye gönderir.

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

        // Backend API adresi appsettings.json içindeki ApiSettings:BaseUrl değerinden okunur.
        var apiBaseUrl = configuration["ApiSettings:BaseUrl"];

        if (string.IsNullOrWhiteSpace(apiBaseUrl))
            throw new InvalidOperationException("ApiSettings:BaseUrl appsettings.json içinde tanımlı olmalıdır.");

        // HttpClient isteklerinin FoodWise.API adresine gönderilmesi sağlanır.
        _httpClient.BaseAddress = new Uri(apiBaseUrl.TrimEnd('/') + "/");
    }

    // Onaylanmış paylaşım talebi için teslimat oluşturma isteğini API'ye gönderir.
    public async Task<DeliveryViewModel?> CreateDeliveryAsync(int shareRequestId, string token)
    {
        SetBearerToken(token);

        var response = await _httpClient.PostAsync($"api/delivery/create/{shareRequestId}", null);

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<DeliveryViewModel>(GetJsonOptions());
    }

    // Kullanıcının bağışladığı ürünlere ait teslimatları API'den getirir.
    public async Task<List<DeliveryViewModel>> GetMyDonatedDeliveriesAsync(string token)
    {
        SetBearerToken(token);

        var response = await _httpClient.GetAsync("api/delivery/my-donated");

        if (!response.IsSuccessStatusCode)
            return new List<DeliveryViewModel>();

        var result = await response.Content.ReadFromJsonAsync<List<DeliveryViewModel>>(GetJsonOptions());

        return result ?? new List<DeliveryViewModel>();
    }

    // Kullanıcının teslim alacağı veya teslim aldığı ürünlere ait teslimatları API'den getirir.
    public async Task<List<DeliveryViewModel>> GetMyReceivedDeliveriesAsync(string token)
    {
        SetBearerToken(token);

        var response = await _httpClient.GetAsync("api/delivery/my-received");

        if (!response.IsSuccessStatusCode)
            return new List<DeliveryViewModel>();

        var result = await response.Content.ReadFromJsonAsync<List<DeliveryViewModel>>(GetJsonOptions());

        return result ?? new List<DeliveryViewModel>();
    }

    // Bağışçının ürünü teslimat kutusuna bıraktığını API'ye bildirir.
    public async Task<DeliveryViewModel?> MarkAsDroppedOffAsync(DropOffDeliveryViewModel model, string token)
    {
        SetBearerToken(token);

        // API tarafına yalnızca teslim bırakma görseli gönderilir.
        var requestModel = new
        {
            model.DropOffImageUrl
        };

        var response = await _httpClient.PostAsJsonAsync(
            $"api/delivery/{model.DeliveryId}/drop-off",
            requestModel);

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<DeliveryViewModel>(GetJsonOptions());
    }

    // Alıcının teslimat kutusu QR kodunu okutarak doğrulama yapmasını sağlar.
    public async Task<DeliveryViewModel?> ScanBoxQrAsync(ScanDeliveryBoxViewModel model, string token)
    {
        SetBearerToken(token);

        var response = await _httpClient.PostAsJsonAsync("api/delivery/scan-box", model);

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<DeliveryViewModel>(GetJsonOptions());
    }

    // QR doğrulaması tamamlanan teslimatı API üzerinden tamamlar.
    public async Task<DeliveryViewModel?> CompleteDeliveryAsync(int deliveryId, string token)
    {
        SetBearerToken(token);

        var response = await _httpClient.PostAsync($"api/delivery/{deliveryId}/complete", null);

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<DeliveryViewModel>(GetJsonOptions());
    }

    // JWT token, korumalı Delivery API endpointlerine erişebilmek için Authorization header içine eklenir.
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


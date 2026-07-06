
// EcoPointWebService, FoodWise.Web ile FoodWise.API arasındaki eco puan bağlantısını yönetir.
// Dashboard, profil ve eco puan geçmişi ekranları bu servis üzerinden API'den veri alır.

using System.Net.Http.Headers;
using System.Text.Json;
using FoodWise.Web.ViewModels.EcoPoint;

namespace FoodWise.Web.Services;

public class EcoPointWebService : IEcoPointWebService
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public EcoPointWebService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;

        // Backend API adresi appsettings.json içindeki ApiSettings:BaseUrl değerinden okunur.
        var baseUrl = configuration["ApiSettings:BaseUrl"];

        if (!string.IsNullOrWhiteSpace(baseUrl))
        {
            // HttpClient isteklerinin FoodWise.API adresine gönderilmesi sağlanır.
            _httpClient.BaseAddress = new Uri(baseUrl);
        }

        // API'den gelen JSON alan adlarının ViewModel sınıflarıyla uyumlu okunmasını sağlar.
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    // Kullanıcının toplam eco puanını, seviyesini ve özet bilgilerini API'den getirir.
    public async Task<EcoPointSummaryViewModel> GetSummaryAsync(string token)
    {
        // Token yoksa korumalı API endpointine istek gönderilmez.
        if (string.IsNullOrWhiteSpace(token))
            return new EcoPointSummaryViewModel();

        SetAuthorizationHeader(token);

        var response = await _httpClient.GetAsync("api/EcoPoint/summary");

        if (!response.IsSuccessStatusCode)
            return new EcoPointSummaryViewModel();

        var json = await response.Content.ReadAsStringAsync();

        return JsonSerializer.Deserialize<EcoPointSummaryViewModel>(json, _jsonOptions)
            ?? new EcoPointSummaryViewModel();
    }

    // Kullanıcının eco puan geçmişini API'den getirir.
    public async Task<List<EcoPointHistoryViewModel>> GetHistoryAsync(string token)
    {
        // Token yoksa boş liste döndürülür.
        if (string.IsNullOrWhiteSpace(token))
            return new List<EcoPointHistoryViewModel>();

        SetAuthorizationHeader(token);

        var response = await _httpClient.GetAsync("api/EcoPoint/history");

        if (!response.IsSuccessStatusCode)
            return new List<EcoPointHistoryViewModel>();

        var json = await response.Content.ReadAsStringAsync();

        return JsonSerializer.Deserialize<List<EcoPointHistoryViewModel>>(json, _jsonOptions)
            ?? new List<EcoPointHistoryViewModel>();
    }

    // JWT token, korumalı EcoPoint API endpointlerine erişebilmek için Authorization header içine eklenir.
    private void SetAuthorizationHeader(string token)
    {
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
    }
}


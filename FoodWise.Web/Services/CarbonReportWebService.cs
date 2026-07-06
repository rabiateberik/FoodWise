
// CarbonReportWebService, FoodWise.Web ile FoodWise.API arasındaki karbon raporu bağlantısını yönetir.
// Web tarafındaki rapor sayfaları, karbon raporu verilerini bu servis üzerinden API'den alır.

using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FoodWise.Web.ViewModels.CarbonReport;

namespace FoodWise.Web.Services;

public class CarbonReportWebService : ICarbonReportWebService
{
    private readonly HttpClient _httpClient;

    public CarbonReportWebService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;

        // Backend API adresi appsettings.json içindeki ApiSettings:BaseUrl değerinden okunur.
        var apiBaseUrl = configuration["ApiSettings:BaseUrl"];

        if (string.IsNullOrWhiteSpace(apiBaseUrl))
            throw new InvalidOperationException("ApiSettings:BaseUrl appsettings.json içinde tanımlı olmalıdır.");

        // HttpClient isteklerinin FoodWise.API adresine gönderilmesi sağlanır.
        _httpClient.BaseAddress = new Uri(apiBaseUrl.TrimEnd('/') + "/");
    }

    // Seçilen ay ve yıl için karbon raporu oluşturma isteğini API'ye gönderir.
    public async Task<CarbonReportViewModel?> GenerateMonthlyReportAsync(int month, int year, string token)
    {
        SetBearerToken(token);

        var response = await _httpClient.PostAsync(
            $"api/carbon-report/generate?month={month}&year={year}",
            null);

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<CarbonReportViewModel>(GetJsonOptions());
    }

    // Kullanıcının daha önce oluşturduğu karbon raporlarını API'den getirir.
    public async Task<List<CarbonReportViewModel>> GetMyReportsAsync(string token)
    {
        SetBearerToken(token);

        var response = await _httpClient.GetAsync("api/carbon-report/my");

        if (!response.IsSuccessStatusCode)
            return new List<CarbonReportViewModel>();

        var result = await response.Content.ReadFromJsonAsync<List<CarbonReportViewModel>>(GetJsonOptions());

        return result ?? new List<CarbonReportViewModel>();
    }

    // Kullanıcının toplam karbon tasarrufu özetini API'den getirir.
    public async Task<CarbonReportSummaryViewModel> GetSummaryAsync(string token)
    {
        SetBearerToken(token);

        var response = await _httpClient.GetAsync("api/carbon-report/summary");

        if (!response.IsSuccessStatusCode)
            return new CarbonReportSummaryViewModel();

        var result = await response.Content.ReadFromJsonAsync<CarbonReportSummaryViewModel>(GetJsonOptions());

        return result ?? new CarbonReportSummaryViewModel();
    }

    // JWT token, korumalı API endpointlerine erişebilmek için Authorization header içine eklenir.
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


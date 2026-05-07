// Bu servis, FoodWise.Web ile FoodWise.API arasındaki karbon raporu bağlantısını yönetir.
// JWT token ile korunan CarbonReport API endpointlerine istek gönderir.

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

        var apiBaseUrl = configuration["ApiSettings:BaseUrl"];

        if (string.IsNullOrWhiteSpace(apiBaseUrl))
            throw new InvalidOperationException("ApiSettings:BaseUrl appsettings.json içinde tanımlı olmalıdır.");

        _httpClient.BaseAddress = new Uri(apiBaseUrl.TrimEnd('/') + "/");
    }

    public async Task<CarbonReportViewModel?> GenerateMonthlyReportAsync(int month, int year, string token)
    {
        SetBearerToken(token);

        var response = await _httpClient.PostAsync($"api/carbon-report/generate?month={month}&year={year}", null);

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<CarbonReportViewModel>(GetJsonOptions());
    }

    public async Task<List<CarbonReportViewModel>> GetMyReportsAsync(string token)
    {
        SetBearerToken(token);

        var response = await _httpClient.GetAsync("api/carbon-report/my");

        if (!response.IsSuccessStatusCode)
            return new List<CarbonReportViewModel>();

        var result = await response.Content.ReadFromJsonAsync<List<CarbonReportViewModel>>(GetJsonOptions());

        return result ?? new List<CarbonReportViewModel>();
    }

    public async Task<CarbonReportSummaryViewModel> GetSummaryAsync(string token)
    {
        SetBearerToken(token);

        var response = await _httpClient.GetAsync("api/carbon-report/summary");

        if (!response.IsSuccessStatusCode)
            return new CarbonReportSummaryViewModel();

        var result = await response.Content.ReadFromJsonAsync<CarbonReportSummaryViewModel>(GetJsonOptions());

        return result ?? new CarbonReportSummaryViewModel();
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
// EcoPointWebService, FoodWise.API üzerindeki EcoPoint endpointlerini çağırır.
// Dashboard, Profil ve ileride Eco Puan geçmişi sayfası bu servis üzerinden veri alır.

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

        var baseUrl = configuration["ApiSettings:BaseUrl"];

        if (!string.IsNullOrWhiteSpace(baseUrl))
        {
            _httpClient.BaseAddress = new Uri(baseUrl);
        }

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<EcoPointSummaryViewModel> GetSummaryAsync(string token)
    {
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

    public async Task<List<EcoPointHistoryViewModel>> GetHistoryAsync(string token)
    {
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

    private void SetAuthorizationHeader(string token)
    {
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
    }
}
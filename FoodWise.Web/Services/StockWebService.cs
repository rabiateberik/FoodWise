// Bu servis, FoodWise.Web ile FoodWise.API arasındaki stok işlemleri bağlantısını yönetir.
// JWT token ile korunan Stock API endpointlerine istek gönderir.

using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FoodWise.Web.ViewModels.Stock;

namespace FoodWise.Web.Services;

public class StockWebService : IStockWebService
{
    private readonly HttpClient _httpClient;

    public StockWebService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;

        var apiBaseUrl = configuration["ApiSettings:BaseUrl"];

        if (string.IsNullOrWhiteSpace(apiBaseUrl))
            throw new InvalidOperationException("ApiSettings:BaseUrl appsettings.json içinde tanımlı olmalıdır.");

        _httpClient.BaseAddress = new Uri(apiBaseUrl.TrimEnd('/') + "/");
    }

    public async Task<List<StockItemViewModel>> GetMyStockAsync(string token)
    {
        SetBearerToken(token);

        var response = await _httpClient.GetAsync("api/stock");

        if (!response.IsSuccessStatusCode)
            return new List<StockItemViewModel>();

        var result = await response.Content.ReadFromJsonAsync<List<StockItemViewModel>>(GetJsonOptions());

        return result ?? new List<StockItemViewModel>();
    }

    public async Task<List<StockItemViewModel>> GetRiskyStockAsync(string token)
    {
        SetBearerToken(token);

        var response = await _httpClient.GetAsync("api/stock/risky");

        if (!response.IsSuccessStatusCode)
            return new List<StockItemViewModel>();

        var result = await response.Content.ReadFromJsonAsync<List<StockItemViewModel>>(GetJsonOptions());

        return result ?? new List<StockItemViewModel>();
    }

    public async Task<bool> CreateAsync(CreateStockItemViewModel model, string token)
    {
        SetBearerToken(token);

        var requestModel = new
        {
            model.ProductId,
            model.UnitId,
            model.Quantity,
            model.ExpirationDate,
            model.OpenedDate,
            model.StorageCondition,
            ImageUrl = (string?)null,
            model.Note
        };

        var response = await _httpClient.PostAsJsonAsync("api/stock", requestModel);

        return response.IsSuccessStatusCode;
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
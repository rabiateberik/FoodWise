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

    public async Task<StockItemViewModel?> GetByIdAsync(int id, string token)
    {
        SetBearerToken(token);

        // Düzenleme sayfasında mevcut stok ürününü forma doldurmak için tekil stok kaydı alınır.
        var response = await _httpClient.GetAsync($"api/stock/{id}");

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<StockItemViewModel>(GetJsonOptions());
    }

    public async Task<bool> CreateAsync(CreateStockItemViewModel model, string token)
    {
        SetBearerToken(token);

        // API tarafı StorageCondition değerini enum/int olarak beklediği için
        // Web formundan gelen değer güvenli şekilde gönderilir.
        var storageConditionValue = model.StorageCondition;

        var requestModel = new
        {
            model.ProductId,
            model.ProductName,
            model.UnitId,
            model.Quantity,
            model.ExpirationDate,
            model.OpenedDate,
            StorageCondition = storageConditionValue,
            ImageUrl = (string?)null,
            model.Note
        };

        var response = await _httpClient.PostAsJsonAsync("api/stock", requestModel);

        if (!response.IsSuccessStatusCode)
        {
            var errorMessage = await response.Content.ReadAsStringAsync();

            // Ekleme hatasını terminal/Output ekranında görmek için geçici log.
            Console.WriteLine($"Stock create failed. Status: {response.StatusCode}, Error: {errorMessage}");

            return false;
        }

        return true;
    }

    public async Task<bool> UpdateAsync(int id, EditStockItemViewModel model, string token)
    {
        SetBearerToken(token);

        // API tarafı StorageCondition değerini enum/int olarak beklediği için
        // Web formundan gelen string değer güvenli şekilde int'e çevrilir.
        var storageConditionValue = int.TryParse(model.StorageCondition, out var parsedStorageCondition)
            ? parsedStorageCondition
            : 5;

        // API tarafındaki UpdateStockItemDto ile uyumlu olacak şekilde güncelleme modeli hazırlanır.
        var requestModel = new
        {
            model.ProductId,
            model.ProductName,
            model.UnitId,
            model.Quantity,
            model.ExpirationDate,
            model.OpenedDate,
            StorageCondition = storageConditionValue,
            ImageUrl = (string?)null,
            model.Note
        };

        var response = await _httpClient.PutAsJsonAsync($"api/stock/{id}", requestModel);

        if (!response.IsSuccessStatusCode)
        {
            var errorMessage = await response.Content.ReadAsStringAsync();

            Console.WriteLine($"Stock update failed. Status: {response.StatusCode}, Error: {errorMessage}");

            return false;
        }

        return true;
    }

    public async Task<bool> DeleteAsync(int id, string token)
    {
        SetBearerToken(token);

        // Kullanıcının kendi stok ürününü silmesi için API'ye DELETE isteği gönderilir.
        var response = await _httpClient.DeleteAsync($"api/stock/{id}");

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
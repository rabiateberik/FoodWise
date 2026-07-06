
// StockWebService, FoodWise.Web ile FoodWise.API arasındaki stok işlemleri bağlantısını yönetir.
// Stok listeleme, riskli ürünleri getirme, stok ekleme, güncelleme ve silme işlemleri API üzerinden yapılır.

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

        // Backend API adresi appsettings.json içindeki ApiSettings:BaseUrl değerinden okunur.
        var apiBaseUrl = configuration["ApiSettings:BaseUrl"];

        if (string.IsNullOrWhiteSpace(apiBaseUrl))
            throw new InvalidOperationException("ApiSettings:BaseUrl appsettings.json içinde tanımlı olmalıdır.");

        // HttpClient isteklerinin FoodWise.API adresine gönderilmesi sağlanır.
        _httpClient.BaseAddress = new Uri(apiBaseUrl.TrimEnd('/') + "/");
    }

    // Kullanıcının stok ürünlerini API'den getirir.
    public async Task<List<StockItemViewModel>> GetMyStockAsync(string token)
    {
        SetBearerToken(token);

        var response = await _httpClient.GetAsync("api/stock");

        if (!response.IsSuccessStatusCode)
            return new List<StockItemViewModel>();

        var result = await response.Content.ReadFromJsonAsync<List<StockItemViewModel>>(GetJsonOptions());

        return result ?? new List<StockItemViewModel>();
    }

    // Kullanıcının riskli stok ürünlerini API'den getirir.
    public async Task<List<StockItemViewModel>> GetRiskyStockAsync(string token)
    {
        SetBearerToken(token);

        var response = await _httpClient.GetAsync("api/stock/risky");

        if (!response.IsSuccessStatusCode)
            return new List<StockItemViewModel>();

        var result = await response.Content.ReadFromJsonAsync<List<StockItemViewModel>>(GetJsonOptions());

        return result ?? new List<StockItemViewModel>();
    }

    // Düzenleme sayfasında mevcut stok ürününü forma doldurmak için tekil stok kaydı alınır.
    public async Task<StockItemViewModel?> GetByIdAsync(int id, string token)
    {
        SetBearerToken(token);

        var response = await _httpClient.GetAsync($"api/stock/{id}");

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<StockItemViewModel>(GetJsonOptions());
    }

    // Yeni stok ürünü oluşturma isteğini API'ye gönderir.
    public async Task<bool> CreateAsync(CreateStockItemViewModel model, string token)
    {
        SetBearerToken(token);

        // API tarafı StorageCondition değerini enum/int olarak beklediği için
        // Web formundan gelen değer doğrudan istek modeline eklenir.
        var storageConditionValue = model.StorageCondition;

        // API tarafındaki CreateStockItemDto ile uyumlu olacak şekilde istek modeli hazırlanır.
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

            // Geliştirme aşamasında stok ekleme hatasını terminal/Output ekranında görmek için yazdırılır.
            Console.WriteLine($"Stock create failed. Status: {response.StatusCode}, Error: {errorMessage}");

            return false;
        }

        return true;
    }

    // Mevcut stok ürününü güncelleme isteğini API'ye gönderir.
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

            // Geliştirme aşamasında stok güncelleme hatasını terminal/Output ekranında görmek için yazdırılır.
            Console.WriteLine($"Stock update failed. Status: {response.StatusCode}, Error: {errorMessage}");

            return false;
        }

        return true;
    }

    // Kullanıcının kendi stok ürününü silmesi/pasif hale getirmesi için API'ye DELETE isteği gönderir.
    public async Task<bool> DeleteAsync(int id, string token)
    {
        SetBearerToken(token);

        var response = await _httpClient.DeleteAsync($"api/stock/{id}");

        return response.IsSuccessStatusCode;
    }

    // Kullanıcının son kullanma tarihi geçmiş stok ürünlerini API'den getirir.
    public async Task<List<StockItemViewModel>> GetExpiredStockAsync(string token)
    {
        SetBearerToken(token);

        var response = await _httpClient.GetAsync("api/stock/expired");

        if (!response.IsSuccessStatusCode)
            return new List<StockItemViewModel>();

        return await response.Content.ReadFromJsonAsync<List<StockItemViewModel>>(GetJsonOptions())
               ?? new List<StockItemViewModel>();
    }

    // JWT token, korumalı Stock API endpointlerine erişebilmek için Authorization header içine eklenir.
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

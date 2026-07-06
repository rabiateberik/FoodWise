
// RecipeWebService, FoodWise.Web ile FoodWise.API arasındaki tarif önerisi bağlantısını yönetir.
// Tarif listeleme, stok ürününe göre öneri alma, genel öneriler ve tarif etkileşimleri bu servis üzerinden API'ye gönderilir.

using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FoodWise.Web.ViewModels.Recipe;

namespace FoodWise.Web.Services;

public class RecipeWebService : IRecipeWebService
{
    private readonly HttpClient _httpClient;

    public RecipeWebService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;

        // Backend API adresi appsettings.json içindeki ApiSettings:BaseUrl değerinden okunur.
        var apiBaseUrl = configuration["ApiSettings:BaseUrl"];

        if (string.IsNullOrWhiteSpace(apiBaseUrl))
            throw new InvalidOperationException("ApiSettings:BaseUrl appsettings.json içinde tanımlı olmalıdır.");

        // HttpClient isteklerinin FoodWise.API adresine gönderilmesi sağlanır.
        _httpClient.BaseAddress = new Uri(apiBaseUrl.TrimEnd('/') + "/");
    }

    // Sistemdeki tüm aktif tarifleri API'den getirir.
    public async Task<List<RecipeRecommendationViewModel>> GetAllRecipesAsync(string token)
    {
        SetBearerToken(token);

        var response = await _httpClient.GetAsync("api/recipe");

        if (!response.IsSuccessStatusCode)
            return new List<RecipeRecommendationViewModel>();

        var result = await response.Content.ReadFromJsonAsync<List<RecipeRecommendationViewModel>>(GetJsonOptions());

        return result ?? new List<RecipeRecommendationViewModel>();
    }

    // Seçilen stok ürününe göre tarif önerilerini API'den getirir.
    public async Task<List<RecipeRecommendationViewModel>> GetRecommendationsByStockItemAsync(int stockItemId, string token)
    {
        SetBearerToken(token);

        var response = await _httpClient.GetAsync($"api/recipe/recommendations/{stockItemId}");

        if (!response.IsSuccessStatusCode)
            return new List<RecipeRecommendationViewModel>();

        var result = await response.Content.ReadFromJsonAsync<List<RecipeRecommendationViewModel>>(GetJsonOptions());

        return result ?? new List<RecipeRecommendationViewModel>();
    }

    // Kullanıcının tüm stoklarına göre genel tarif önerilerini API'den getirir.
    public async Task<List<RecipeRecommendationViewModel>> GetGeneralRecommendationsAsync(string token)
    {
        SetBearerToken(token);

        var response = await _httpClient.GetAsync("api/recipe/recommendations");

        if (!response.IsSuccessStatusCode)
            return new List<RecipeRecommendationViewModel>();

        var result = await response.Content.ReadFromJsonAsync<List<RecipeRecommendationViewModel>>(GetJsonOptions());

        return result ?? new List<RecipeRecommendationViewModel>();
    }

    // Kullanıcının kaydettiği tarifleri API'den getirir.
    public async Task<List<RecipeRecommendationViewModel>> GetSavedRecipesAsync(string token)
    {
        SetBearerToken(token);

        var response = await _httpClient.GetAsync("api/recipe/interactions/saved");

        if (!response.IsSuccessStatusCode)
            return new List<RecipeRecommendationViewModel>();

        var result = await response.Content.ReadFromJsonAsync<List<RecipeRecommendationViewModel>>(GetJsonOptions());

        return result ?? new List<RecipeRecommendationViewModel>();
    }

    // Kullanıcının daha önce yaptığı olarak işaretlediği tarifleri API'den getirir.
    public async Task<List<RecipeRecommendationViewModel>> GetCookedRecipesAsync(string token)
    {
        SetBearerToken(token);

        var response = await _httpClient.GetAsync("api/recipe/interactions/cooked");

        if (!response.IsSuccessStatusCode)
            return new List<RecipeRecommendationViewModel>();

        var result = await response.Content.ReadFromJsonAsync<List<RecipeRecommendationViewModel>>(GetJsonOptions());

        return result ?? new List<RecipeRecommendationViewModel>();
    }

    // Kullanıcının tarifle ilgili etkileşimini API'ye gönderir.
    // Beğenme, kaydetme, yaptım, görüntüleme gibi işlemler bu metotla kaydedilir.
    public async Task<bool> CreateRecipeInteractionAsync(CreateRecipeInteractionViewModel model, string token)
    {
        SetBearerToken(token);

        var response = await _httpClient.PostAsJsonAsync(
            "api/recipe/interactions",
            model,
            GetJsonOptions()
        );

        return response.IsSuccessStatusCode;
    }

    // JWT token, korumalı Recipe API endpointlerine erişebilmek için Authorization header içine eklenir.
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


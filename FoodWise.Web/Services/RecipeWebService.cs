// Bu servis, FoodWise.Web ile FoodWise.API arasındaki tarif önerisi bağlantısını yönetir.
// JWT token ile korunan Recipe API endpointlerine istek gönderir.

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

        var apiBaseUrl = configuration["ApiSettings:BaseUrl"];

        if (string.IsNullOrWhiteSpace(apiBaseUrl))
            throw new InvalidOperationException("ApiSettings:BaseUrl appsettings.json içinde tanımlı olmalıdır.");

        _httpClient.BaseAddress = new Uri(apiBaseUrl.TrimEnd('/') + "/");
    }

    public async Task<List<RecipeRecommendationViewModel>> GetAllRecipesAsync(string token)
    {
        SetBearerToken(token);

        var response = await _httpClient.GetAsync("api/recipe");

        if (!response.IsSuccessStatusCode)
            return new List<RecipeRecommendationViewModel>();

        var result = await response.Content.ReadFromJsonAsync<List<RecipeRecommendationViewModel>>(GetJsonOptions());

        return result ?? new List<RecipeRecommendationViewModel>();
    }

    public async Task<List<RecipeRecommendationViewModel>> GetRecommendationsByStockItemAsync(int stockItemId, string token)
    {
        SetBearerToken(token);

        var response = await _httpClient.GetAsync($"api/recipe/recommendations/{stockItemId}");

        if (!response.IsSuccessStatusCode)
            return new List<RecipeRecommendationViewModel>();

        var result = await response.Content.ReadFromJsonAsync<List<RecipeRecommendationViewModel>>(GetJsonOptions());

        return result ?? new List<RecipeRecommendationViewModel>();
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
    public async Task<List<RecipeRecommendationViewModel>> GetGeneralRecommendationsAsync(string token)
    {
        SetBearerToken(token);

        var response = await _httpClient.GetAsync("api/recipe/recommendations");

        if (!response.IsSuccessStatusCode)
            return new List<RecipeRecommendationViewModel>();

        return await response.Content.ReadFromJsonAsync<List<RecipeRecommendationViewModel>>(GetJsonOptions())
               ?? new List<RecipeRecommendationViewModel>();
    }
}
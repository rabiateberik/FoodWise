// MlRecipeRecommendationService, ASP.NET Core API ile Python FastAPI tarif öneri modeli arasında bağlantı kurar.
// Tarif ve stok eşleşme bilgilerini ML servisine gönderir, 0-100 arası öneri skorunu geri alır.

using System.Net.Http.Json;
using System.Text.Json;
using FoodWise.Application.DTOs.RecipeRecommendation;
using FoodWise.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace FoodWise.Infrastructure.Services;

public class MlRecipeRecommendationService : IMlRecipeRecommendationService
{
    private readonly HttpClient _httpClient;

    public MlRecipeRecommendationService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;

        var baseUrl = configuration["MlApiSettings:BaseUrl"];

        if (string.IsNullOrWhiteSpace(baseUrl))
            throw new InvalidOperationException("MlApiSettings:BaseUrl appsettings.json içinde tanımlı olmalıdır.");

        _httpClient.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
    }

    public async Task<MlRecipeScorePredictionResponseDto?> PredictRecipeScoreAsync(
        MlRecipeScorePredictionRequestDto request)
    {
        try
        {
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };

            var response = await _httpClient.PostAsJsonAsync(
                "predict-recipe-score",
                request,
                jsonOptions);

            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadFromJsonAsync<MlRecipeScorePredictionResponseDto>(
                jsonOptions);
        }
        catch
        {
            // ML servisi kapalıysa veya hata oluşursa null döner.
            // RecipeService tarafında eski skor kullanılmaya devam eder.
            return null;
        }
    }
}
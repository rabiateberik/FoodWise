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

        // Python FastAPI servisinin adresi appsettings.json içinden okunur.
        var baseUrl = configuration["MlApiSettings:BaseUrl"];

        if (string.IsNullOrWhiteSpace(baseUrl))
            throw new InvalidOperationException("MlApiSettings:BaseUrl appsettings.json içinde tanımlı olmalıdır.");

        // HttpClient için temel ML API adresi ayarlanır.
        _httpClient.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
    }

    // Tek bir tarif için ML servisinden öneri skoru alır.
    public async Task<MlRecipeScorePredictionResponseDto?> PredictRecipeScoreAsync(
        MlRecipeScorePredictionRequestDto request)
    {
        try
        {
            var jsonOptions = CreateJsonOptions();

            // Tarif bilgileri Python FastAPI tarafındaki predict-recipe-score endpointine gönderilir.
            var response = await _httpClient.PostAsJsonAsync(
                "predict-recipe-score",
                request,
                jsonOptions);

            if (!response.IsSuccessStatusCode)
                return null;

            // Python servisinden gelen JSON cevap DTO yapısına çevrilir.
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

    // Birden fazla tarif için ML skorlarını tek istekte alır.
    // Böylece her tarif için ayrı ayrı API çağrısı yapılmaz ve performans artar.
    public async Task<List<MlRecipeScoreBatchPredictionItemResponseDto>> PredictRecipeScoresBatchAsync(
        List<MlRecipeScorePredictionRequestDto> requests)
    {
        try
        {
            if (requests == null || !requests.Any())
                return new List<MlRecipeScoreBatchPredictionItemResponseDto>();

            var jsonOptions = CreateJsonOptions();

            // Tarif önerileri toplu istek formatına dönüştürülür.
            var batchRequest = new MlRecipeScoreBatchPredictionRequestDto
            {
                Items = requests
            };

            // Toplu tarif verileri Python FastAPI batch tahmin endpointine gönderilir.
            var response = await _httpClient.PostAsJsonAsync(
                "predict-recipe-scores-batch",
                batchRequest,
                jsonOptions);

            if (!response.IsSuccessStatusCode)
                return new List<MlRecipeScoreBatchPredictionItemResponseDto>();

            // Python servisinden gelen toplu skor cevabı okunur.
            var result = await response.Content.ReadFromJsonAsync<MlRecipeScoreBatchPredictionResponseDto>(
                jsonOptions);

            return result?.Items ?? new List<MlRecipeScoreBatchPredictionItemResponseDto>();
        }
        catch
        {
            // ML servisi kapalıysa veya hata oluşursa boş liste döner.
            // RecipeService tarafında eski skorlar kullanılmaya devam eder.
            return new List<MlRecipeScoreBatchPredictionItemResponseDto>();
        }
    }

    // C# tarafındaki PascalCase alan adları ile Python tarafındaki camelCase JSON alanlarını uyumlu hale getirir.
    private static JsonSerializerOptions CreateJsonOptions()
    {
        return new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
    }
}


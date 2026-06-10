// MlRiskPredictionService, ASP.NET Core API ile Python FastAPI ML servisi arasında bağlantı kurar.
// Stok bilgilerini ML servisine gönderir ve tahmin edilen risk seviyesini geri alır.

using System.Net.Http.Json;
using System.Text.Json;
using FoodWise.Application.DTOs.RiskPrediction;
using FoodWise.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace FoodWise.Infrastructure.Services;

public class MlRiskPredictionService : IMlRiskPredictionService
{
    private readonly HttpClient _httpClient;

    public MlRiskPredictionService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;

        var baseUrl = configuration["MlApiSettings:BaseUrl"];

        if (string.IsNullOrWhiteSpace(baseUrl))
            throw new InvalidOperationException("MlApiSettings:BaseUrl appsettings.json içinde tanımlı olmalıdır.");

        _httpClient.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
    }

    public async Task<MlRiskPredictionResponseDto?> PredictRiskAsync(MlRiskPredictionRequestDto request)
    {
        try
        {
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };

            var response = await _httpClient.PostAsJsonAsync(
                "predict-risk",
                request,
                jsonOptions);

            if (!response.IsSuccessStatusCode)
                return null;

            var result = await response.Content.ReadFromJsonAsync<MlRiskPredictionResponseDto>(
                jsonOptions);

            return result;
        }
        catch
        {
            // ML servisi kapalıysa veya erişilemezse null döner.
            // StockService tarafında eski kural tabanlı risk hesabına fallback yapılacaktır.
            return null;
        }
    }
}
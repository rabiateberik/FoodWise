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

        // Python FastAPI ML servisinin temel adresi appsettings.json içinden okunur.
        var baseUrl = configuration["MlApiSettings:BaseUrl"];

        if (string.IsNullOrWhiteSpace(baseUrl))
            throw new InvalidOperationException("MlApiSettings:BaseUrl appsettings.json içinde tanımlı olmalıdır.");

        // HttpClient'ın tüm istekleri bu temel adres üzerinden göndermesi sağlanır.
        _httpClient.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
    }

    // Stok ürünü bilgilerini Python ML servisine göndererek risk tahmini alır.
    public async Task<MlRiskPredictionResponseDto?> PredictRiskAsync(MlRiskPredictionRequestDto request)
    {
        try
        {
            // C# tarafındaki PascalCase alan adları Python tarafındaki camelCase JSON yapısıyla uyumlu hale getirilir.
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };

            // Risk tahmini için istek Python FastAPI tarafındaki predict-risk endpointine gönderilir.
            var response = await _httpClient.PostAsJsonAsync(
                "predict-risk",
                request,
                jsonOptions);

            // ML servisi başarılı cevap dönmezse risk tahmini alınamaz.
            if (!response.IsSuccessStatusCode)
                return null;

            // Python servisinden gelen JSON cevap DTO yapısına çevrilir.
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


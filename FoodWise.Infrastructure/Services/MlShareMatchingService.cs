// MlShareMatchingService, ASP.NET Core API ile Python FastAPI akıllı eşleştirme modeli arasında bağlantı kurar.
// Paylaşım talebi, konum ve kullanıcı geçmişi bilgilerini ML servisine gönderir,
// 0-100 arası MatchScore sonucunu geri alır.

using System.Net.Http.Json;
using System.Text.Json;
using FoodWise.Application.DTOs.ShareMatching;
using FoodWise.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace FoodWise.Infrastructure.Services;

public class MlShareMatchingService : IMlShareMatchingService
{
    private readonly HttpClient _httpClient;

    public MlShareMatchingService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;

        // Python FastAPI ML servisinin temel adresi appsettings.json içinden okunur.
        var baseUrl = configuration["MlApiSettings:BaseUrl"];

        if (string.IsNullOrWhiteSpace(baseUrl))
            throw new InvalidOperationException("MlApiSettings:BaseUrl appsettings.json içinde tanımlı olmalıdır.");

        // HttpClient isteklerinin Python ML servis adresine gönderilmesi sağlanır.
        _httpClient.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
    }

    // Paylaşım ilanı ve kullanıcı bilgilerine göre ML servisinden eşleşme skoru alır.
    public async Task<MlShareMatchPredictionResponseDto?> PredictMatchScoreAsync(
        MlShareMatchPredictionRequestDto request)
    {
        try
        {
            // C# tarafındaki PascalCase alan adları Python tarafındaki camelCase JSON yapısıyla uyumlu hale getirilir.
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };

            // Eşleşme tahmini için istek Python FastAPI tarafındaki predict-match-score endpointine gönderilir.
            var response = await _httpClient.PostAsJsonAsync(
                "predict-match-score",
                request,
                jsonOptions);

            // ML servisi başarılı cevap dönmezse skor alınamaz.
            if (!response.IsSuccessStatusCode)
                return null;

            // Python servisinden gelen JSON cevap DTO yapısına çevrilir.
            return await response.Content.ReadFromJsonAsync<MlShareMatchPredictionResponseDto>(
                jsonOptions);
        }
        catch
        {
            // ML servisi kapalıysa veya hata oluşursa null döner.
            // Paylaşım eşleştirme tarafında eski kural tabanlı skor kullanılmaya devam eder.
            return null;
        }
    }
}


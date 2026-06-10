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

        var baseUrl = configuration["MlApiSettings:BaseUrl"];

        if (string.IsNullOrWhiteSpace(baseUrl))
            throw new InvalidOperationException("MlApiSettings:BaseUrl appsettings.json içinde tanımlı olmalıdır.");

        _httpClient.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
    }

    public async Task<MlShareMatchPredictionResponseDto?> PredictMatchScoreAsync(
        MlShareMatchPredictionRequestDto request)
    {
        try
        {
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };

            var response = await _httpClient.PostAsJsonAsync(
                "predict-match-score",
                request,
                jsonOptions);

            if (!response.IsSuccessStatusCode)
                return null;

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
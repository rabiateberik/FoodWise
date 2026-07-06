
// ProfileWebService, FoodWise.Web ile FoodWise.API arasındaki profil işlemleri bağlantısını yönetir.
// Profil görüntüleme, profil güncelleme, şifre değiştirme ve hesap silme işlemleri API üzerinden yapılır.

using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FoodWise.Web.ViewModels.Profile;

namespace FoodWise.Web.Services;

public class ProfileWebService : IProfileWebService
{
    private readonly HttpClient _httpClient;

    public ProfileWebService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;

        // Backend API adresi appsettings.json içindeki ApiSettings:BaseUrl değerinden okunur.
        var apiBaseUrl = configuration["ApiSettings:BaseUrl"];

        if (string.IsNullOrWhiteSpace(apiBaseUrl))
            throw new InvalidOperationException("ApiSettings:BaseUrl appsettings.json içinde tanımlı olmalıdır.");

        // HttpClient isteklerinin FoodWise.API adresine gönderilmesi sağlanır.
        _httpClient.BaseAddress = new Uri(apiBaseUrl.TrimEnd('/') + "/");
    }

    // Giriş yapan kullanıcının profil bilgilerini API'den getirir.
    public async Task<ProfileViewModel?> GetMyProfileAsync(string token)
    {
        SetBearerToken(token);

        var response = await _httpClient.GetAsync("api/profile/me");

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<ProfileViewModel>(GetJsonOptions());
    }

    // Kullanıcının profil ve konum bilgilerini güncelleme isteğini API'ye gönderir.
    public async Task<bool> UpdateProfileAsync(UpdateProfileViewModel model, string token)
    {
        SetBearerToken(token);

        var response = await _httpClient.PutAsJsonAsync("api/profile/me", model);

        return response.IsSuccessStatusCode;
    }

    // Kullanıcının şifre değiştirme isteğini API'ye gönderir.
    public async Task<bool> ChangePasswordAsync(ChangePasswordViewModel model, string token)
    {
        SetBearerToken(token);

        var response = await _httpClient.PostAsJsonAsync("api/profile/change-password", model);

        return response.IsSuccessStatusCode;
    }

    // Kullanıcının hesap silme/pasif hale getirme isteğini API'ye gönderir.
    public async Task<bool> DeleteAccountAsync(DeleteAccountViewModel model, string token)
    {
        SetBearerToken(token);

        var response = await _httpClient.PostAsJsonAsync("api/profile/delete-account", model);

        return response.IsSuccessStatusCode;
    }

    // JWT token, korumalı Profile API endpointlerine erişebilmek için Authorization header içine eklenir.
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


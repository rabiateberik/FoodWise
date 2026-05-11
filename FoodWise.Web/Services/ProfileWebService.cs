// Bu servis, FoodWise.Web ile FoodWise.API arasındaki profil bilgisi bağlantısını yönetir.
// JWT token ile korunan Profile API endpointlerine istek gönderir.

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

        var apiBaseUrl = configuration["ApiSettings:BaseUrl"];

        if (string.IsNullOrWhiteSpace(apiBaseUrl))
            throw new InvalidOperationException("ApiSettings:BaseUrl appsettings.json içinde tanımlı olmalıdır.");

        _httpClient.BaseAddress = new Uri(apiBaseUrl.TrimEnd('/') + "/");
    }

    public async Task<ProfileViewModel?> GetMyProfileAsync(string token)
    {
        SetBearerToken(token);

        var response = await _httpClient.GetAsync("api/profile/me");

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<ProfileViewModel>(GetJsonOptions());
    }

    public async Task<bool> UpdateProfileAsync(UpdateProfileViewModel model, string token)
    {
        SetBearerToken(token);

        var response = await _httpClient.PutAsJsonAsync("api/profile/me", model);

        return response.IsSuccessStatusCode;
    }

    public async Task<bool> ChangePasswordAsync(ChangePasswordViewModel model, string token)
    {
        SetBearerToken(token);

        var response = await _httpClient.PostAsJsonAsync("api/profile/change-password", model);

        return response.IsSuccessStatusCode;
    }

    private void SetBearerToken(string token)
    {
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
    }
    public async Task<bool> DeleteAccountAsync(DeleteAccountViewModel model, string token)
    {
        SetBearerToken(token);

        var response = await _httpClient.PostAsJsonAsync("api/profile/delete-account", model);

        return response.IsSuccessStatusCode;
    }
    private static JsonSerializerOptions GetJsonOptions()
    {
        return new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }
}
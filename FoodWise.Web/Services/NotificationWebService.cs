
// NotificationWebService, FoodWise.Web ile FoodWise.API arasındaki bildirim işlemleri bağlantısını yönetir.
// Web tarafındaki bildirim ekranları, bildirim listeleme, okundu yapma ve silme işlemlerini bu servis üzerinden API'ye gönderir.

using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FoodWise.Web.ViewModels.Notification;

namespace FoodWise.Web.Services;

public class NotificationWebService : INotificationWebService
{
    private readonly HttpClient _httpClient;

    public NotificationWebService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;

        // Backend API adresi appsettings.json içindeki ApiSettings:BaseUrl değerinden okunur.
        var apiBaseUrl = configuration["ApiSettings:BaseUrl"];

        if (string.IsNullOrWhiteSpace(apiBaseUrl))
            throw new InvalidOperationException("ApiSettings:BaseUrl appsettings.json içinde tanımlı olmalıdır.");

        // HttpClient isteklerinin FoodWise.API adresine gönderilmesi sağlanır.
        _httpClient.BaseAddress = new Uri(apiBaseUrl.TrimEnd('/') + "/");
    }

    // Kullanıcının tüm bildirimlerini API'den getirir.
    public async Task<List<NotificationViewModel>> GetMyNotificationsAsync(string token)
    {
        SetBearerToken(token);

        var response = await _httpClient.GetAsync("api/notification");

        if (!response.IsSuccessStatusCode)
            return new List<NotificationViewModel>();

        var result = await response.Content.ReadFromJsonAsync<List<NotificationViewModel>>(GetJsonOptions());

        return result ?? new List<NotificationViewModel>();
    }

    // Kullanıcının okunmamış bildirim sayısını API'den getirir.
    public async Task<int> GetUnreadCountAsync(string token)
    {
        SetBearerToken(token);

        var response = await _httpClient.GetAsync("api/notification/unread-count");

        if (!response.IsSuccessStatusCode)
            return 0;

        var result = await response.Content.ReadFromJsonAsync<UnreadCountViewModel>(GetJsonOptions());

        return result?.UnreadCount ?? 0;
    }

    // Seçilen bildirimi okundu olarak işaretlemek için API'ye PATCH isteği gönderir.
    public async Task<bool> MarkAsReadAsync(int notificationId, string token)
    {
        SetBearerToken(token);

        var response = await _httpClient.PatchAsync($"api/notification/{notificationId}/read", null);

        return response.IsSuccessStatusCode;
    }

    // Kullanıcının tüm bildirimlerini okundu yapmak için API'ye PATCH isteği gönderir.
    public async Task<bool> MarkAllAsReadAsync(string token)
    {
        SetBearerToken(token);

        var response = await _httpClient.PatchAsync("api/notification/read-all", null);

        return response.IsSuccessStatusCode;
    }

    // Seçilen bildirimi silmek/pasif hale getirmek için API'ye DELETE isteği gönderir.
    public async Task<bool> DeleteAsync(int notificationId, string token)
    {
        SetBearerToken(token);

        var response = await _httpClient.DeleteAsync($"api/notification/{notificationId}");

        return response.IsSuccessStatusCode;
    }

    // Test amaçlı bildirim oluşturmak için API'ye örnek bildirim verisi gönderir.
    public async Task<bool> CreateTestNotificationAsync(string token)
    {
        SetBearerToken(token);

        var requestModel = new
        {
            Title = "FoodWise Test Bildirimi",
            Message = "Bu bildirim, Web arayüzünden test amacıyla oluşturuldu.",
            Type = 6
        };

        var response = await _httpClient.PostAsJsonAsync("api/notification/test", requestModel);

        return response.IsSuccessStatusCode;
    }

    // JWT token, korumalı Notification API endpointlerine erişebilmek için Authorization header içine eklenir.
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


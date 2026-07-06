
// AdminWebService, FoodWise.Web ile FoodWise.API arasındaki admin panel bağlantısını yönetir.
// Admin panelindeki kategori, ürün, kullanıcı, teslim noktası ve teslimat işlemleri için API'ye HTTP istekleri gönderir.
// Admin endpointleri JWT token ile korunduğu için her istekte Bearer token kullanılır.

using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FoodWise.Web.ViewModels.Admin;

namespace FoodWise.Web.Services;

public class AdminWebService : IAdminWebService
{
    private readonly HttpClient _httpClient;

    public AdminWebService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;

        // Backend API adresi appsettings.json içindeki ApiSettings:BaseUrl değerinden okunur.
        var apiBaseUrl = configuration["ApiSettings:BaseUrl"];

        if (string.IsNullOrWhiteSpace(apiBaseUrl))
            throw new InvalidOperationException("ApiSettings:BaseUrl appsettings.json içinde tanımlı olmalıdır.");

        // HttpClient isteklerinin FoodWise.API adresine gönderilmesi sağlanır.
        _httpClient.BaseAddress = new Uri(apiBaseUrl.TrimEnd('/') + "/");
    }

    // Admin dashboard ekranında gösterilecek özet bilgileri API'den getirir.
    public async Task<AdminDashboardViewModel?> GetDashboardSummaryAsync(string token)
    {
        SetBearerToken(token);

        var response = await _httpClient.GetAsync("api/admin/dashboard");

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<AdminDashboardViewModel>(GetJsonOptions());
    }

    // Admin endpointleri korumalı olduğu için JWT token Authorization header içine eklenir.
    private void SetBearerToken(string token)
    {
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
    }

    // API'den gelen JSON verilerinin ViewModel sınıflarına sorunsuz çevrilmesi için ortak JSON ayarıdır.
    private static JsonSerializerOptions GetJsonOptions()
    {
        return new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    // Kategori listesini API'den getirir.
    public async Task<List<AdminCategoryViewModel>> GetCategoriesAsync(string token)
    {
        SetBearerToken(token);

        var response = await _httpClient.GetAsync("api/admin/categories");

        if (!response.IsSuccessStatusCode)
            return new List<AdminCategoryViewModel>();

        return await response.Content.ReadFromJsonAsync<List<AdminCategoryViewModel>>(GetJsonOptions())
               ?? new List<AdminCategoryViewModel>();
    }

    // Seçilen kategori bilgisini API'den getirir.
    public async Task<AdminCategoryViewModel?> GetCategoryByIdAsync(int id, string token)
    {
        SetBearerToken(token);

        var response = await _httpClient.GetAsync($"api/admin/categories/{id}");

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<AdminCategoryViewModel>(GetJsonOptions());
    }

    // Yeni kategori oluşturma isteğini API'ye gönderir.
    public async Task<bool> CreateCategoryAsync(CreateAdminCategoryViewModel model, string token)
    {
        SetBearerToken(token);

        var response = await _httpClient.PostAsJsonAsync("api/admin/categories", model);

        return response.IsSuccessStatusCode;
    }

    // Kategori güncelleme isteğini API'ye gönderir.
    public async Task<bool> UpdateCategoryAsync(UpdateAdminCategoryViewModel model, string token)
    {
        SetBearerToken(token);

        var response = await _httpClient.PutAsJsonAsync($"api/admin/categories/{model.Id}", model);

        return response.IsSuccessStatusCode;
    }

    // Kategorinin aktif/pasif durumunu değiştirmek için API'ye PATCH isteği gönderir.
    public async Task<bool> ToggleCategoryStatusAsync(int id, string token)
    {
        SetBearerToken(token);

        var request = new HttpRequestMessage(
            HttpMethod.Patch,
            $"api/admin/categories/{id}/toggle-status");

        var response = await _httpClient.SendAsync(request);

        return response.IsSuccessStatusCode;
    }

    // Ürün listesini API'den getirir.
    public async Task<List<AdminProductViewModel>> GetProductsAsync(string token)
    {
        SetBearerToken(token);

        var response = await _httpClient.GetAsync("api/admin/products");

        if (!response.IsSuccessStatusCode)
            return new List<AdminProductViewModel>();

        return await response.Content.ReadFromJsonAsync<List<AdminProductViewModel>>(GetJsonOptions())
               ?? new List<AdminProductViewModel>();
    }

    // Seçilen ürün bilgisini API'den getirir.
    public async Task<AdminProductViewModel?> GetProductByIdAsync(int id, string token)
    {
        SetBearerToken(token);

        var response = await _httpClient.GetAsync($"api/admin/products/{id}");

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<AdminProductViewModel>(GetJsonOptions());
    }

    // Yeni ürün oluşturma isteğini API'ye gönderir.
    public async Task<bool> CreateProductAsync(CreateAdminProductViewModel model, string token)
    {
        SetBearerToken(token);

        var response = await _httpClient.PostAsJsonAsync("api/admin/products", model);

        return response.IsSuccessStatusCode;
    }

    // Ürün güncelleme isteğini API'ye gönderir.
    public async Task<bool> UpdateProductAsync(UpdateAdminProductViewModel model, string token)
    {
        SetBearerToken(token);

        var response = await _httpClient.PutAsJsonAsync($"api/admin/products/{model.Id}", model);

        return response.IsSuccessStatusCode;
    }

    // Ürünün aktif/pasif durumunu değiştirmek için API'ye PATCH isteği gönderir.
    public async Task<bool> ToggleProductStatusAsync(int id, string token)
    {
        SetBearerToken(token);

        var request = new HttpRequestMessage(
            HttpMethod.Patch,
            $"api/admin/products/{id}/toggle-status");

        var response = await _httpClient.SendAsync(request);

        return response.IsSuccessStatusCode;
    }

    // Ürünün admin onay durumunu değiştirmek için API'ye PATCH isteği gönderir.
    public async Task<bool> ToggleProductApprovalAsync(int id, string token)
    {
        SetBearerToken(token);

        var request = new HttpRequestMessage(
            HttpMethod.Patch,
            $"api/admin/products/{id}/toggle-approval");

        var response = await _httpClient.SendAsync(request);

        return response.IsSuccessStatusCode;
    }

    // Teslim noktalarını API'den getirir.
    public async Task<List<AdminDeliveryPointViewModel>> GetDeliveryPointsAsync(string token)
    {
        SetBearerToken(token);

        var response = await _httpClient.GetAsync("api/admin/delivery-points");

        if (!response.IsSuccessStatusCode)
            return new List<AdminDeliveryPointViewModel>();

        return await response.Content.ReadFromJsonAsync<List<AdminDeliveryPointViewModel>>(GetJsonOptions())
               ?? new List<AdminDeliveryPointViewModel>();
    }

    // Seçilen teslim noktası bilgisini API'den getirir.
    public async Task<AdminDeliveryPointViewModel?> GetDeliveryPointByIdAsync(int id, string token)
    {
        SetBearerToken(token);

        var response = await _httpClient.GetAsync($"api/admin/delivery-points/{id}");

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<AdminDeliveryPointViewModel>(GetJsonOptions());
    }

    // Yeni teslim noktası oluşturma isteğini API'ye gönderir.
    public async Task<bool> CreateDeliveryPointAsync(CreateAdminDeliveryPointViewModel model, string token)
    {
        SetBearerToken(token);

        var response = await _httpClient.PostAsJsonAsync("api/admin/delivery-points", model);

        return response.IsSuccessStatusCode;
    }

    // Teslim noktası güncelleme isteğini API'ye gönderir.
    public async Task<bool> UpdateDeliveryPointAsync(UpdateAdminDeliveryPointViewModel model, string token)
    {
        SetBearerToken(token);

        var response = await _httpClient.PutAsJsonAsync($"api/admin/delivery-points/{model.Id}", model);

        return response.IsSuccessStatusCode;
    }

    // Teslim noktasının aktif/pasif durumunu değiştirmek için API'ye PATCH isteği gönderir.
    public async Task<bool> ToggleDeliveryPointStatusAsync(int id, string token)
    {
        SetBearerToken(token);

        var request = new HttpRequestMessage(
            HttpMethod.Patch,
            $"api/admin/delivery-points/{id}/toggle-status");

        var response = await _httpClient.SendAsync(request);

        return response.IsSuccessStatusCode;
    }

    // Teslim kutularını API'den getirir.
    // deliveryPointId gönderilirse sadece ilgili teslim noktasına ait kutular listelenir.
    public async Task<List<AdminDeliveryBoxViewModel>> GetDeliveryBoxesAsync(string token, int? deliveryPointId = null)
    {
        SetBearerToken(token);

        var url = deliveryPointId.HasValue
            ? $"api/admin/delivery-boxes?deliveryPointId={deliveryPointId.Value}"
            : "api/admin/delivery-boxes";

        var response = await _httpClient.GetAsync(url);

        if (!response.IsSuccessStatusCode)
            return new List<AdminDeliveryBoxViewModel>();

        return await response.Content.ReadFromJsonAsync<List<AdminDeliveryBoxViewModel>>(GetJsonOptions())
               ?? new List<AdminDeliveryBoxViewModel>();
    }

    // Seçilen teslim kutusu bilgisini API'den getirir.
    public async Task<AdminDeliveryBoxViewModel?> GetDeliveryBoxByIdAsync(int id, string token)
    {
        SetBearerToken(token);

        var response = await _httpClient.GetAsync($"api/admin/delivery-boxes/{id}");

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<AdminDeliveryBoxViewModel>(GetJsonOptions());
    }

    // Yeni teslim kutusu oluşturma isteğini API'ye gönderir.
    public async Task<bool> CreateDeliveryBoxAsync(CreateAdminDeliveryBoxViewModel model, string token)
    {
        SetBearerToken(token);

        var response = await _httpClient.PostAsJsonAsync("api/admin/delivery-boxes", model);

        return response.IsSuccessStatusCode;
    }

    // Teslim kutusu güncelleme isteğini API'ye gönderir.
    public async Task<bool> UpdateDeliveryBoxAsync(UpdateAdminDeliveryBoxViewModel model, string token)
    {
        SetBearerToken(token);

        var response = await _httpClient.PutAsJsonAsync($"api/admin/delivery-boxes/{model.Id}", model);

        return response.IsSuccessStatusCode;
    }

    // Teslim kutusunun aktif/pasif durumunu değiştirmek için API'ye PATCH isteği gönderir.
    public async Task<bool> ToggleDeliveryBoxStatusAsync(int id, string token)
    {
        SetBearerToken(token);

        var request = new HttpRequestMessage(
            HttpMethod.Patch,
            $"api/admin/delivery-boxes/{id}/toggle-status");

        var response = await _httpClient.SendAsync(request);

        return response.IsSuccessStatusCode;
    }

    // Kullanıcı listesini API'den getirir.
    public async Task<List<AdminUserViewModel>> GetUsersAsync(string token)
    {
        SetBearerToken(token);

        var response = await _httpClient.GetAsync("api/admin/users");

        if (!response.IsSuccessStatusCode)
            return new List<AdminUserViewModel>();

        return await response.Content.ReadFromJsonAsync<List<AdminUserViewModel>>(GetJsonOptions())
               ?? new List<AdminUserViewModel>();
    }

    // Seçilen kullanıcı bilgisini API'den getirir.
    public async Task<AdminUserViewModel?> GetUserByIdAsync(string id, string token)
    {
        SetBearerToken(token);

        var response = await _httpClient.GetAsync($"api/admin/users/{id}");

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<AdminUserViewModel>(GetJsonOptions());
    }

    // Kullanıcının aktif/pasif durumunu değiştirmek için API'ye PATCH isteği gönderir.
    public async Task<bool> ToggleUserStatusAsync(string id, string token)
    {
        SetBearerToken(token);

        var request = new HttpRequestMessage(
            HttpMethod.Patch,
            $"api/admin/users/{id}/toggle-status");

        var response = await _httpClient.SendAsync(request);

        return response.IsSuccessStatusCode;
    }

    // Seçilen kullanıcının stok ürünlerini API'den getirir.
    public async Task<List<AdminUserStockViewModel>> GetUserStocksAsync(string id, string token)
    {
        SetBearerToken(token);

        var response = await _httpClient.GetAsync($"api/admin/users/{id}/stocks");

        if (!response.IsSuccessStatusCode)
            return new List<AdminUserStockViewModel>();

        return await response.Content.ReadFromJsonAsync<List<AdminUserStockViewModel>>(GetJsonOptions())
               ?? new List<AdminUserStockViewModel>();
    }

    // Seçilen kullanıcının paylaşım ilanlarını API'den getirir.
    public async Task<List<AdminUserShareListingViewModel>> GetUserShareListingsAsync(string id, string token)
    {
        SetBearerToken(token);

        var response = await _httpClient.GetAsync($"api/admin/users/{id}/share-listings");

        if (!response.IsSuccessStatusCode)
            return new List<AdminUserShareListingViewModel>();

        return await response.Content.ReadFromJsonAsync<List<AdminUserShareListingViewModel>>(GetJsonOptions())
               ?? new List<AdminUserShareListingViewModel>();
    }

    // Seçilen kullanıcının teslimat kayıtlarını API'den getirir.
    public async Task<List<AdminUserDeliveryViewModel>> GetUserDeliveriesAsync(string id, string token)
    {
        SetBearerToken(token);

        var response = await _httpClient.GetAsync($"api/admin/users/{id}/deliveries");

        if (!response.IsSuccessStatusCode)
            return new List<AdminUserDeliveryViewModel>();

        return await response.Content.ReadFromJsonAsync<List<AdminUserDeliveryViewModel>>(GetJsonOptions())
               ?? new List<AdminUserDeliveryViewModel>();
    }

    // Admin panelinde takip edilecek paylaşım ilanlarını API'den getirir.
    public async Task<List<AdminShareListingViewModel>> GetShareListingsAsync(string token)
    {
        SetBearerToken(token);

        var response = await _httpClient.GetAsync("api/admin/share-listings");

        if (!response.IsSuccessStatusCode)
            return new List<AdminShareListingViewModel>();

        return await response.Content.ReadFromJsonAsync<List<AdminShareListingViewModel>>(GetJsonOptions())
               ?? new List<AdminShareListingViewModel>();
    }

    // Admin panelinde takip edilecek teslimat kayıtlarını API'den getirir.
    public async Task<List<AdminDeliveryMonitorViewModel>> GetDeliveriesAsync(string token)
    {
        SetBearerToken(token);

        var response = await _httpClient.GetAsync("api/admin/deliveries");

        if (!response.IsSuccessStatusCode)
            return new List<AdminDeliveryMonitorViewModel>();

        return await response.Content.ReadFromJsonAsync<List<AdminDeliveryMonitorViewModel>>(GetJsonOptions())
               ?? new List<AdminDeliveryMonitorViewModel>();
    }
}


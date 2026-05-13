// AdminWebService, FoodWise.Web ile FoodWise.API arasındaki admin panel bağlantısını yönetir.
// Admin endpointleri JWT token ile korunur.

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

        var apiBaseUrl = configuration["ApiSettings:BaseUrl"];

        if (string.IsNullOrWhiteSpace(apiBaseUrl))
            throw new InvalidOperationException("ApiSettings:BaseUrl appsettings.json içinde tanımlı olmalıdır.");

        _httpClient.BaseAddress = new Uri(apiBaseUrl.TrimEnd('/') + "/");
    }

    public async Task<AdminDashboardViewModel?> GetDashboardSummaryAsync(string token)
    {
        SetBearerToken(token);

        var response = await _httpClient.GetAsync("api/admin/dashboard");

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<AdminDashboardViewModel>(GetJsonOptions());
    }

    private void SetBearerToken(string token)
    {
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
    }

    private static JsonSerializerOptions GetJsonOptions()
    {
        return new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }
    public async Task<List<AdminCategoryViewModel>> GetCategoriesAsync(string token)
    {
        SetBearerToken(token);

        var response = await _httpClient.GetAsync("api/admin/categories");

        if (!response.IsSuccessStatusCode)
            return new List<AdminCategoryViewModel>();

        return await response.Content.ReadFromJsonAsync<List<AdminCategoryViewModel>>(GetJsonOptions())
               ?? new List<AdminCategoryViewModel>();
    }

    public async Task<AdminCategoryViewModel?> GetCategoryByIdAsync(int id, string token)
    {
        SetBearerToken(token);

        var response = await _httpClient.GetAsync($"api/admin/categories/{id}");

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<AdminCategoryViewModel>(GetJsonOptions());
    }

    public async Task<bool> CreateCategoryAsync(CreateAdminCategoryViewModel model, string token)
    {
        SetBearerToken(token);

        var response = await _httpClient.PostAsJsonAsync("api/admin/categories", model);

        return response.IsSuccessStatusCode;
    }

    public async Task<bool> UpdateCategoryAsync(UpdateAdminCategoryViewModel model, string token)
    {
        SetBearerToken(token);

        var response = await _httpClient.PutAsJsonAsync($"api/admin/categories/{model.Id}", model);

        return response.IsSuccessStatusCode;
    }

    public async Task<bool> ToggleCategoryStatusAsync(int id, string token)
    {
        SetBearerToken(token);

        var request = new HttpRequestMessage(
            HttpMethod.Patch,
            $"api/admin/categories/{id}/toggle-status");

        var response = await _httpClient.SendAsync(request);

        return response.IsSuccessStatusCode;
    }
    public async Task<List<AdminProductViewModel>> GetProductsAsync(string token)
    {
        SetBearerToken(token);

        var response = await _httpClient.GetAsync("api/admin/products");

        if (!response.IsSuccessStatusCode)
            return new List<AdminProductViewModel>();

        return await response.Content.ReadFromJsonAsync<List<AdminProductViewModel>>(GetJsonOptions())
               ?? new List<AdminProductViewModel>();
    }

    public async Task<AdminProductViewModel?> GetProductByIdAsync(int id, string token)
    {
        SetBearerToken(token);

        var response = await _httpClient.GetAsync($"api/admin/products/{id}");

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<AdminProductViewModel>(GetJsonOptions());
    }

    public async Task<bool> CreateProductAsync(CreateAdminProductViewModel model, string token)
    {
        SetBearerToken(token);

        var response = await _httpClient.PostAsJsonAsync("api/admin/products", model);

        return response.IsSuccessStatusCode;
    }

    public async Task<bool> UpdateProductAsync(UpdateAdminProductViewModel model, string token)
    {
        SetBearerToken(token);

        var response = await _httpClient.PutAsJsonAsync($"api/admin/products/{model.Id}", model);

        return response.IsSuccessStatusCode;
    }

    public async Task<bool> ToggleProductStatusAsync(int id, string token)
    {
        SetBearerToken(token);

        var request = new HttpRequestMessage(
            HttpMethod.Patch,
            $"api/admin/products/{id}/toggle-status");

        var response = await _httpClient.SendAsync(request);

        return response.IsSuccessStatusCode;
    }

    public async Task<bool> ToggleProductApprovalAsync(int id, string token)
    {
        SetBearerToken(token);

        var request = new HttpRequestMessage(
            HttpMethod.Patch,
            $"api/admin/products/{id}/toggle-approval");

        var response = await _httpClient.SendAsync(request);

        return response.IsSuccessStatusCode;
    }
    // DeliveryPoint yönetimi için gerekli metotlar
    public async Task<List<AdminDeliveryPointViewModel>> GetDeliveryPointsAsync(string token)
    {
        SetBearerToken(token);

        var response = await _httpClient.GetAsync("api/admin/delivery-points");

        if (!response.IsSuccessStatusCode)
            return new List<AdminDeliveryPointViewModel>();

        return await response.Content.ReadFromJsonAsync<List<AdminDeliveryPointViewModel>>(GetJsonOptions())
               ?? new List<AdminDeliveryPointViewModel>();
    }

    public async Task<AdminDeliveryPointViewModel?> GetDeliveryPointByIdAsync(int id, string token)
    {
        SetBearerToken(token);

        var response = await _httpClient.GetAsync($"api/admin/delivery-points/{id}");

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<AdminDeliveryPointViewModel>(GetJsonOptions());
    }

    public async Task<bool> CreateDeliveryPointAsync(CreateAdminDeliveryPointViewModel model, string token)
    {
        SetBearerToken(token);

        var response = await _httpClient.PostAsJsonAsync("api/admin/delivery-points", model);

        return response.IsSuccessStatusCode;
    }

    public async Task<bool> UpdateDeliveryPointAsync(UpdateAdminDeliveryPointViewModel model, string token)
    {
        SetBearerToken(token);

        var response = await _httpClient.PutAsJsonAsync($"api/admin/delivery-points/{model.Id}", model);

        return response.IsSuccessStatusCode;
    }

    public async Task<bool> ToggleDeliveryPointStatusAsync(int id, string token)
    {
        SetBearerToken(token);

        var request = new HttpRequestMessage(
            HttpMethod.Patch,
            $"api/admin/delivery-points/{id}/toggle-status");

        var response = await _httpClient.SendAsync(request);

        return response.IsSuccessStatusCode;
    }
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

    public async Task<AdminDeliveryBoxViewModel?> GetDeliveryBoxByIdAsync(int id, string token)
    {
        SetBearerToken(token);

        var response = await _httpClient.GetAsync($"api/admin/delivery-boxes/{id}");

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<AdminDeliveryBoxViewModel>(GetJsonOptions());
    }

    public async Task<bool> CreateDeliveryBoxAsync(CreateAdminDeliveryBoxViewModel model, string token)
    {
        SetBearerToken(token);

        var response = await _httpClient.PostAsJsonAsync("api/admin/delivery-boxes", model);

        return response.IsSuccessStatusCode;
    }

    public async Task<bool> UpdateDeliveryBoxAsync(UpdateAdminDeliveryBoxViewModel model, string token)
    {
        SetBearerToken(token);

        var response = await _httpClient.PutAsJsonAsync($"api/admin/delivery-boxes/{model.Id}", model);

        return response.IsSuccessStatusCode;
    }

    public async Task<bool> ToggleDeliveryBoxStatusAsync(int id, string token)
    {
        SetBearerToken(token);

        var request = new HttpRequestMessage(
            HttpMethod.Patch,
            $"api/admin/delivery-boxes/{id}/toggle-status");

        var response = await _httpClient.SendAsync(request);

        return response.IsSuccessStatusCode;
    }
}
// IAdminWebService, FoodWise.Web projesinin Admin API endpointleriyle haberleşmesini sağlar.

using FoodWise.Web.ViewModels.Admin;

namespace FoodWise.Web.Services;

public interface IAdminWebService
{
    Task<AdminDashboardViewModel?> GetDashboardSummaryAsync(string token);

    Task<List<AdminCategoryViewModel>> GetCategoriesAsync(string token);

    Task<AdminCategoryViewModel?> GetCategoryByIdAsync(int id, string token);

    Task<bool> CreateCategoryAsync(CreateAdminCategoryViewModel model, string token);

    Task<bool> UpdateCategoryAsync(UpdateAdminCategoryViewModel model, string token);

    Task<bool> ToggleCategoryStatusAsync(int id, string token);

    Task<List<AdminProductViewModel>> GetProductsAsync(string token);

    Task<AdminProductViewModel?> GetProductByIdAsync(int id, string token);

    Task<bool> CreateProductAsync(CreateAdminProductViewModel model, string token);

    Task<bool> UpdateProductAsync(UpdateAdminProductViewModel model, string token);

    Task<bool> ToggleProductStatusAsync(int id, string token);

    Task<bool> ToggleProductApprovalAsync(int id, string token);
    // DeliveryPoint yönetimi için gerekli metotlar
    Task<List<AdminDeliveryPointViewModel>> GetDeliveryPointsAsync(string token);

    Task<AdminDeliveryPointViewModel?> GetDeliveryPointByIdAsync(int id, string token);

    Task<bool> CreateDeliveryPointAsync(CreateAdminDeliveryPointViewModel model, string token);

    Task<bool> UpdateDeliveryPointAsync(UpdateAdminDeliveryPointViewModel model, string token);

    Task<bool> ToggleDeliveryPointStatusAsync(int id, string token);
    // DeliveryBox yönetimi için gerekli metotlar
    Task<List<AdminDeliveryBoxViewModel>> GetDeliveryBoxesAsync(string token, int? deliveryPointId = null);

    Task<AdminDeliveryBoxViewModel?> GetDeliveryBoxByIdAsync(int id, string token);

    Task<bool> CreateDeliveryBoxAsync(CreateAdminDeliveryBoxViewModel model, string token);

    Task<bool> UpdateDeliveryBoxAsync(UpdateAdminDeliveryBoxViewModel model, string token);

    Task<bool> ToggleDeliveryBoxStatusAsync(int id, string token);
    // Kullanıcı yönetimi için gerekli metotlar
    Task<List<AdminUserViewModel>> GetUsersAsync(string token);

    Task<AdminUserViewModel?> GetUserByIdAsync(string id, string token);

    Task<bool> ToggleUserStatusAsync(string id, string token);
    Task<List<AdminUserStockViewModel>> GetUserStocksAsync(string id, string token);

    Task<List<AdminUserShareListingViewModel>> GetUserShareListingsAsync(string id, string token);

    Task<List<AdminUserDeliveryViewModel>> GetUserDeliveriesAsync(string id, string token);
    Task<List<AdminShareListingViewModel>> GetShareListingsAsync(string token);

    Task<List<AdminDeliveryMonitorViewModel>> GetDeliveriesAsync(string token);
}
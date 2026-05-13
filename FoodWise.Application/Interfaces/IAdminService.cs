using FoodWise.Application.DTOs.Admin;

namespace FoodWise.Application.Interfaces;

public interface IAdminService
{
    Task<AdminDashboardDto> GetDashboardSummaryAsync();

    Task<List<AdminCategoryDto>> GetCategoriesAsync();

    Task<AdminCategoryDto?> GetCategoryByIdAsync(int id);

    Task<AdminCategoryDto?> CreateCategoryAsync(CreateAdminCategoryDto model);

    Task<AdminCategoryDto?> UpdateCategoryAsync(int id, UpdateAdminCategoryDto model);

    Task<bool> ToggleCategoryStatusAsync(int id);
    Task<List<AdminProductDto>> GetProductsAsync();

    Task<AdminProductDto?> GetProductByIdAsync(int id);

    Task<AdminProductDto?> CreateProductAsync(CreateAdminProductDto model);

    Task<AdminProductDto?> UpdateProductAsync(int id, UpdateAdminProductDto model);

    Task<bool> ToggleProductStatusAsync(int id);

    Task<bool> ToggleProductApprovalAsync(int id);
    //DeliveryPoint yönetimi için gerekli metotlar
    Task<List<AdminDeliveryPointDto>> GetDeliveryPointsAsync();

    Task<AdminDeliveryPointDto?> GetDeliveryPointByIdAsync(int id);

    Task<AdminDeliveryPointDto?> CreateDeliveryPointAsync(CreateAdminDeliveryPointDto model);

    Task<AdminDeliveryPointDto?> UpdateDeliveryPointAsync(int id, UpdateAdminDeliveryPointDto model);
    //
    Task<bool> ToggleDeliveryPointStatusAsync(int id);
    // DeliveryBox yönetimi için gerekli metotlar
    Task<List<AdminDeliveryBoxDto>> GetDeliveryBoxesAsync(int? deliveryPointId = null);

    Task<AdminDeliveryBoxDto?> GetDeliveryBoxByIdAsync(int id);

    Task<AdminDeliveryBoxDto?> CreateDeliveryBoxAsync(CreateAdminDeliveryBoxDto model);

    Task<AdminDeliveryBoxDto?> UpdateDeliveryBoxAsync(int id, UpdateAdminDeliveryBoxDto model);

    Task<bool> ToggleDeliveryBoxStatusAsync(int id);
    Task<List<AdminUserDto>> GetUsersAsync();

    Task<AdminUserDto?> GetUserByIdAsync(string id);

    Task<bool> ToggleUserStatusAsync(string id);
    Task<List<AdminUserStockDto>> GetUserStocksAsync(string userId);

    Task<List<AdminUserShareListingDto>> GetUserShareListingsAsync(string userId);

    Task<List<AdminUserDeliveryDto>> GetUserDeliveriesAsync(string userId);
}
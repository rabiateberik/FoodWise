// AdminService, admin paneli için sistem genelindeki özet verileri hesaplar.

using FoodWise.Application.DTOs.Admin;
using FoodWise.Application.Interfaces;
using FoodWise.Domain.Entities;
using FoodWise.Domain.Enums;
using FoodWise.Infrastructure.Data;
using FoodWise.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FoodWise.Infrastructure.Services;

public class AdminService : IAdminService
{
    private readonly FoodWiseDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminService(
        FoodWiseDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<AdminDashboardDto> GetDashboardSummaryAsync()
    {
        var totalCarbonSavedKg = await _context.CarbonReports
            .SumAsync(x => (decimal?)x.EstimatedCarbonSaved) ?? 0;

        var totalEcoPoint = await _context.EcoPointHistories
            .SumAsync(x => (int?)x.Point) ?? 0;

        return new AdminDashboardDto
        {
            TotalUserCount = await _userManager.Users.CountAsync(),
            ActiveUserCount = await _userManager.Users.CountAsync(x => x.IsActive),
            PassiveUserCount = await _userManager.Users.CountAsync(x => !x.IsActive),

            TotalCategoryCount = await _context.Categories.CountAsync(x => x.IsActive),
            TotalProductCount = await _context.Products.CountAsync(x => x.IsActive),
            UserCreatedProductCount = await _context.Products.CountAsync(x =>
                x.IsActive &&
                !x.IsSystemDefined),

            TotalDeliveryPointCount = await _context.DeliveryPoints.CountAsync(x => x.IsActive),
            TotalDeliveryBoxCount = await _context.DeliveryBoxes.CountAsync(x => x.IsActive),

            TotalShareListingCount = await _context.ShareListings.CountAsync(x => x.IsActive),
            ActiveShareListingCount = await _context.ShareListings.CountAsync(x =>
                x.IsActive &&
                x.Status != ShareListingStatus.Cancelled &&
                x.Status != ShareListingStatus.Expired &&
                x.Status != ShareListingStatus.Delivered),

            CompletedDeliveryCount = await _context.Deliveries.CountAsync(x =>
                x.Status == DeliveryStatus.Delivered),

            ExpiredDeliveryCount = await _context.Deliveries.CountAsync(x =>
                x.Status == DeliveryStatus.Expired),

            TotalCarbonSavedKg = totalCarbonSavedKg,
            TotalEcoPoint = totalEcoPoint
        };
    }
    public async Task<List<AdminCategoryDto>> GetCategoriesAsync()
    {
        var categories = await _context.Categories
            .Include(x => x.Products)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        return categories
            .Select(MapCategoryToDto)
            .ToList();
    }

    public async Task<AdminCategoryDto?> GetCategoryByIdAsync(int id)
    {
        var category = await _context.Categories
            .Include(x => x.Products)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (category == null)
            return null;

        return MapCategoryToDto(category);
    }

    public async Task<AdminCategoryDto?> CreateCategoryAsync(CreateAdminCategoryDto model)
    {
        var normalizedName = model.Name.Trim();

        var categoryExists = await _context.Categories
            .AnyAsync(x => x.Name.ToLower() == normalizedName.ToLower());

        if (categoryExists)
            return null;

        var category = new Category
        {
            Name = normalizedName,
            Description = string.IsNullOrWhiteSpace(model.Description)
                ? null
                : model.Description.Trim(),
            CreatedAt = DateTime.Now,
            IsActive = true
        };

        await _context.Categories.AddAsync(category);
        await _context.SaveChangesAsync();

        return MapCategoryToDto(category);
    }

    public async Task<AdminCategoryDto?> UpdateCategoryAsync(int id, UpdateAdminCategoryDto model)
    {
        var category = await _context.Categories
            .Include(x => x.Products)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (category == null)
            return null;

        var normalizedName = model.Name.Trim();

        var categoryExists = await _context.Categories
            .AnyAsync(x =>
                x.Id != id &&
                x.Name.ToLower() == normalizedName.ToLower());

        if (categoryExists)
            return null;

        category.Name = normalizedName;
        category.Description = string.IsNullOrWhiteSpace(model.Description)
            ? null
            : model.Description.Trim();
        category.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        return MapCategoryToDto(category);
    }

    public async Task<bool> ToggleCategoryStatusAsync(int id)
    {
        var category = await _context.Categories
            .FirstOrDefaultAsync(x => x.Id == id);

        if (category == null)
            return false;

        category.IsActive = !category.IsActive;
        category.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        return true;
    }

    private static AdminCategoryDto MapCategoryToDto(Category category)
    {
        return new AdminCategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            IsActive = category.IsActive,
            CreatedAt = category.CreatedAt,
            ProductCount = category.Products?.Count ?? 0
        };
    }

    public async Task<List<AdminProductDto>> GetProductsAsync()
    {
        // Admin panelinde aktif/pasif tüm ürünler gösterilir.
        // Böylece pasifleştirilen ürünler de yönetilebilir.
        var products = await _context.Products
            .Include(x => x.Category)
            .Include(x => x.StockItems)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        return products
            .Select(MapProductToDto)
            .ToList();
    }

    public async Task<AdminProductDto?> GetProductByIdAsync(int id)
    {
        var product = await _context.Products
            .Include(x => x.Category)
            .Include(x => x.StockItems)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (product == null)
            return null;

        return MapProductToDto(product);
    }

    public async Task<AdminProductDto?> CreateProductAsync(CreateAdminProductDto model)
    {
        var normalizedName = model.Name.Trim();

        // Aynı isimde ürün oluşturulmasını engeller.
        var productExists = await _context.Products
            .AnyAsync(x => x.Name.ToLower() == normalizedName.ToLower());

        if (productExists)
            return null;

        // Ürünün bağlanacağı kategori gerçekten var mı kontrol edilir.
        var categoryExists = await _context.Categories
            .AnyAsync(x => x.Id == model.CategoryId && x.IsActive);

        if (!categoryExists)
            return null;

        var product = new Product
        {
            CategoryId = model.CategoryId,
            Name = normalizedName,
            DefaultShelfLifeDays = model.DefaultShelfLifeDays,
            OpenedShelfLifeDays = model.OpenedShelfLifeDays,
            CarbonFactor = model.CarbonFactor,
            IsSensitiveFood = model.IsSensitiveFood,

            // Admin tarafından eklenen ürün sistem ürünü kabul edilir.
            IsSystemDefined = true,
            IsApproved = model.IsApproved,
            IsActive = true,

            CreatedAt = DateTime.Now
        };

        await _context.Products.AddAsync(product);
        await _context.SaveChangesAsync();

        var createdProduct = await _context.Products
            .Include(x => x.Category)
            .Include(x => x.StockItems)
            .FirstAsync(x => x.Id == product.Id);

        return MapProductToDto(createdProduct);
    }

    public async Task<AdminProductDto?> UpdateProductAsync(int id, UpdateAdminProductDto model)
    {
        var product = await _context.Products
            .Include(x => x.Category)
            .Include(x => x.StockItems)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (product == null)
            return null;

        var normalizedName = model.Name.Trim();

        // Başka bir üründe aynı ad varsa güncelleme engellenir.
        var productExists = await _context.Products
            .AnyAsync(x =>
                x.Id != id &&
                x.Name.ToLower() == normalizedName.ToLower());

        if (productExists)
            return null;

        var categoryExists = await _context.Categories
            .AnyAsync(x => x.Id == model.CategoryId && x.IsActive);

        if (!categoryExists)
            return null;

        product.CategoryId = model.CategoryId;
        product.Name = normalizedName;
        product.DefaultShelfLifeDays = model.DefaultShelfLifeDays;
        product.OpenedShelfLifeDays = model.OpenedShelfLifeDays;
        product.CarbonFactor = model.CarbonFactor;
        product.IsSensitiveFood = model.IsSensitiveFood;
        product.IsApproved = model.IsApproved;
        product.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        var updatedProduct = await _context.Products
            .Include(x => x.Category)
            .Include(x => x.StockItems)
            .FirstAsync(x => x.Id == product.Id);

        return MapProductToDto(updatedProduct);
    }

    public async Task<bool> ToggleProductStatusAsync(int id)
    {
        var product = await _context.Products
            .FirstOrDefaultAsync(x => x.Id == id);

        if (product == null)
            return false;

        product.IsActive = !product.IsActive;
        product.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> ToggleProductApprovalAsync(int id)
    {
        var product = await _context.Products
            .FirstOrDefaultAsync(x => x.Id == id);

        if (product == null)
            return false;

        product.IsApproved = !product.IsApproved;
        product.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        return true;
    }

    private static AdminProductDto MapProductToDto(Product product)
    {
        return new AdminProductDto
        {
            Id = product.Id,
            CategoryId = product.CategoryId,
            CategoryName = product.Category?.Name ?? "-",
            Name = product.Name,
            DefaultShelfLifeDays = product.DefaultShelfLifeDays,
            OpenedShelfLifeDays = product.OpenedShelfLifeDays,
            CarbonFactor = product.CarbonFactor,
            IsSensitiveFood = product.IsSensitiveFood,
            IsSystemDefined = product.IsSystemDefined,
            IsApproved = product.IsApproved,
            IsActive = product.IsActive,
            CreatedByUserId = product.CreatedByUserId,
            CreatedAt = product.CreatedAt,
            StockItemCount = product.StockItems?.Count ?? 0
        };
    }
    public async Task<List<AdminDeliveryPointDto>> GetDeliveryPointsAsync()
    {
        // Admin panelinde aktif/pasif tüm teslimat noktaları gösterilir.
        var deliveryPoints = await _context.DeliveryPoints
            .Include(x => x.DeliveryBoxes)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        return deliveryPoints
            .Select(MapDeliveryPointToDto)
            .ToList();
    }

    public async Task<AdminDeliveryPointDto?> GetDeliveryPointByIdAsync(int id)
    {
        var deliveryPoint = await _context.DeliveryPoints
            .Include(x => x.DeliveryBoxes)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (deliveryPoint == null)
            return null;

        return MapDeliveryPointToDto(deliveryPoint);
    }

    public async Task<AdminDeliveryPointDto?> CreateDeliveryPointAsync(CreateAdminDeliveryPointDto model)
    {
        var normalizedName = model.Name.Trim();
        var normalizedCity = model.City.Trim();
        var normalizedDistrict = model.District.Trim();
        var normalizedNeighborhood = string.IsNullOrWhiteSpace(model.Neighborhood)
            ? null
            : model.Neighborhood.Trim();

        // Aynı şehir/ilçe/mahalle içinde aynı teslimat noktası adı tekrar oluşturulmasın.
        var deliveryPointExists = await _context.DeliveryPoints
            .AnyAsync(x =>
                x.Name.ToLower() == normalizedName.ToLower() &&
                x.City != null &&
                x.City.ToLower() == normalizedCity.ToLower() &&
                x.District != null &&
                x.District.ToLower() == normalizedDistrict.ToLower());

        if (deliveryPointExists)
            return null;

        var deliveryPoint = new DeliveryPoint
        {
            Name = normalizedName,
            Description = string.IsNullOrWhiteSpace(model.Description)
                ? null
                : model.Description.Trim(),

            City = normalizedCity,
            District = normalizedDistrict,
            Neighborhood = normalizedNeighborhood,

            Latitude = model.Latitude,
            Longitude = model.Longitude,
            WorkingHours = string.IsNullOrWhiteSpace(model.WorkingHours)
                ? null
                : model.WorkingHours.Trim(),
            StorageType = string.IsNullOrWhiteSpace(model.StorageType)
                ? null
                : model.StorageType.Trim(),

            CreatedAt = DateTime.Now,
            IsActive = true
        };

        await _context.DeliveryPoints.AddAsync(deliveryPoint);
        await _context.SaveChangesAsync();

        var createdDeliveryPoint = await _context.DeliveryPoints
            .Include(x => x.DeliveryBoxes)
            .FirstAsync(x => x.Id == deliveryPoint.Id);

        return MapDeliveryPointToDto(createdDeliveryPoint);
    }

    public async Task<AdminDeliveryPointDto?> UpdateDeliveryPointAsync(int id, UpdateAdminDeliveryPointDto model)
    {
        var deliveryPoint = await _context.DeliveryPoints
            .Include(x => x.DeliveryBoxes)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (deliveryPoint == null)
            return null;

        var normalizedName = model.Name.Trim();
        var normalizedCity = model.City.Trim();
        var normalizedDistrict = model.District.Trim();
        var normalizedNeighborhood = string.IsNullOrWhiteSpace(model.Neighborhood)
            ? null
            : model.Neighborhood.Trim();

        // Başka bir teslimat noktasında aynı şehir/ilçe içinde aynı ad varsa engellenir.
        var deliveryPointExists = await _context.DeliveryPoints
            .AnyAsync(x =>
                x.Id != id &&
                x.Name.ToLower() == normalizedName.ToLower() &&
                x.City != null &&
                x.City.ToLower() == normalizedCity.ToLower() &&
                x.District != null &&
                x.District.ToLower() == normalizedDistrict.ToLower());

        if (deliveryPointExists)
            return null;

        deliveryPoint.Name = normalizedName;
        deliveryPoint.Description = string.IsNullOrWhiteSpace(model.Description)
            ? null
            : model.Description.Trim();

        deliveryPoint.City = normalizedCity;
        deliveryPoint.District = normalizedDistrict;
        deliveryPoint.Neighborhood = normalizedNeighborhood;

        deliveryPoint.Latitude = model.Latitude;
        deliveryPoint.Longitude = model.Longitude;
        deliveryPoint.WorkingHours = string.IsNullOrWhiteSpace(model.WorkingHours)
            ? null
            : model.WorkingHours.Trim();
        deliveryPoint.StorageType = string.IsNullOrWhiteSpace(model.StorageType)
            ? null
            : model.StorageType.Trim();

        deliveryPoint.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        return MapDeliveryPointToDto(deliveryPoint);
    }

    public async Task<bool> ToggleDeliveryPointStatusAsync(int id)
    {
        var deliveryPoint = await _context.DeliveryPoints
            .FirstOrDefaultAsync(x => x.Id == id);

        if (deliveryPoint == null)
            return false;

        deliveryPoint.IsActive = !deliveryPoint.IsActive;
        deliveryPoint.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        return true;
    }

    private static AdminDeliveryPointDto MapDeliveryPointToDto(DeliveryPoint deliveryPoint)
    {
        return new AdminDeliveryPointDto
        {
            Id = deliveryPoint.Id,
            Name = deliveryPoint.Name,
            Description = deliveryPoint.Description,
            City = deliveryPoint.City,
            District = deliveryPoint.District,
            Neighborhood = deliveryPoint.Neighborhood,
            Latitude = deliveryPoint.Latitude,
            Longitude = deliveryPoint.Longitude,
            WorkingHours = deliveryPoint.WorkingHours,
            StorageType = deliveryPoint.StorageType,
            IsActive = deliveryPoint.IsActive,
            CreatedAt = deliveryPoint.CreatedAt,
            DeliveryBoxCount = deliveryPoint.DeliveryBoxes?.Count ?? 0
        };
    }

    public async Task<List<AdminDeliveryBoxDto>> GetDeliveryBoxesAsync(int? deliveryPointId = null)
    {
        // Admin panelinde aktif/pasif tüm teslim kutuları gösterilir.
        // deliveryPointId gelirse sadece ilgili teslim noktasına ait kutular listelenir.
        var query = _context.DeliveryBoxes
            .Include(x => x.DeliveryPoint)
            .AsQueryable();

        if (deliveryPointId.HasValue)
        {
            query = query.Where(x => x.DeliveryPointId == deliveryPointId.Value);
        }

        var deliveryBoxes = await query
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        return deliveryBoxes
            .Select(MapDeliveryBoxToDto)
            .ToList();
    }

    public async Task<AdminDeliveryBoxDto?> GetDeliveryBoxByIdAsync(int id)
    {
        var deliveryBox = await _context.DeliveryBoxes
            .Include(x => x.DeliveryPoint)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (deliveryBox == null)
            return null;

        return MapDeliveryBoxToDto(deliveryBox);
    }

    public async Task<AdminDeliveryBoxDto?> CreateDeliveryBoxAsync(CreateAdminDeliveryBoxDto model)
    {
        var normalizedBoxCode = model.BoxCode.Trim();
        var normalizedQrCodeValue = model.QrCodeValue.Trim();

        // Kutu aktif bir teslim noktasına bağlı olmalıdır.
        var deliveryPointExists = await _context.DeliveryPoints
            .AnyAsync(x => x.Id == model.DeliveryPointId && x.IsActive);

        if (!deliveryPointExists)
            return null;

        // Aynı teslim noktasında aynı kutu kodu tekrar oluşturulmasın.
        var boxCodeExists = await _context.DeliveryBoxes
            .AnyAsync(x =>
                x.DeliveryPointId == model.DeliveryPointId &&
                x.BoxCode.ToLower() == normalizedBoxCode.ToLower());

        if (boxCodeExists)
            return null;

        // QR kod değeri sistem genelinde benzersiz olmalıdır.
        var qrCodeExists = await _context.DeliveryBoxes
            .AnyAsync(x => x.QrCodeValue.ToLower() == normalizedQrCodeValue.ToLower());

        if (qrCodeExists)
            return null;

        var deliveryBox = new DeliveryBox
        {
            DeliveryPointId = model.DeliveryPointId,
            BoxCode = normalizedBoxCode,
            QrCodeValue = normalizedQrCodeValue,
            Description = string.IsNullOrWhiteSpace(model.Description)
                ? null
                : model.Description.Trim(),

            IsOccupied = false,
            IsActive = true,
            CreatedAt = DateTime.Now
        };

        await _context.DeliveryBoxes.AddAsync(deliveryBox);
        await _context.SaveChangesAsync();

        var createdDeliveryBox = await _context.DeliveryBoxes
            .Include(x => x.DeliveryPoint)
            .FirstAsync(x => x.Id == deliveryBox.Id);

        return MapDeliveryBoxToDto(createdDeliveryBox);
    }

    public async Task<AdminDeliveryBoxDto?> UpdateDeliveryBoxAsync(int id, UpdateAdminDeliveryBoxDto model)
    {
        var deliveryBox = await _context.DeliveryBoxes
            .Include(x => x.DeliveryPoint)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (deliveryBox == null)
            return null;

        var normalizedBoxCode = model.BoxCode.Trim();
        var normalizedQrCodeValue = model.QrCodeValue.Trim();

        var deliveryPointExists = await _context.DeliveryPoints
            .AnyAsync(x => x.Id == model.DeliveryPointId && x.IsActive);

        if (!deliveryPointExists)
            return null;

        // Başka bir kutuda aynı teslim noktası + kutu kodu varsa engellenir.
        var boxCodeExists = await _context.DeliveryBoxes
            .AnyAsync(x =>
                x.Id != id &&
                x.DeliveryPointId == model.DeliveryPointId &&
                x.BoxCode.ToLower() == normalizedBoxCode.ToLower());

        if (boxCodeExists)
            return null;

        // Başka bir kutuda aynı QR kod değeri varsa engellenir.
        var qrCodeExists = await _context.DeliveryBoxes
            .AnyAsync(x =>
                x.Id != id &&
                x.QrCodeValue.ToLower() == normalizedQrCodeValue.ToLower());

        if (qrCodeExists)
            return null;

        deliveryBox.DeliveryPointId = model.DeliveryPointId;
        deliveryBox.BoxCode = normalizedBoxCode;
        deliveryBox.QrCodeValue = normalizedQrCodeValue;
        deliveryBox.Description = string.IsNullOrWhiteSpace(model.Description)
            ? null
            : model.Description.Trim();
        deliveryBox.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        var updatedDeliveryBox = await _context.DeliveryBoxes
            .Include(x => x.DeliveryPoint)
            .FirstAsync(x => x.Id == deliveryBox.Id);

        return MapDeliveryBoxToDto(updatedDeliveryBox);
    }

    public async Task<bool> ToggleDeliveryBoxStatusAsync(int id)
    {
        var deliveryBox = await _context.DeliveryBoxes
            .FirstOrDefaultAsync(x => x.Id == id);

        if (deliveryBox == null)
            return false;

        // Dolu kutu pasifleştirilemez.
        // Önce teslimat tamamlanmalı veya süresi dolmalıdır.
        if (deliveryBox.IsOccupied)
            return false;

        deliveryBox.IsActive = !deliveryBox.IsActive;
        deliveryBox.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        return true;
    }

    private static AdminDeliveryBoxDto MapDeliveryBoxToDto(DeliveryBox deliveryBox)
    {
        return new AdminDeliveryBoxDto
        {
            Id = deliveryBox.Id,
            DeliveryPointId = deliveryBox.DeliveryPointId,
            DeliveryPointName = deliveryBox.DeliveryPoint?.Name ?? "-",
            City = deliveryBox.DeliveryPoint?.City,
            District = deliveryBox.DeliveryPoint?.District,
            Neighborhood = deliveryBox.DeliveryPoint?.Neighborhood,
            BoxCode = deliveryBox.BoxCode,
            QrCodeValue = deliveryBox.QrCodeValue,
            Description = deliveryBox.Description,
            IsOccupied = deliveryBox.IsOccupied,
            IsActive = deliveryBox.IsActive,
            CreatedAt = deliveryBox.CreatedAt
        };
    }

    public async Task<List<AdminUserDto>> GetUsersAsync()
    {
        // Admin panelinde aktif ve pasif tüm kullanıcılar listelenir.
        var users = await _userManager.Users
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        var result = new List<AdminUserDto>();

        foreach (var user in users)
        {
            result.Add(await MapUserToDtoAsync(user));
        }

        return result;
    }

    public async Task<AdminUserDto?> GetUserByIdAsync(string id)
    {
        var user = await _userManager.FindByIdAsync(id);

        if (user == null)
            return null;

        return await MapUserToDtoAsync(user);
    }

    public async Task<bool> ToggleUserStatusAsync(string id)
    {
        var user = await _userManager.FindByIdAsync(id);

        if (user == null)
            return false;

        // Admin hesabı pasifleştirilemez.
        // Böylece sistem yönetim erişimi yanlışlıkla kapanmaz.
        if (await _userManager.IsInRoleAsync(user, "Admin"))
            return false;

        user.IsActive = !user.IsActive;
        user.UpdatedAt = DateTime.Now;

        user.DeletedAt = user.IsActive
            ? null
            : DateTime.Now;

        var result = await _userManager.UpdateAsync(user);

        return result.Succeeded;
    }

    private async Task<AdminUserDto> MapUserToDtoAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);

        // Kullanıcının stok kayıtları sayılır.
        var stockItemCount = await _context.StockItems
            .CountAsync(x => x.UserId == user.Id && x.IsActive);

        // Kullanıcının oluşturduğu paylaşım ilanları sayılır.
        var shareListingCount = await _context.ShareListings
            .CountAsync(x => x.DonorUserId == user.Id && x.IsActive);

        // Kullanıcının halen aktif kabul edilen paylaşım ilanları sayılır.
        var activeShareListingCount = await _context.ShareListings
            .CountAsync(x =>
                x.DonorUserId == user.Id &&
                x.IsActive &&
                x.Status != ShareListingStatus.Cancelled &&
                x.Status != ShareListingStatus.Expired &&
                x.Status != ShareListingStatus.Delivered);

        // Kullanıcının bağışçı olduğu teslimatlar.
        var donatedDeliveryCount = await _context.Deliveries
            .CountAsync(x => x.DonorUserId == user.Id);

        // Kullanıcının alıcı olduğu teslimatlar.
        var receivedDeliveryCount = await _context.Deliveries
            .CountAsync(x => x.ReceiverUserId == user.Id);

        // Kullanıcının başarıyla tamamladığı bağış teslimatları.
        var completedDonatedDeliveryCount = await _context.Deliveries
            .CountAsync(x =>
                x.DonorUserId == user.Id &&
                x.Status == DeliveryStatus.Delivered);

        // Kullanıcının başarıyla teslim aldığı ürünler.
        var completedReceivedDeliveryCount = await _context.Deliveries
            .CountAsync(x =>
                x.ReceiverUserId == user.Id &&
                x.Status == DeliveryStatus.Delivered);

        // Kullanıcının bağışçı veya alıcı olduğu süresi dolan teslimatlar.
        var expiredDeliveryCount = await _context.Deliveries
            .CountAsync(x =>
                (x.DonorUserId == user.Id || x.ReceiverUserId == user.Id) &&
                x.Status == DeliveryStatus.Expired);

        // Kullanıcının kazandığı toplam eco puan.
        var totalEcoPoint = await _context.EcoPointHistories
            .Where(x => x.UserId == user.Id)
            .SumAsync(x => (int?)x.Point) ?? 0;

        // Kullanıcının karbon raporlarındaki toplam tahmini karbon tasarrufu.
        var totalCarbonSavedKg = await _context.CarbonReports
            .Where(x => x.UserId == user.Id)
            .SumAsync(x => (decimal?)x.EstimatedCarbonSaved) ?? 0;

        return new AdminUserDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            City = user.City,
            District = user.District,
            Neighborhood = user.Neighborhood,
            NeedScore = user.NeedScore,
            ReliabilityScore = user.ReliabilityScore,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            DeletedAt = user.DeletedAt,
            Roles = roles.ToList(),

            StockItemCount = stockItemCount,
            ShareListingCount = shareListingCount,
            ActiveShareListingCount = activeShareListingCount,
            DonatedDeliveryCount = donatedDeliveryCount,
            ReceivedDeliveryCount = receivedDeliveryCount,
            CompletedDonatedDeliveryCount = completedDonatedDeliveryCount,
            CompletedReceivedDeliveryCount = completedReceivedDeliveryCount,
            ExpiredDeliveryCount = expiredDeliveryCount,
            TotalEcoPoint = totalEcoPoint,
            TotalCarbonSavedKg = totalCarbonSavedKg
        };
    }
    public async Task<List<AdminUserStockDto>> GetUserStocksAsync(string userId)
    {
        // Admin panelinde kullanıcının stok kayıtları ürün, birim ve son risk bilgisiyle listelenir.
        var stockItems = await _context.StockItems
            .Include(x => x.Product)
            .Include(x => x.Unit)
            .Include(x => x.WasteRiskPredictions)
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        return stockItems.Select(x => new AdminUserStockDto
        {
            Id = x.Id,
            ProductName = x.Product.Name,
            Quantity = x.Quantity,
            UnitName = x.Unit.ShortName,
            ExpirationDate = x.ExpirationDate,
            Status = x.Status.ToString(),
            RiskLevel = x.WasteRiskPredictions
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => r.RiskLevel.ToString())
                .FirstOrDefault(),
            IsActive = x.IsActive,
            CreatedAt = x.CreatedAt
        }).ToList();
    }

    public async Task<List<AdminUserShareListingDto>> GetUserShareListingsAsync(string userId)
    {
        // Admin panelinde kullanıcının oluşturduğu paylaşım ilanları listelenir.
        var listings = await _context.ShareListings
            .Include(x => x.StockItem)
                .ThenInclude(x => x.Product)
            .Include(x => x.StockItem)
                .ThenInclude(x => x.Unit)
            .Include(x => x.DeliveryPoint)
            .Where(x => x.DonorUserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        return listings.Select(x => new AdminUserShareListingDto
        {
            Id = x.Id,
            ProductName = x.StockItem.Product.Name,
            Quantity = x.Quantity,
            UnitName = x.StockItem.Unit.ShortName,
            DeliveryPointName = x.DeliveryPoint != null ? x.DeliveryPoint.Name : "-",
            Status = x.Status.ToString(),
            CreatedAt = x.CreatedAt
        }).ToList();
    }

    public async Task<List<AdminUserDeliveryDto>> GetUserDeliveriesAsync(string userId)
    {
        // Admin panelinde kullanıcının bağışçı veya alıcı olduğu tüm teslimatlar listelenir.
        var deliveries = await _context.Deliveries
            .Include(x => x.ShareListing)
                .ThenInclude(x => x.StockItem)
                    .ThenInclude(x => x.Product)
            .Include(x => x.ShareListing)
                .ThenInclude(x => x.StockItem)
                    .ThenInclude(x => x.Unit)
            .Include(x => x.DeliveryPoint)
            .Include(x => x.DeliveryBox)
            .Where(x => x.DonorUserId == userId || x.ReceiverUserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        return deliveries.Select(x => new AdminUserDeliveryDto
        {
            Id = x.Id,
            Role = x.DonorUserId == userId ? "Bağışçı" : "Alıcı",
            ProductName = x.ShareListing.StockItem.Product.Name,
            Quantity = x.ShareListing.Quantity,
            UnitName = x.ShareListing.StockItem.Unit.ShortName,
            DeliveryPointName = x.DeliveryPoint?.Name,
            BoxCode = x.DeliveryBox?.BoxCode,
            Status = x.Status.ToString(),
            ExpiresAt = x.ExpiresAt,
            DroppedOffAt = x.DroppedOffAt,
            DeliveredAt = x.DeliveredAt,
            CreatedAt = x.CreatedAt
        }).ToList();
    }

    public async Task<List<AdminShareListingDto>> GetShareListingsAsync()
    {
        // Admin panelinde sistem genelindeki tüm paylaşım ilanları izleme amaçlı listelenir.
        var listings = await _context.ShareListings
            .Include(x => x.StockItem)
                .ThenInclude(x => x.Product)
            .Include(x => x.StockItem)
                .ThenInclude(x => x.Unit)
            .Include(x => x.DeliveryPoint)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        var donorIds = listings
            .Select(x => x.DonorUserId)
            .Distinct()
            .ToList();

        var donors = await _userManager.Users
            .Where(x => donorIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id);

        return listings.Select(x =>
        {
            donors.TryGetValue(x.DonorUserId, out var donor);

            return new AdminShareListingDto
            {
                Id = x.Id,
                DonorUserId = x.DonorUserId,
                DonorFullName = donor?.FullName ?? "-",
                DonorEmail = donor?.Email ?? "-",
                ProductName = x.StockItem.Product.Name,
                Quantity = x.Quantity,
                UnitName = x.StockItem.Unit.ShortName,
                DeliveryPointName = x.DeliveryPoint?.Name,
                City = x.DeliveryPoint?.City,
                District = x.DeliveryPoint?.District,
                Status = x.Status.ToString(),
                IsActive = x.IsActive,
                CreatedAt = x.CreatedAt
            };
        }).ToList();
    }
    // Admin panelinde sistem genelindeki tüm teslimatlar izleme amaçlı listelenir.
    public async Task<List<AdminDeliveryMonitorDto>> GetDeliveriesAsync()
    {
        // Admin panelinde sistem genelindeki tüm teslimatlar izleme amaçlı listelenir.
        var deliveries = await _context.Deliveries
            .Include(x => x.ShareListing)
                .ThenInclude(x => x.StockItem)
                    .ThenInclude(x => x.Product)
            .Include(x => x.ShareListing)
                .ThenInclude(x => x.StockItem)
                    .ThenInclude(x => x.Unit)
            .Include(x => x.DeliveryPoint)
            .Include(x => x.DeliveryBox)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        var userIds = deliveries
            .SelectMany(x => new[] { x.DonorUserId, x.ReceiverUserId })
            .Distinct()
            .ToList();

        var users = await _userManager.Users
            .Where(x => userIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id);

        return deliveries.Select(x =>
        {
            users.TryGetValue(x.DonorUserId, out var donor);
            users.TryGetValue(x.ReceiverUserId, out var receiver);

            return new AdminDeliveryMonitorDto
            {
                Id = x.Id,

                DonorUserId = x.DonorUserId,
                DonorFullName = donor?.FullName ?? "-",
                DonorEmail = donor?.Email ?? "-",

                ReceiverUserId = x.ReceiverUserId,
                ReceiverFullName = receiver?.FullName ?? "-",
                ReceiverEmail = receiver?.Email ?? "-",

                ProductName = x.ShareListing.StockItem.Product.Name,
                Quantity = x.ShareListing.Quantity,
                UnitName = x.ShareListing.StockItem.Unit.ShortName,

                DeliveryPointName = x.DeliveryPoint?.Name,
                BoxCode = x.DeliveryBox?.BoxCode,

                Status = x.Status.ToString(),
                ExpiresAt = x.ExpiresAt,
                DroppedOffAt = x.DroppedOffAt,
                DeliveredAt = x.DeliveredAt,
                CreatedAt = x.CreatedAt
            };
        }).ToList();
    }
}
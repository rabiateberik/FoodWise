using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// StockService, kullanıcının stok ürünlerini yönetir.
// Ürün ekleme, listeleme, güncelleme, silme ve basit risk tahmini işlemleri burada yapılır.
using FoodWise.Application.DTOs.Stock;
using FoodWise.Application.Interfaces;
using FoodWise.Domain.Entities;
using FoodWise.Domain.Enums;
using FoodWise.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FoodWise.Infrastructure.Services;

public class StockService : IStockService
{
    private readonly FoodWiseDbContext _context;

    public StockService(FoodWiseDbContext context)
    {
        _context = context;
    }

    public async Task<List<StockItemDto>> GetUserStockAsync(string userId)
    {
        // Kullanıcının aktif stok ürünleri ürün, birim ve risk bilgileriyle birlikte çekilir.
        var stockItems = await _context.StockItems
            .Include(x => x.Product)
            .Include(x => x.Unit)
            .Include(x => x.WasteRiskPredictions)
            .Where(x => x.UserId == userId && x.Status == StockItemStatus.Active)
            .OrderBy(x => x.ExpirationDate)
            .ToListAsync();

        return stockItems.Select(MapToDto).ToList();
    }

    public async Task<List<StockItemDto>> GetRiskyStockItemsAsync(string userId)
    {
        // Risk seviyesi yüksek veya kritik olan stok ürünleri listelenir.
        var stockItems = await _context.StockItems
            .Include(x => x.Product)
            .Include(x => x.Unit)
            .Include(x => x.WasteRiskPredictions)
            .Where(x =>
                x.UserId == userId &&
                x.Status == StockItemStatus.Active &&
                x.WasteRiskPredictions.Any(r =>
                    r.RiskLevel == RiskLevel.High ||
                    r.RiskLevel == RiskLevel.Critical))
            .OrderBy(x => x.ExpirationDate)
            .ToListAsync();

        return stockItems.Select(MapToDto).ToList();
    }

    public async Task<StockItemDto?> GetByIdAsync(int id, string userId)
    {
        // Sadece giriş yapan kullanıcının kendi stok kaydı getirilebilir.
        var stockItem = await _context.StockItems
            .Include(x => x.Product)
            .Include(x => x.Unit)
            .Include(x => x.WasteRiskPredictions)
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);

        return stockItem == null ? null : MapToDto(stockItem);
    }

    public async Task<StockItemDto> CreateAsync(string userId, CreateStockItemDto model)
    {
        // Ürün sistemde varsa mevcut ProductId kullanılır.
        // Ürün sistemde yoksa ProductName ile yeni ürün otomatik oluşturulur.
        var product = await GetOrCreateProductAsync(userId, model.ProductId, model.ProductName);

        // Yeni stok kaydı oluşturulur.
        var stockItem = new StockItem
        {
            UserId = userId,
            ProductId = product.Id,
            UnitId = model.UnitId,
            Quantity = model.Quantity,
            ExpirationDate = model.ExpirationDate,
            OpenedDate = model.OpenedDate,
            StorageCondition = model.StorageCondition,
            Status = StockItemStatus.Active,
            ImageUrl = model.ImageUrl,
            Note = model.Note,
            CreatedAt = DateTime.Now
        };

        await _context.StockItems.AddAsync(stockItem);
        await _context.SaveChangesAsync();

        // Ürün eklendikten sonra otomatik risk tahmini oluşturulur.
        await CreateRiskPredictionAsync(stockItem.Id);

        var createdStockItem = await _context.StockItems
            .Include(x => x.Product)
            .Include(x => x.Unit)
            .Include(x => x.WasteRiskPredictions)
            .FirstAsync(x => x.Id == stockItem.Id);

        return MapToDto(createdStockItem);
    }
    private async Task<Product> GetOrCreateProductAsync(string userId, CreateStockItemDto model)
    {
        // Kullanıcı öneri listesinden mevcut ürünü seçtiyse o ürün kullanılır.
        if (model.ProductId.HasValue && model.ProductId.Value > 0)
        {
            var existingProductById = await _context.Products
                .FirstOrDefaultAsync(x =>
                    x.Id == model.ProductId.Value &&
                    x.IsActive &&
                    x.IsApproved);

            if (existingProductById != null)
                return existingProductById;
        }

        var productName = model.ProductName?.Trim();

        if (string.IsNullOrWhiteSpace(productName))
            throw new InvalidOperationException("Ürün adı boş olamaz.");

        // Aynı isimde aktif ürün varsa yeni kayıt açmadan mevcut ürün kullanılır.
        var existingProductByName = await _context.Products
            .FirstOrDefaultAsync(x =>
                x.Name.ToLower() == productName.ToLower() &&
                x.IsActive);

        if (existingProductByName != null)
            return existingProductByName;

        // Kullanıcı listede olmayan bir ürün yazarsa ürün otomatik olarak Diğer kategorisine eklenir.
        var otherCategory = await GetOrCreateOtherCategoryAsync();

        var newProduct = new Product
        {
            CategoryId = otherCategory.Id,
            Name = productName,
            DefaultShelfLifeDays = 7,
            OpenedShelfLifeDays = null,
            CarbonFactor = 1,
            IsSensitiveFood = false,

            // Admin paneli gelene kadar kullanıcı ürünleri otomatik onaylı kabul edilir.
            IsSystemDefined = false,
            IsApproved = true,
            CreatedByUserId = userId,

            CreatedAt = DateTime.Now,
            IsActive = true
        };

        await _context.Products.AddAsync(newProduct);
        await _context.SaveChangesAsync();

        return newProduct;
    }

    private async Task<Category> GetOrCreateOtherCategoryAsync()
    {
        // Yeni kullanıcı ürünleri için varsayılan kategori olarak Diğer kullanılır.
        var otherCategory = await _context.Categories
            .FirstOrDefaultAsync(x => x.Name == "Diğer" && x.IsActive);

        if (otherCategory != null)
            return otherCategory;

        var category = new Category
        {
            Name = "Diğer",
            Description = "Kullanıcı tarafından eklenen ve belirli kategoriye atanamayan ürünler için varsayılan kategori.",
            CreatedAt = DateTime.Now,
            IsActive = true
        };

        await _context.Categories.AddAsync(category);
        await _context.SaveChangesAsync();

        return category;
    }
    public async Task<StockItemDto?> UpdateAsync(int id, string userId, UpdateStockItemDto model)
    {
        // Güncellenecek stok kaydı kullanıcıya ait mi kontrol edilir.
        var stockItem = await _context.StockItems
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);

        if (stockItem == null)
            return null;

        // Ürün ve birim bilgileri de düzenleme formundan gelen değerlere göre güncellenir.
        // Ürün adı değiştirildiyse mevcut ürün bulunur veya yeni ürün otomatik oluşturulur.
        var product = await GetOrCreateProductAsync(userId, model.ProductId, model.ProductName);

        stockItem.ProductId = product.Id;
        stockItem.UnitId = model.UnitId;

        stockItem.Quantity = model.Quantity;
        stockItem.ExpirationDate = model.ExpirationDate;
        stockItem.OpenedDate = model.OpenedDate;
        stockItem.StorageCondition = model.StorageCondition;
        stockItem.ImageUrl = model.ImageUrl;
        stockItem.Note = model.Note;
        stockItem.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        // Ürün, tarih veya saklama bilgisi değişmiş olabileceği için risk tahmini yeniden hesaplanır.
        await CreateRiskPredictionAsync(stockItem.Id);

        var updatedStockItem = await _context.StockItems
            .Include(x => x.Product)
            .Include(x => x.Unit)
            .Include(x => x.WasteRiskPredictions)
            .FirstAsync(x => x.Id == stockItem.Id);

        return MapToDto(updatedStockItem);
    }
    private async Task<Product> GetOrCreateProductAsync(string userId, int? productId, string? productName)
    {
        var normalizedProductName = productName?.Trim();

        // Ürün adı varsa önce ada göre arama yapılır.
        // Böylece kullanıcı düzenleme ekranında ürün adını değiştirirse yeni ad dikkate alınır.
        if (!string.IsNullOrWhiteSpace(normalizedProductName))
        {
            var existingProductByName = await _context.Products
                .FirstOrDefaultAsync(x =>
                    x.Name.ToLower() == normalizedProductName.ToLower() &&
                    x.IsActive &&
                    x.IsApproved);

            if (existingProductByName != null)
                return existingProductByName;

            var otherCategory = await GetOrCreateOtherCategoryAsync();

            var newProduct = new Product
            {
                CategoryId = otherCategory.Id,
                Name = normalizedProductName,
                DefaultShelfLifeDays = 7,
                OpenedShelfLifeDays = null,
                CarbonFactor = 1,
                IsSensitiveFood = false,
                IsSystemDefined = false,
                IsApproved = true,
                CreatedByUserId = userId,
                CreatedAt = DateTime.Now,
                IsActive = true
            };

            await _context.Products.AddAsync(newProduct);
            await _context.SaveChangesAsync();

            return newProduct;
        }

        // Ürün adı boş geldiyse ProductId üzerinden mevcut ürün aranır.
        if (productId.HasValue && productId.Value > 0)
        {
            var existingProductById = await _context.Products
                .FirstOrDefaultAsync(x =>
                    x.Id == productId.Value &&
                    x.IsActive &&
                    x.IsApproved);

            if (existingProductById != null)
                return existingProductById;
        }

        throw new InvalidOperationException("Ürün adı boş olamaz.");
    }
    public async Task<bool> DeleteAsync(int id, string userId)
    {
        // Silinecek stok kaydı kullanıcıya ait mi kontrol edilir.
        var stockItem = await _context.StockItems
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);

        if (stockItem == null)
            return false;

        // Stok ürünü aktif bir paylaşım ilanında kullanılıyorsa silinmez.
        // Böylece devam eden paylaşım/talep/teslimat akışı bozulmaz.
        var hasActiveShareListing = await _context.ShareListings
            .AnyAsync(x =>
                x.StockItemId == id &&
                x.Status != ShareListingStatus.Cancelled &&
                x.Status != ShareListingStatus.Delivered);

        if (hasActiveShareListing)
            return false;

        // Fiziksel silmek yerine durum Deleted yapılır.
        // Böylece raporlama ve geçmiş veri için kayıt korunur.
        stockItem.Status = StockItemStatus.Deleted;
        stockItem.IsActive = false;
        stockItem.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        return true;
    }

    private async Task CreateRiskPredictionAsync(int stockItemId)
    {
        // Risk hesaplaması için ürün ve stok bilgileri birlikte alınır.
        var stockItem = await _context.StockItems
            .Include(x => x.Product)
            .FirstAsync(x => x.Id == stockItemId);

        var daysRemaining = (stockItem.ExpirationDate.Date - DateTime.Now.Date).Days;
        var riskScore = CalculateRiskScore(stockItem, daysRemaining);
        var riskLevel = GetRiskLevel(riskScore);
        var recommendationType = GetRecommendationType(riskScore, daysRemaining);

        var riskPrediction = new WasteRiskPrediction
        {
            StockItemId = stockItem.Id,
            RiskScore = riskScore,
            RiskLevel = riskLevel,
            DaysRemaining = daysRemaining,
            PredictedWasteDate = stockItem.ExpirationDate,
            RecommendationType = recommendationType,
            RecommendationText = CreateRecommendationText(riskLevel, recommendationType),
            CalculatedAt = DateTime.Now,
            CreatedAt = DateTime.Now
        };

        await _context.WasteRiskPredictions.AddAsync(riskPrediction);
        await _context.SaveChangesAsync();
    }

    private int CalculateRiskScore(StockItem stockItem, int daysRemaining)
    {
        var score = 0;

        // Son kullanma tarihi yaklaştıkça risk puanı artar.
        if (daysRemaining <= 0)
            score += 80;
        else if (daysRemaining <= 1)
            score += 60;
        else if (daysRemaining <= 3)
            score += 40;
        else if (daysRemaining <= 7)
            score += 25;

        // Ürün açılmışsa bozulma riski artar.
        if (stockItem.OpenedDate.HasValue)
            score += 20;

        // Hassas gıdalar için ek risk puanı verilir.
        if (stockItem.Product.IsSensitiveFood)
            score += 10;

        // Risk puanı 100'ü geçmemelidir.
        return Math.Min(score, 100);
    }

    private RiskLevel GetRiskLevel(int riskScore)
    {
        if (riskScore >= 90)
            return RiskLevel.Critical;

        if (riskScore >= 70)
            return RiskLevel.High;

        if (riskScore >= 40)
            return RiskLevel.Medium;

        return RiskLevel.Low;
    }

    private RecommendationType GetRecommendationType(int riskScore, int daysRemaining)
    {
        // Çok yüksek risk varsa paylaşım önerilir.
        if (riskScore >= 70 || daysRemaining <= 1)
            return RecommendationType.Share;

        // Orta riskte önce tarif önerisi daha uygundur.
        if (riskScore >= 40)
            return RecommendationType.Recipe;

        return RecommendationType.Consume;
    }

    private string CreateRecommendationText(RiskLevel riskLevel, RecommendationType recommendationType)
    {
        return recommendationType switch
        {
            RecommendationType.Share => "Bu ürünün israf riski yüksek. Tüketemeyecekseniz güvenli teslim noktası üzerinden paylaşabilirsiniz.",
            RecommendationType.Recipe => "Bu ürünün son kullanma tarihi yaklaşıyor. Tarif önerilerini inceleyerek değerlendirebilirsiniz.",
            RecommendationType.Consume => "Bu ürün şu an düşük riskli. Planlı tüketim önerilir.",
            _ => "Bu ürün için özel bir öneri bulunmamaktadır."
        };
    }

    private StockItemDto MapToDto(StockItem stockItem)
    {
        // En güncel risk tahmini kullanıcıya gösterilir.
        var latestRisk = stockItem.WasteRiskPredictions
            .OrderByDescending(x => x.CalculatedAt)
            .FirstOrDefault();

        return new StockItemDto
        {
            Id = stockItem.Id,
            ProductId = stockItem.ProductId,
            ProductName = stockItem.Product.Name,
            UnitId = stockItem.UnitId,
            UnitName = stockItem.Unit.ShortName,
            Quantity = stockItem.Quantity,
            ExpirationDate = stockItem.ExpirationDate,
            OpenedDate = stockItem.OpenedDate,
            StorageCondition = stockItem.StorageCondition.ToString(),
            Status = stockItem.Status.ToString(),
            RiskScore = latestRisk?.RiskScore,
            RiskLevel = latestRisk?.RiskLevel.ToString(),
            RecommendationText = latestRisk?.RecommendationText,
            Note = stockItem.Note
        };
    }
}
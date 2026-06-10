// StockService, kullanıcının stok ürünlerini yönetir.
// Ürün ekleme, listeleme, güncelleme, silme, süresi geçen ürünleri ayırma ve basit risk tahmini işlemleri burada yapılır.

using FoodWise.Application.DTOs.Stock;
using FoodWise.Application.Interfaces;
using FoodWise.Domain.Entities;
using FoodWise.Domain.Enums;
using FoodWise.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using FoodWise.Application.DTOs.RiskPrediction;

namespace FoodWise.Infrastructure.Services;

public class StockService : IStockService
{
    private readonly FoodWiseDbContext _context;
    private readonly IMlRiskPredictionService _mlRiskPredictionService;

    public StockService(FoodWiseDbContext context, IMlRiskPredictionService mlRiskPredictionService)
    {
        _context = context;
        _mlRiskPredictionService = mlRiskPredictionService;
    }

    public async Task<List<StockItemDto>> GetUserStockAsync(string userId)
    {
        // Son tüketim tarihi geçmiş aktif ürünler otomatik olarak Expired durumuna alınır.
        await MarkExpiredStockItemsAsync(userId);

        // Kullanıcının aktif stok ürünleri ürün, birim ve risk bilgileriyle birlikte çekilir.
        var stockItems = await _context.StockItems
            .Include(x => x.Product)
            .Include(x => x.Unit)
            .Include(x => x.WasteRiskPredictions)
            .Where(x =>
                x.UserId == userId &&
                x.Status == StockItemStatus.Active &&
                x.IsActive)
            .OrderBy(x => x.ExpirationDate)
            .ToListAsync();

        // Her stok ürününün aktif paylaşım ilanında kullanılıp kullanılmadığı kontrol edilir.
        var activeShareListingMap = await GetActiveShareListingMapAsync(
            userId,
            stockItems.Select(x => x.Id).ToList());

        return stockItems
            .Select(x => MapToDto(x, activeShareListingMap))
            .ToList();
    }

    public async Task<List<StockItemDto>> GetRiskyStockItemsAsync(string userId)
    {
        // Son tüketim tarihi geçmiş aktif ürünler riskli ürünler yerine Expired durumuna alınır.
        await MarkExpiredStockItemsAsync(userId);

        // Risk seviyesi yüksek veya kritik olan aktif stok ürünleri listelenir.
        var stockItems = await _context.StockItems
            .Include(x => x.Product)
            .Include(x => x.Unit)
            .Include(x => x.WasteRiskPredictions)
            .Where(x =>
                x.UserId == userId &&
                x.Status == StockItemStatus.Active &&
                x.IsActive &&
                x.WasteRiskPredictions.Any(r =>
                    r.RiskLevel == RiskLevel.High ||
                    r.RiskLevel == RiskLevel.Critical))
            .OrderBy(x => x.ExpirationDate)
            .ToListAsync();

        var activeShareListingMap = await GetActiveShareListingMapAsync(
            userId,
            stockItems.Select(x => x.Id).ToList());

        return stockItems
            .Select(x => MapToDto(x, activeShareListingMap))
            .ToList();
    }

    public async Task<List<StockItemDto>> GetExpiredStockItemsAsync(string userId)
    {
        // Sayfa açıldığında yeni süresi geçen ürünler varsa Expired durumuna alınır.
        await MarkExpiredStockItemsAsync(userId);

        var stockItems = await _context.StockItems
            .Include(x => x.Product)
            .Include(x => x.Unit)
            .Include(x => x.WasteRiskPredictions)
            .Where(x =>
                x.UserId == userId &&
                x.Status == StockItemStatus.Expired &&
                x.IsActive)
            .OrderByDescending(x => x.ExpirationDate)
            .ToListAsync();

        return stockItems
            .Select(x => MapToDto(x))
            .ToList();
    }

    public async Task<StockItemDto?> GetByIdAsync(int id, string userId)
    {
        // Süresi geçmiş ürün düzenleme ekranına düşmesin diye önce durum kontrolü yapılır.
        await MarkExpiredStockItemsAsync(userId);

        // Sadece giriş yapan kullanıcının kendi aktif stok kaydı getirilebilir.
        var stockItem = await _context.StockItems
            .Include(x => x.Product)
            .Include(x => x.Unit)
            .Include(x => x.WasteRiskPredictions)
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.UserId == userId &&
                x.Status == StockItemStatus.Active &&
                x.IsActive);

        if (stockItem == null)
            return null;

        var activeShareListingMap = await GetActiveShareListingMapAsync(
            userId,
            new List<int> { stockItem.Id });

        return MapToDto(stockItem, activeShareListingMap);
    }

    public async Task<StockItemDto> CreateAsync(string userId, CreateStockItemDto model)
    {
        // Ürün sistemde varsa mevcut Product kullanılır.
        // Ürün sistemde yoksa ProductName ile yeni ürün otomatik oluşturulur.
        var product = await GetOrCreateProductAsync(userId, model.ProductId, model.ProductName);

        var stockItem = new StockItem
        {
            UserId = userId,
            ProductId = product.Id,
            UnitId = model.UnitId,
            Quantity = model.Quantity,
            ExpirationDate = model.ExpirationDate,
            OpenedDate = model.OpenedDate,
            StorageCondition = model.StorageCondition,

            // Geçmiş tarihli ürün eklenirse aktif listeye değil süresi geçen ürünlere düşer.
            Status = model.ExpirationDate.Date < DateTime.Now.Date
                ? StockItemStatus.Expired
                : StockItemStatus.Active,

            ImageUrl = model.ImageUrl,
            Note = model.Note,
            CreatedAt = DateTime.Now,
            IsActive = true
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

    public async Task<StockItemDto?> UpdateAsync(int id, string userId, UpdateStockItemDto model)
    {
        // Güncellenecek stok kaydı kullanıcıya ait mi kontrol edilir.
        var stockItem = await _context.StockItems
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.UserId == userId &&
                x.Status == StockItemStatus.Active &&
                x.IsActive);

        if (stockItem == null)
            return null;

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

        // Güncelleme sonrası tarih geçmişe çekildiyse ürün Expired olur.
        stockItem.Status = model.ExpirationDate.Date < DateTime.Now.Date
            ? StockItemStatus.Expired
            : StockItemStatus.Active;

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

    public async Task<bool> DeleteAsync(int id, string userId)
    {
        // Silmeden önce süresi geçen ürünler güncellenir.
        await MarkExpiredStockItemsAsync(userId);

        // Aktif veya süresi geçmiş stok kaydı kullanıcıya ait mi kontrol edilir.
        var stockItem = await _context.StockItems
            .FirstOrDefaultAsync(x =>
                x.Id == id &&
                x.UserId == userId &&
                x.IsActive &&
                (x.Status == StockItemStatus.Active ||
                 x.Status == StockItemStatus.Expired));

        if (stockItem == null)
            return false;

        // Stok ürünü aktif bir paylaşım ilanında kullanılıyorsa silinmez.
        // Böylece devam eden paylaşım/talep/teslimat akışı bozulmaz.
        var hasActiveShareListing = await _context.ShareListings
            .AnyAsync(x =>
                x.StockItemId == id &&
                x.DonorUserId == userId &&
                x.IsActive &&
                x.Status != ShareListingStatus.Cancelled &&
                x.Status != ShareListingStatus.Expired &&
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

    private async Task MarkExpiredStockItemsAsync(string userId)
    {
        var today = DateTime.Now.Date;

        var expiredStockItems = await _context.StockItems
            .Where(x =>
                x.UserId == userId &&
                x.Status == StockItemStatus.Active &&
                x.IsActive &&
                x.ExpirationDate.Date < today)
            .ToListAsync();

        if (!expiredStockItems.Any())
            return;

        var expiredStockItemIds = expiredStockItems
            .Select(x => x.Id)
            .ToList();

        foreach (var stockItem in expiredStockItems)
        {
            stockItem.Status = StockItemStatus.Expired;
            stockItem.UpdatedAt = DateTime.Now;
        }

        // Süresi geçen ürün aktif paylaşım ilanındaysa o ilan da süresi geçmiş yapılır.
        // Böylece süresi geçmiş ürün başkalarına gösterilmez.
        var activeShareListings = await _context.ShareListings
            .Where(x =>
                expiredStockItemIds.Contains(x.StockItemId) &&
                x.DonorUserId == userId &&
                x.IsActive &&
                x.Status != ShareListingStatus.Cancelled &&
                x.Status != ShareListingStatus.Expired &&
                x.Status != ShareListingStatus.Delivered)
            .ToListAsync();

        foreach (var listing in activeShareListings)
        {
            listing.Status = ShareListingStatus.Expired;
            listing.UpdatedAt = DateTime.Now;
        }

        await _context.SaveChangesAsync();
    }

    private async Task<Product> GetOrCreateProductAsync(string userId, int? productId, string? productName)
    {
        var normalizedProductName = productName?.Trim();

        // Ürün adı varsa önce ada göre arama yapılır.
        // Böylece kullanıcı yeni ürün yazarsa bu ad dikkate alınır.
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

    private async Task<Dictionary<int, int>> GetActiveShareListingMapAsync(string userId, List<int> stockItemIds)
    {
        if (stockItemIds == null || !stockItemIds.Any())
            return new Dictionary<int, int>();

        // Aktif paylaşım ilanları bulunur.
        // İptal edilmiş, süresi dolmuş veya teslim edilmiş ilanlar aktif kabul edilmez.
        var activeListings = await _context.ShareListings
            .Where(x =>
                stockItemIds.Contains(x.StockItemId) &&
                x.DonorUserId == userId &&
                x.IsActive &&
                x.Status != ShareListingStatus.Cancelled &&
                x.Status != ShareListingStatus.Expired &&
                x.Status != ShareListingStatus.Delivered)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        return activeListings
            .GroupBy(x => x.StockItemId)
            .ToDictionary(x => x.Key, x => x.First().Id);
    }

    private async Task CreateRiskPredictionAsync(int stockItemId)
    {
        // Risk hesaplaması için ürün, kategori ve stok bilgileri birlikte alınır.
        var stockItem = await _context.StockItems
            .Include(x => x.Product)
                .ThenInclude(x => x.Category)
            .FirstAsync(x => x.Id == stockItemId);

        var daysRemaining = (stockItem.ExpirationDate.Date - DateTime.Now.Date).Days;

        // Önce eski kural tabanlı risk sonucu hazırlanır.
        // ML servisi çalışmazsa sistem bu sonuca geri döner.
        var ruleBasedRiskScore = CalculateRiskScore(stockItem, daysRemaining);
        var ruleBasedRiskLevel = GetRiskLevel(ruleBasedRiskScore);

        var finalRiskScore = ruleBasedRiskScore;
        var finalRiskLevel = ruleBasedRiskLevel;

        // Python ML servisi çalışıyorsa tahmin sonucu alınır.
        var mlPrediction = await TryPredictRiskWithMlAsync(stockItem, daysRemaining);
        if (mlPrediction != null && !string.IsNullOrWhiteSpace(mlPrediction.RiskLabel))
        {
            Console.WriteLine($"ML risk tahmini kullanıldı: {mlPrediction.RiskLabel}");

            finalRiskLevel = ConvertMlRiskLabelToRiskLevel(mlPrediction.RiskLabel);
            finalRiskScore = ConvertMlRiskLabelToRiskScore(mlPrediction);
        }
        else
        {
            Console.WriteLine($"ML servisi kullanılamadı. Kural tabanlı risk kullanıldı: {ruleBasedRiskLevel}");
        }

        // ML sonucu geldikten sonra FoodWise iş kurallarıyla güvenlik kontrolü yapılır.
        // Böylece açılış tarihi daha eski olan ürün yanlışlıkla düşük riskte kalmaz.
        finalRiskScore = Math.Max(finalRiskScore, ruleBasedRiskScore);
        finalRiskScore = ApplyBusinessRiskGuards(finalRiskScore, stockItem, daysRemaining);
        finalRiskLevel = GetRiskLevel(finalRiskScore);

        Console.WriteLine($"Final risk sonucu: {finalRiskLevel} - Skor: {finalRiskScore}");

        var recommendationType = GetRecommendationType(finalRiskScore, daysRemaining);

        var riskPrediction = new WasteRiskPrediction
        {
            StockItemId = stockItem.Id,
            RiskScore = finalRiskScore,
            RiskLevel = finalRiskLevel,
            DaysRemaining = daysRemaining,
            PredictedWasteDate = stockItem.ExpirationDate,
            RecommendationType = recommendationType,
            RecommendationText = CreateRecommendationText(finalRiskLevel, recommendationType),
            CalculatedAt = DateTime.Now,
            CreatedAt = DateTime.Now
        };

        await _context.WasteRiskPredictions.AddAsync(riskPrediction);
        await _context.SaveChangesAsync();
    }
    private async Task<MlRiskPredictionResponseDto?> TryPredictRiskWithMlAsync(
    StockItem stockItem,
    int daysRemaining)
    {
        try
        {
            var request = new MlRiskPredictionRequestDto
            {
                ProductName = stockItem.Product.Name,
                Category = stockItem.Product.Category?.Name ?? "Diğer",
                StorageCondition = ConvertStorageConditionToMlText(stockItem.StorageCondition),
                DaysUntilExpiration = daysRemaining,
                DaysSinceOpened = CalculateDaysSinceOpened(stockItem.OpenedDate),
                IsOpened = stockItem.OpenedDate.HasValue,
                IsSensitive = stockItem.Product.IsSensitiveFood,
                Quantity = stockItem.Quantity,
                PreviousWasteCount = await GetPreviousWasteCountAsync(stockItem),
                PreviousSharedCount = await GetPreviousSharedCountAsync(stockItem),
                Season = GetCurrentSeasonText()
            };

            return await _mlRiskPredictionService.PredictRiskAsync(request);
        }
        catch
        {
            // ML tarafında hata olursa StockService eski kural tabanlı risk hesabıyla devam eder.
            return null;
        }
    }

    private int CalculateDaysSinceOpened(DateTime? openedDate)
    {
        if (!openedDate.HasValue)
            return 0;

        var days = (DateTime.Now.Date - openedDate.Value.Date).Days;

        return Math.Max(days, 0);
    }

    private async Task<int> GetPreviousWasteCountAsync(StockItem stockItem)
    {
        // Kullanıcının aynı üründe daha önce süresi geçmiş aktif/geçmiş kayıt sayısı alınır.
        return await _context.StockItems
            .CountAsync(x =>
                x.UserId == stockItem.UserId &&
                x.ProductId == stockItem.ProductId &&
                x.Id != stockItem.Id &&
                x.Status == StockItemStatus.Expired);
    }

    private async Task<int> GetPreviousSharedCountAsync(StockItem stockItem)
    {
        // Kullanıcının aynı ürünü daha önce başarıyla paylaşma sayısı alınır.
        return await _context.ShareListings
            .CountAsync(x =>
                x.DonorUserId == stockItem.UserId &&
                x.StockItem.ProductId == stockItem.ProductId &&
                x.Status == ShareListingStatus.Delivered);
    }

    private string ConvertStorageConditionToMlText(StorageCondition storageCondition)
    {
        var value = storageCondition.ToString();

        return value switch
        {
            "Refrigerated" => "Buzdolabı",
            "Refrigerator" => "Buzdolabı",
            "Fridge" => "Buzdolabı",
            "Cold" => "Buzdolabı",
            "Buzdolabi" => "Buzdolabı",
            "Buzdolabı" => "Buzdolabı",

            "RoomTemperature" => "Oda Sıcaklığı",
            "Room" => "Oda Sıcaklığı",
            "OdaSicakligi" => "Oda Sıcaklığı",
            "OdaSıcaklığı" => "Oda Sıcaklığı",

            "Freezer" => "Dondurucu",
            "Frozen" => "Dondurucu",
            "Dondurucu" => "Dondurucu",

            _ => "Buzdolabı"
        };
    }

    private string GetCurrentSeasonText()
    {
        var month = DateTime.Now.Month;

        return month switch
        {
            3 or 4 or 5 => "İlkbahar",
            6 or 7 or 8 => "Yaz",
            9 or 10 or 11 => "Sonbahar",
            _ => "Kış"
        };
    }

    private RiskLevel ConvertMlRiskLabelToRiskLevel(string riskLabel)
    {
        return riskLabel switch
        {
            "Critical" => RiskLevel.Critical,
            "High" => RiskLevel.High,
            "Medium" => RiskLevel.Medium,
            "Low" => RiskLevel.Low,
            _ => RiskLevel.Low
        };
    }

    private int ConvertMlRiskLabelToRiskScore(MlRiskPredictionResponseDto prediction)
    {
        var riskLabel = prediction.RiskLabel;

        var probability = prediction.Probabilities.TryGetValue(riskLabel, out var value)
            ? value
            : 0.75;

        var confidenceBonus = (int)Math.Round(probability * 10);

        var score = riskLabel switch
        {
            "Critical" => 90 + confidenceBonus,
            "High" => 70 + confidenceBonus,
            "Medium" => 45 + confidenceBonus,
            "Low" => 15 + confidenceBonus,
            _ => 20
        };

        return Math.Clamp(score, 0, 100);
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

        // Ürün açılmışsa sadece "açıldı" diye değil,
        // kaç gündür açık olduğuna göre risk artırılır.
        if (stockItem.OpenedDate.HasValue)
        {
            score += 20;

            var daysSinceOpened = CalculateDaysSinceOpened(stockItem.OpenedDate);

            if (daysSinceOpened >= 7)
                score += 30;
            else if (daysSinceOpened >= 5)
                score += 22;
            else if (daysSinceOpened >= 3)
                score += 14;
            else if (daysSinceOpened >= 1)
                score += 7;
        }

        // Hassas gıdalar için ek risk puanı verilir.
        if (stockItem.Product.IsSensitiveFood)
            score += 10;

        var storageText = ConvertStorageConditionToMlText(stockItem.StorageCondition);

        // Hassas ürün oda sıcaklığında saklanıyorsa risk daha yüksek kabul edilir.
        if (stockItem.Product.IsSensitiveFood && storageText == "Oda Sıcaklığı")
            score += 15;

        return Math.Min(score, 100);
    }
    private int ApplyBusinessRiskGuards(
    int currentRiskScore,
    StockItem stockItem,
    int daysRemaining)
    {
        var finalScore = currentRiskScore;
        var daysSinceOpened = CalculateDaysSinceOpened(stockItem.OpenedDate);
        var storageText = ConvertStorageConditionToMlText(stockItem.StorageCondition);

        // Son kullanma tarihi geçmiş veya çok yakın ürünler düşük riskte kalmamalıdır.
        if (daysRemaining <= 0)
            finalScore = Math.Max(finalScore, 90);
        else if (daysRemaining <= 1)
            finalScore = Math.Max(finalScore, 75);
        else if (daysRemaining <= 3)
            finalScore = Math.Max(finalScore, 55);

        // Açılış tarihi geçmişe gittikçe risk artmalıdır.
        if (stockItem.OpenedDate.HasValue)
        {
            if (daysSinceOpened >= 7)
                finalScore = Math.Max(finalScore, stockItem.Product.IsSensitiveFood ? 85 : 70);
            else if (daysSinceOpened >= 5)
                finalScore = Math.Max(finalScore, stockItem.Product.IsSensitiveFood ? 75 : 60);
            else if (daysSinceOpened >= 3)
                finalScore = Math.Max(finalScore, stockItem.Product.IsSensitiveFood ? 65 : 45);
            else if (daysSinceOpened >= 1)
                finalScore = Math.Max(finalScore, stockItem.Product.IsSensitiveFood ? 45 : 30);
        }

        // Hassas ürün oda sıcaklığında saklanıyorsa risk düşük kalmamalıdır.
        if (stockItem.Product.IsSensitiveFood && storageText == "Oda Sıcaklığı")
            finalScore = Math.Max(finalScore, 70);

        return Math.Clamp(finalScore, 0, 100);
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

    private StockItemDto MapToDto(
        StockItem stockItem,
        Dictionary<int, int>? activeShareListingMap = null)
    {
        // En güncel risk tahmini kullanıcıya gösterilir.
        var latestRisk = stockItem.WasteRiskPredictions
            .OrderByDescending(x => x.CalculatedAt)
            .FirstOrDefault();

        var hasActiveShareListing = activeShareListingMap != null &&
                                    activeShareListingMap.ContainsKey(stockItem.Id);

        var activeShareListingId = activeShareListingMap != null &&
                                   activeShareListingMap.TryGetValue(stockItem.Id, out var listingId)
            ? listingId
            : (int?)null;

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
            Note = stockItem.Note,

            HasActiveShareListing = hasActiveShareListing,
            ActiveShareListingId = activeShareListingId
        };
    }
}
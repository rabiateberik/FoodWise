using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FoodWise.Application.DTOs.Report;
using FoodWise.Application.Interfaces;
using FoodWise.Domain.Entities;
using FoodWise.Domain.Enums;
using FoodWise.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FoodWise.Infrastructure.Services;

// CarbonReportService, kullanıcının aylık karbon tasarrufu raporlarını oluşturur.
// Tamamlanan teslimatlar üzerinden kurtarılan gıda miktarını ve tahmini karbon tasarrufunu hesaplar.
public class CarbonReportService : ICarbonReportService
{
    private readonly FoodWiseDbContext _context;

    public CarbonReportService(FoodWiseDbContext context)
    {
        _context = context;
    }

    // Kullanıcının seçilen ay ve yıl için karbon raporunu oluşturur veya mevcut raporu günceller.
    public async Task<CarbonReportDto> GenerateMonthlyReportAsync(string userId, int month, int year)
    {
        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1);

        // Kullanıcının bağışçı veya alıcı olarak dahil olduğu tamamlanmış teslimatlar alınır.
        // Böylece kullanıcı hem ürün bağışladığında hem de ürün teslim aldığında karbon katkısı hesaplanır.
        var deliveredItems = await _context.Deliveries
            .Include(x => x.ShareListing)
                .ThenInclude(x => x.StockItem)
                    .ThenInclude(x => x.Product)
            .Include(x => x.ShareListing)
                .ThenInclude(x => x.StockItem)
                    .ThenInclude(x => x.Unit)
            .Where(x =>
                x.IsActive &&
                (x.DonorUserId == userId || x.ReceiverUserId == userId) &&
                x.Status == DeliveryStatus.Delivered &&
                x.DeliveredAt != null &&
                x.DeliveredAt >= startDate &&
                x.DeliveredAt < endDate)
            .ToListAsync();

        decimal savedFoodKg = 0;
        decimal estimatedCarbonSaved = 0;

        // Her teslimat için paylaşılan ürün miktarı kilograma çevrilir.
        // Ürünün karbon katsayısı ile çarpılarak yaklaşık karbon tasarrufu hesaplanır.
        foreach (var delivery in deliveredItems)
        {
            var listing = delivery.ShareListing;
            var stockItem = listing.StockItem;
            var unitShortName = stockItem.Unit.ShortName;
            var product = stockItem.Product;

            var quantityKg = ConvertToKg(listing.Quantity, unitShortName);

            savedFoodKg += quantityKg;
            estimatedCarbonSaved += quantityKg * product.CarbonFactor;
        }

        // Kullanıcının dahil olduğu tamamlanmış paylaşım sayısı hesaplanır.
        var sharedProductCount = deliveredItems
            .Select(x => x.ShareListingId)
            .Distinct()
            .Count();

        // Aynı ay içinde israf edilmiş veya son kullanımı geçmiş ürün sayısı hesaplanır.
        var wastedProductCount = await _context.StockItems
            .CountAsync(x =>
                x.IsActive &&
                x.UserId == userId &&
                (x.Status == StockItemStatus.Wasted || x.Status == StockItemStatus.Expired) &&
                x.ExpirationDate >= startDate &&
                x.ExpirationDate < endDate);

        // Aynı ay ve yıl için daha önce rapor oluşturulmuşsa tekrar kayıt açılmaz.
        // Mevcut rapor güncellenir, yoksa yeni rapor oluşturulur.
        var existingReport = await _context.CarbonReports
            .FirstOrDefaultAsync(x =>
                x.UserId == userId &&
                x.Month == month &&
                x.Year == year &&
                x.IsActive);

        if (existingReport == null)
        {
            existingReport = new CarbonReport
            {
                UserId = userId,
                Month = month,
                Year = year,
                SavedFoodKg = savedFoodKg,
                EstimatedCarbonSaved = estimatedCarbonSaved,
                SharedProductCount = sharedProductCount,
                WastedProductCount = wastedProductCount,
                CreatedAt = DateTime.Now,
                IsActive = true
            };

            await _context.CarbonReports.AddAsync(existingReport);
        }
        else
        {
            existingReport.SavedFoodKg = savedFoodKg;
            existingReport.EstimatedCarbonSaved = estimatedCarbonSaved;
            existingReport.SharedProductCount = sharedProductCount;
            existingReport.WastedProductCount = wastedProductCount;
            existingReport.UpdatedAt = DateTime.Now;
        }

        await _context.SaveChangesAsync();

        return MapToDto(existingReport);
    }

    // Kullanıcının belirli ay ve yıla ait karbon raporunu getirir.
    public async Task<CarbonReportDto?> GetMonthlyReportAsync(string userId, int month, int year)
    {
        var report = await _context.CarbonReports
            .FirstOrDefaultAsync(x =>
                x.UserId == userId &&
                x.Month == month &&
                x.Year == year &&
                x.IsActive);

        return report == null ? null : MapToDto(report);
    }

    // Kullanıcının tüm aktif karbon raporlarını en güncelden eskiye doğru listeler.
    public async Task<List<CarbonReportDto>> GetMyReportsAsync(string userId)
    {
        var reports = await _context.CarbonReports
            .Where(x => x.UserId == userId && x.IsActive)
            .OrderByDescending(x => x.Year)
            .ThenByDescending(x => x.Month)
            .ToListAsync();

        return reports.Select(MapToDto).ToList();
    }

    // Kullanıcının tüm raporlarından genel karbon tasarrufu özetini hesaplar.
    public async Task<CarbonReportSummaryDto> GetSummaryAsync(string userId)
    {
        var reports = await _context.CarbonReports
            .Where(x => x.UserId == userId && x.IsActive)
            .ToListAsync();

        return new CarbonReportSummaryDto
        {
            TotalSavedFoodKg = reports.Sum(x => x.SavedFoodKg),
            TotalEstimatedCarbonSaved = reports.Sum(x => x.EstimatedCarbonSaved),
            TotalSharedProductCount = reports.Sum(x => x.SharedProductCount),
            TotalWastedProductCount = reports.Sum(x => x.WastedProductCount),
            ReportCount = reports.Count
        };
    }

    // CarbonReport entity'sini API tarafında kullanılacak DTO yapısına dönüştürür.
    private static CarbonReportDto MapToDto(CarbonReport report)
    {
        return new CarbonReportDto
        {
            Id = report.Id,
            Month = report.Month,
            Year = report.Year,
            SavedFoodKg = report.SavedFoodKg,
            EstimatedCarbonSaved = report.EstimatedCarbonSaved,
            SharedProductCount = report.SharedProductCount,
            WastedProductCount = report.WastedProductCount,
            CreatedAt = report.CreatedAt,
            UpdatedAt = report.UpdatedAt
        };
    }

    // Farklı birimlerdeki ürün miktarlarını raporlama için yaklaşık kilogram değerine çevirir.
    private static decimal ConvertToKg(decimal quantity, string unitShortName)
    {
        // Bu dönüşüm raporlama için yaklaşık değer üretir.
        // İleride ürün bazlı ağırlık katsayısı eklenirse daha hassas hesaplama yapılabilir.
        return unitShortName.ToLower() switch
        {
            "kg" => quantity,
            "gr" => quantity / 1000,
            "lt" => quantity,
            "ml" => quantity / 1000,
            "adet" => quantity * 0.10m,
            "paket" => quantity * 0.50m,
            _ => quantity
        };
    }
}


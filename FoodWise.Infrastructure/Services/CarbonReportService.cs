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

public class CarbonReportService : ICarbonReportService
{
    private readonly FoodWiseDbContext _context;

    public CarbonReportService(FoodWiseDbContext context)
    {
        _context = context;
    }

    public async Task<CarbonReportDto> GenerateMonthlyReportAsync(string userId, int month, int year)
    {
        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1);

        // Kullanıcının bağışçı veya alıcı olarak dahil olduğu tamamlanmış teslimatlar alınır.
        // Böylece kullanıcı hem ürün bağışladığında hem de başkasından ürün teslim aldığında karbon katkısı hesaplanır.
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

        // Kullanıcının dahil olduğu teslimatı tamamlanan paylaşım sayısıdır.
        // Bağışladığı veya teslim aldığı tamamlanmış ürünler bu sayıya dahil edilir.
        var sharedProductCount = deliveredItems
            .Select(x => x.ShareListingId)
            .Distinct()
            .Count();

        // Kullanıcının aynı ay içinde israf/son kullanımı geçmiş olarak işaretlenen ürünleri.
        var wastedProductCount = await _context.StockItems
            .CountAsync(x =>
                x.IsActive &&
                x.UserId == userId &&
                (x.Status == StockItemStatus.Wasted || x.Status == StockItemStatus.Expired) &&
                x.ExpirationDate >= startDate &&
                x.ExpirationDate < endDate);

        // Aynı ay için daha önce rapor varsa güncellenir, yoksa yeni rapor oluşturulur.
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

    public async Task<List<CarbonReportDto>> GetMyReportsAsync(string userId)
    {
        var reports = await _context.CarbonReports
            .Where(x => x.UserId == userId && x.IsActive)
            .OrderByDescending(x => x.Year)
            .ThenByDescending(x => x.Month)
            .ToListAsync();

        return reports.Select(MapToDto).ToList();
    }

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

    private static decimal ConvertToKg(decimal quantity, string unitShortName)
    {
        // Bu dönüşüm raporlama için yaklaşık değer üretir.
        // İleride ürün bazlı ağırlık katsayısı eklenirse daha hassas hale getirilebilir.
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
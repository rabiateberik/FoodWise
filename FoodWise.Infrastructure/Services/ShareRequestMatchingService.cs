// ShareRequestMatchingService, paylaşım talebi için kullanıcı-ilan eşleşme skorunu hesaplar.
// Skor; konum yakınlığı, ihtiyaç puanı, güvenilirlik puanı, talep geçmişi ve ML modeli birlikte kullanılarak üretilir.
// ML servisi çalışmazsa sistem eski kural tabanlı skorla devam eder.

using FoodWise.Application.DTOs.ShareMatching;
using FoodWise.Application.Interfaces;
using FoodWise.Domain.Entities;
using FoodWise.Domain.Enums;
using FoodWise.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FoodWise.Infrastructure.Services;

public class ShareRequestMatchingService : IShareRequestMatchingService
{
    private readonly FoodWiseDbContext _context;
    private readonly IMlShareMatchingService _mlShareMatchingService;

    public ShareRequestMatchingService(
        FoodWiseDbContext context,
        IMlShareMatchingService mlShareMatchingService)
    {
        _context = context;
        _mlShareMatchingService = mlShareMatchingService;
    }

    public async Task<int> CalculateMatchScoreAsync(string requesterUserId, ShareListing listing)
    {
        var requester = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == requesterUserId && x.IsActive);

        if (requester == null)
            return 40;

        var listingDetail = await _context.ShareListings
            .AsNoTracking()
            .Include(x => x.DeliveryPoint)
            .Include(x => x.StockItem)
                .ThenInclude(x => x.Product)
                    .ThenInclude(x => x.Category)
            .Include(x => x.StockItem)
                .ThenInclude(x => x.WasteRiskPredictions)
            .FirstOrDefaultAsync(x => x.Id == listing.Id);

        if (listingDetail == null)
            return 40;

        var locationInfo = CalculateLocationInfo(
            requester.City,
            requester.District,
            requester.Neighborhood,
            listingDetail.DeliveryPoint?.City,
            listingDetail.DeliveryPoint?.District,
            listingDetail.DeliveryPoint?.Neighborhood
        );

        var locationScore = CalculateLocationScore(locationInfo.LocationPriority);
        var needScore = CalculateNeedScore(requester.NeedScore);
        var reliabilityScore = CalculateReliabilityScore(requester.ReliabilityScore);

        var requestHistory = await GetRequestHistorySummaryAsync(requesterUserId);
        var historyScore = CalculateRequestHistoryScore(requestHistory);

        var ruleBasedScore = locationScore + needScore + reliabilityScore + historyScore;
        var roundedRuleBasedScore = (int)Math.Clamp(Math.Round(ruleBasedScore), 0, 100);

        var mlRequest = await CreateMlShareMatchRequestAsync(
            requester,
            requesterUserId,
            listingDetail,
            locationInfo,
            requestHistory);

        var mlResult = await _mlShareMatchingService.PredictMatchScoreAsync(mlRequest);

        if (mlResult == null)
        {
            Console.WriteLine($"ML eşleştirme skoru alınamadı. Kural tabanlı skor kullanıldı: {roundedRuleBasedScore}");
            return roundedRuleBasedScore;
        }

        var mlScore = (int)Math.Round(Math.Clamp(mlResult.MatchScore, 0, 100));

        // Mevcut kural tabanlı skor tamamen yok sayılmaz.
        // ML skoru daha baskın olacak şekilde hibrit skor hesaplanır.
        var finalScore = (int)Math.Round((roundedRuleBasedScore * 0.40) + (mlScore * 0.60));
        finalScore = Math.Clamp(finalScore, 0, 100);

        Console.WriteLine($"ML eşleştirme skoru kullanıldı. Kural: {roundedRuleBasedScore} - ML: {mlScore} - Final: {finalScore}");

        return finalScore;
    }

    private async Task<MlShareMatchPredictionRequestDto> CreateMlShareMatchRequestAsync(
        dynamic requester,
        string requesterUserId,
        ShareListing listing,
        LocationMatchInfo locationInfo,
        RequestHistorySummary requestHistory)
    {
        var completedDeliveryCount = await _context.Deliveries
            .AsNoTracking()
            .CountAsync(x =>
                x.ReceiverUserId == requesterUserId &&
                x.DeliveredAt.HasValue);

        var donorPastShareCount = await _context.Deliveries
            .AsNoTracking()
            .CountAsync(x =>
                x.DonorUserId == listing.DonorUserId &&
                x.DeliveredAt.HasValue);

        var requesterPastReceiveCount = completedDeliveryCount;

        var productRiskLevel = GetProductRiskLevel(listing.StockItem);
        var daysUntilExpiration = (listing.StockItem.ExpirationDate.Date - DateTime.Now.Date).Days;

        return new MlShareMatchPredictionRequestDto
        {
            SameCity = locationInfo.SameCity,
            SameDistrict = locationInfo.SameDistrict,
            SameNeighborhood = locationInfo.SameNeighborhood,
            DistancePriority = locationInfo.LocationPriority,

            NeedScore = Math.Clamp(requester.NeedScore, 0, 100),
            ReliabilityScore = Math.Clamp(requester.ReliabilityScore, 0, 100),

            CompletedDeliveryCount = completedDeliveryCount,
            CancelledRequestCount = requestHistory.CancelledRequestCount,
            PendingRequestCount = requestHistory.PendingRequestCount,
            PreviousSuccessfulRequests = requestHistory.ApprovedRequestCount,

            ProductRiskLevel = productRiskLevel,
            DaysUntilExpiration = daysUntilExpiration,
            IsSensitiveFood = listing.StockItem.Product.IsSensitiveFood,

            DonorPastShareCount = donorPastShareCount,
            RequesterPastReceiveCount = requesterPastReceiveCount,
            RequestHour = DateTime.Now.Hour,

            ProductCategory = listing.StockItem.Product.Category?.Name ?? "Diğer"
        };
    }

    private static LocationMatchInfo CalculateLocationInfo(
        string? userCity,
        string? userDistrict,
        string? userNeighborhood,
        string? pointCity,
        string? pointDistrict,
        string? pointNeighborhood)
    {
        var normalizedUserCity = NormalizeText(userCity);
        var normalizedUserDistrict = NormalizeText(userDistrict);
        var normalizedUserNeighborhood = NormalizeText(userNeighborhood);

        var normalizedPointCity = NormalizeText(pointCity);
        var normalizedPointDistrict = NormalizeText(pointDistrict);
        var normalizedPointNeighborhood = NormalizeText(pointNeighborhood);

        var sameCity =
            !string.IsNullOrWhiteSpace(normalizedUserCity) &&
            normalizedUserCity == normalizedPointCity;

        var sameDistrict =
            sameCity &&
            !string.IsNullOrWhiteSpace(normalizedUserDistrict) &&
            normalizedUserDistrict == normalizedPointDistrict;

        var sameNeighborhood =
            sameDistrict &&
            !string.IsNullOrWhiteSpace(normalizedUserNeighborhood) &&
            normalizedUserNeighborhood == normalizedPointNeighborhood;

        var locationPriority = 99;

        if (sameNeighborhood)
            locationPriority = 1;
        else if (sameDistrict)
            locationPriority = 2;
        else if (sameCity)
            locationPriority = 3;

        return new LocationMatchInfo
        {
            SameCity = sameCity,
            SameDistrict = sameDistrict,
            SameNeighborhood = sameNeighborhood,
            LocationPriority = locationPriority
        };
    }

    private static decimal CalculateLocationScore(int locationPriority)
    {
        return locationPriority switch
        {
            1 => 35,
            2 => 25,
            3 => 15,
            _ => 5
        };
    }

    private static decimal CalculateNeedScore(int needScore)
    {
        var normalizedNeedScore = Math.Clamp(needScore, 0, 100);

        return normalizedNeedScore / 100m * 25;
    }

    private static decimal CalculateReliabilityScore(int reliabilityScore)
    {
        var normalizedReliabilityScore = Math.Clamp(reliabilityScore, 0, 100);

        return normalizedReliabilityScore / 100m * 15;
    }

    private async Task<RequestHistorySummary> GetRequestHistorySummaryAsync(string requesterUserId)
    {
        var approvedCount = await _context.ShareRequests
            .AsNoTracking()
            .CountAsync(x =>
                x.RequesterUserId == requesterUserId &&
                x.Status == ShareRequestStatus.Approved);

        var cancelledCount = await _context.ShareRequests
            .AsNoTracking()
            .CountAsync(x =>
                x.RequesterUserId == requesterUserId &&
                (x.Status == ShareRequestStatus.Rejected ||
                 x.Status == ShareRequestStatus.Cancelled));

        var pendingCount = await _context.ShareRequests
            .AsNoTracking()
            .CountAsync(x =>
                x.RequesterUserId == requesterUserId &&
                x.Status == ShareRequestStatus.Pending);

        return new RequestHistorySummary
        {
            ApprovedRequestCount = approvedCount,
            CancelledRequestCount = cancelledCount,
            PendingRequestCount = pendingCount
        };
    }

    private static decimal CalculateRequestHistoryScore(RequestHistorySummary history)
    {
        decimal score = 5;

        score += Math.Min(history.ApprovedRequestCount * 2m, 8);
        score -= Math.Min(history.CancelledRequestCount * 1.5m, 5);
        score -= Math.Min(history.PendingRequestCount * 1m, 3);

        return Math.Clamp(score, 0, 15);
    }

    private static string GetProductRiskLevel(StockItem stockItem)
    {
        if (stockItem.WasteRiskPredictions == null ||
            !stockItem.WasteRiskPredictions.Any())
        {
            return "Low";
        }

        var latestRisk = stockItem.WasteRiskPredictions
            .OrderByDescending(x => x.CalculatedAt)
            .FirstOrDefault();

        return latestRisk?.RiskLevel.ToString() ?? "Low";
    }

    private static string NormalizeText(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        return input
            .Trim()
            .ToLower()
            .Replace("ç", "c")
            .Replace("ğ", "g")
            .Replace("ı", "i")
            .Replace("ö", "o")
            .Replace("ş", "s")
            .Replace("ü", "u");
    }

    private class LocationMatchInfo
    {
        public bool SameCity { get; set; }

        public bool SameDistrict { get; set; }

        public bool SameNeighborhood { get; set; }

        public int LocationPriority { get; set; }
    }

    private class RequestHistorySummary
    {
        public int ApprovedRequestCount { get; set; }

        public int CancelledRequestCount { get; set; }

        public int PendingRequestCount { get; set; }
    }
}
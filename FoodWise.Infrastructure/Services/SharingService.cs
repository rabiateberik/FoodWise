using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// SharingService, ürün paylaşım ilanlarını ve paylaşım taleplerini yönetir.
// Bu servis sayesinde kullanıcı stokundaki ürünü güvenli teslim noktası üzerinden paylaşıma açabilir.
using FoodWise.Application.DTOs.Sharing;
using FoodWise.Application.Interfaces;
using FoodWise.Domain.Entities;
using FoodWise.Domain.Enums;
using FoodWise.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FoodWise.Infrastructure.Services;

public class SharingService : ISharingService
{
    private readonly FoodWiseDbContext _context;

    public SharingService(FoodWiseDbContext context)
    {
        _context = context;
    }

    public async Task<ShareListingDto?> CreateListingAsync(string userId, CreateShareListingDto model)
    {
        // Paylaşıma açılacak stok ürününün giriş yapan kullanıcıya ait olması gerekir.
        var stockItem = await _context.StockItems
            .Include(x => x.Product)
            .Include(x => x.Unit)
            .FirstOrDefaultAsync(x =>
                x.Id == model.StockItemId &&
                x.UserId == userId &&
                x.Status == StockItemStatus.Active);

        if (stockItem == null)
            return null;

        // Kullanıcı stoktaki miktardan fazla ürün paylaşamaz.
        if (model.Quantity <= 0 || model.Quantity > stockItem.Quantity)
            return null;

        // Teslim noktası aktif ve sistemde kayıtlı olmalıdır.
        var deliveryPointExists = await _context.DeliveryPoints
            .AnyAsync(x => x.Id == model.DeliveryPointId && x.IsActive);

        if (!deliveryPointExists)
            return null;

        // Teslim bitiş zamanı başlangıçtan önce olamaz.
        if (model.PickupEndTime <= model.PickupStartTime)
            return null;

        var shareListing = new ShareListing
        {
            StockItemId = model.StockItemId,
            DonorUserId = userId,
            DeliveryPointId = model.DeliveryPointId,
            Title = model.Title,
            Description = model.Description,
            Quantity = model.Quantity,
            PickupStartTime = model.PickupStartTime,
            PickupEndTime = model.PickupEndTime,
            Status = ShareListingStatus.Available,
            CreatedAt = DateTime.Now
        };

        await _context.ShareListings.AddAsync(shareListing);
        await _context.SaveChangesAsync();

        var createdListing = await GetListingEntityByIdAsync(shareListing.Id);

        return createdListing == null ? null : MapToListingDto(createdListing);
    }

    public async Task<List<ShareListingDto>> GetAvailableListingsAsync(string userId)
    {
        // Kullanıcı kendi açtığı ilanları alıcı listesinde görmesin diye DonorUserId filtrelenir.
        var listings = await _context.ShareListings
            .Include(x => x.StockItem)
                .ThenInclude(x => x.Product)
            .Include(x => x.StockItem)
                .ThenInclude(x => x.Unit)
            .Include(x => x.DeliveryPoint)
            .Include(x => x.ShareRequests)
            .Where(x =>
                x.IsActive &&
                x.Status == ShareListingStatus.Available &&
                x.DonorUserId != userId &&
                x.PickupEndTime > DateTime.Now)
            .OrderBy(x => x.PickupEndTime)
            .ToListAsync();

        return listings.Select(MapToListingDto).ToList();
    }

    public async Task<List<ShareListingDto>> GetMyListingsAsync(string userId)
    {
        // Giriş yapan kullanıcının oluşturduğu tüm aktif paylaşım ilanları listelenir.
        var listings = await _context.ShareListings
            .Include(x => x.StockItem)
                .ThenInclude(x => x.Product)
            .Include(x => x.StockItem)
                .ThenInclude(x => x.Unit)
            .Include(x => x.DeliveryPoint)
            .Include(x => x.ShareRequests)
            .Where(x => x.IsActive && x.DonorUserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        return listings.Select(MapToListingDto).ToList();
    }

    public async Task<ShareListingDto?> GetListingByIdAsync(int listingId)
    {
        var listing = await GetListingEntityByIdAsync(listingId);

        return listing == null ? null : MapToListingDto(listing);
    }

    public async Task<ShareRequestDto?> CreateRequestAsync(string requesterUserId, int shareListingId)
    {
        // Talep oluşturulacak ilan aktif ve alınabilir durumda olmalıdır.
        var listing = await _context.ShareListings
            .Include(x => x.StockItem)
                .ThenInclude(x => x.Product)
            .FirstOrDefaultAsync(x =>
                x.Id == shareListingId &&
                x.IsActive &&
                x.Status == ShareListingStatus.Available);

        if (listing == null)
            return null;

        // Kullanıcı kendi ilanına talep gönderemez.
        if (listing.DonorUserId == requesterUserId)
            return null;

        // Aynı kullanıcı aynı ilana ikinci kez talep göndermesin.
        var alreadyRequested = await _context.ShareRequests.AnyAsync(x =>
            x.ShareListingId == shareListingId &&
            x.RequesterUserId == requesterUserId &&
            x.Status != ShareRequestStatus.Cancelled);

        if (alreadyRequested)
            return null;

        var request = new ShareRequest
        {
            ShareListingId = shareListingId,
            RequesterUserId = requesterUserId,
            MatchScore = 80,
            Status = ShareRequestStatus.Pending,
            RequestedAt = DateTime.Now,
            CreatedAt = DateTime.Now
        };

        // İlk talep oluştuğunda ilan durumu Requested yapılır.
        listing.Status = ShareListingStatus.Requested;
        listing.UpdatedAt = DateTime.Now;

        await _context.ShareRequests.AddAsync(request);
        await _context.SaveChangesAsync();

        var createdRequest = await GetRequestEntityByIdAsync(request.Id);

        return createdRequest == null ? null : MapToRequestDto(createdRequest);
    }

    public async Task<List<ShareRequestDto>> GetRequestsForMyListingAsync(string userId, int shareListingId)
    {
        // Sadece ilan sahibi kendi ilanına gelen talepleri görebilir.
        var listingBelongsToUser = await _context.ShareListings
            .AnyAsync(x => x.Id == shareListingId && x.DonorUserId == userId);

        if (!listingBelongsToUser)
            return new List<ShareRequestDto>();

        var requests = await _context.ShareRequests
            .Include(x => x.ShareListing)
                .ThenInclude(x => x.StockItem)
                    .ThenInclude(x => x.Product)
            .Where(x => x.ShareListingId == shareListingId)
            .OrderByDescending(x => x.RequestedAt)
            .ToListAsync();

        return requests.Select(MapToRequestDto).ToList();
    }

    public async Task<ShareRequestDto?> ApproveRequestAsync(string donorUserId, int requestId)
    {
        // Onaylanacak talep ve bağlı olduğu ilan birlikte alınır.
        var request = await _context.ShareRequests
            .Include(x => x.ShareListing)
                .ThenInclude(x => x.StockItem)
                    .ThenInclude(x => x.Product)
            .FirstOrDefaultAsync(x => x.Id == requestId);

        if (request == null)
            return null;

        // Sadece ilan sahibi talebi onaylayabilir.
        if (request.ShareListing.DonorUserId != donorUserId)
            return null;

        request.Status = ShareRequestStatus.Approved;
        request.RespondedAt = DateTime.Now;
        request.UpdatedAt = DateTime.Now;

        request.ShareListing.Status = ShareListingStatus.Approved;
        request.ShareListing.UpdatedAt = DateTime.Now;

        // Aynı ilana gelen diğer bekleyen talepler reddedilir.
        var otherRequests = await _context.ShareRequests
            .Where(x =>
                x.ShareListingId == request.ShareListingId &&
                x.Id != request.Id &&
                x.Status == ShareRequestStatus.Pending)
            .ToListAsync();

        foreach (var otherRequest in otherRequests)
        {
            otherRequest.Status = ShareRequestStatus.Rejected;
            otherRequest.RespondedAt = DateTime.Now;
            otherRequest.UpdatedAt = DateTime.Now;
        }

        await _context.SaveChangesAsync();

        var approvedRequest = await GetRequestEntityByIdAsync(request.Id);

        return approvedRequest == null ? null : MapToRequestDto(approvedRequest);
    }

    public async Task<ShareRequestDto?> RejectRequestAsync(string donorUserId, int requestId)
    {
        var request = await _context.ShareRequests
            .Include(x => x.ShareListing)
                .ThenInclude(x => x.StockItem)
                    .ThenInclude(x => x.Product)
            .FirstOrDefaultAsync(x => x.Id == requestId);

        if (request == null)
            return null;

        // Sadece ilan sahibi talebi reddedebilir.
        if (request.ShareListing.DonorUserId != donorUserId)
            return null;

        request.Status = ShareRequestStatus.Rejected;
        request.RespondedAt = DateTime.Now;
        request.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        var rejectedRequest = await GetRequestEntityByIdAsync(request.Id);

        return rejectedRequest == null ? null : MapToRequestDto(rejectedRequest);
    }

    public async Task<bool> CancelListingAsync(string userId, int listingId)
    {
        // Sadece ilan sahibi kendi paylaşım ilanını iptal edebilir.
        var listing = await _context.ShareListings
            .FirstOrDefaultAsync(x => x.Id == listingId && x.DonorUserId == userId);

        if (listing == null)
            return false;

        listing.Status = ShareListingStatus.Cancelled;
        listing.IsActive = false;
        listing.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        return true;
    }

    private async Task<ShareListing?> GetListingEntityByIdAsync(int listingId)
    {
        return await _context.ShareListings
            .Include(x => x.StockItem)
                .ThenInclude(x => x.Product)
            .Include(x => x.StockItem)
                .ThenInclude(x => x.Unit)
            .Include(x => x.DeliveryPoint)
            .Include(x => x.ShareRequests)
            .FirstOrDefaultAsync(x => x.Id == listingId);
    }

    private async Task<ShareRequest?> GetRequestEntityByIdAsync(int requestId)
    {
        return await _context.ShareRequests
            .Include(x => x.ShareListing)
                .ThenInclude(x => x.StockItem)
                    .ThenInclude(x => x.Product)
            .FirstOrDefaultAsync(x => x.Id == requestId);
    }

    private ShareListingDto MapToListingDto(ShareListing listing)
    {
        return new ShareListingDto
        {
            Id = listing.Id,
            StockItemId = listing.StockItemId,
            ProductName = listing.StockItem.Product.Name,
            Quantity = listing.Quantity,
            UnitName = listing.StockItem.Unit.ShortName,
            DonorUserId = listing.DonorUserId,
            DeliveryPointId = listing.DeliveryPointId,
            DeliveryPointName = listing.DeliveryPoint.Name,
            Title = listing.Title,
            Description = listing.Description,
            PickupStartTime = listing.PickupStartTime,
            PickupEndTime = listing.PickupEndTime,
            Status = listing.Status.ToString(),
            RequestCount = listing.ShareRequests.Count
        };
    }

    private ShareRequestDto MapToRequestDto(ShareRequest request)
    {
        return new ShareRequestDto
        {
            Id = request.Id,
            ShareListingId = request.ShareListingId,
            ShareListingTitle = request.ShareListing.Title,
            ProductName = request.ShareListing.StockItem.Product.Name,
            RequesterUserId = request.RequesterUserId,
            MatchScore = request.MatchScore,
            Status = request.Status.ToString(),
            RequestedAt = request.RequestedAt,
            RespondedAt = request.RespondedAt
        };
    }
}
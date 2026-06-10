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
using FoodWise.Application.DTOs.Notification;
namespace FoodWise.Infrastructure.Services;

public class SharingService : ISharingService
{
    private readonly FoodWiseDbContext _context;
    private readonly INotificationService _notificationService;
    private readonly IShareRequestMatchingService _shareRequestMatchingService;
    public SharingService(
        FoodWiseDbContext context,
        INotificationService notificationService,
        IShareRequestMatchingService shareRequestMatchingService)
    {
        _context = context;
        _notificationService = notificationService;
        _shareRequestMatchingService = shareRequestMatchingService;
    }

    public async Task<ShareListingDto?> CreateListingAsync(string userId, CreateShareListingDto model)
    {
        var now = DateTime.Now;

        // Paylaşıma açılacak stok ürününün giriş yapan kullanıcıya ait ve aktif olması gerekir.
        // Ürünün riskli olup olmamasına bakılmaz; kullanıcı isterse riskli olmayan ürünü de paylaşabilir.
        var stockItem = await _context.StockItems
            .Include(x => x.Product)
            .Include(x => x.Unit)
            .FirstOrDefaultAsync(x =>
                x.Id == model.StockItemId &&
                x.UserId == userId &&
                x.Status == StockItemStatus.Active &&
                x.IsActive);

        if (stockItem == null)
            return null;

        // Aynı stok ürünü aktif bir paylaşım ilanında kullanılıyorsa tekrar paylaşım ilanı oluşturulmaz.
        // İptal edilmiş, süresi dolmuş veya teslim edilmiş ilanlardan sonra ürün tekrar paylaşıma açılabilir.
        var hasActiveListing = await _context.ShareListings
            .AnyAsync(x =>
                x.StockItemId == model.StockItemId &&
                x.DonorUserId == userId &&
                x.IsActive &&
                x.Status != ShareListingStatus.Cancelled &&
                x.Status != ShareListingStatus.Expired &&
                x.Status != ShareListingStatus.Delivered);

        if (hasActiveListing)
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

        // Teslim bitiş zamanı geçmiş bir tarih olamaz.
        if (model.PickupEndTime <= now)
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
            CreatedAt = now,
            IsActive = true
        };

        await _context.ShareListings.AddAsync(shareListing);
        await _context.SaveChangesAsync();

        var createdListing = await GetListingEntityByIdAsync(shareListing.Id);

        return createdListing == null ? null : MapToListingDto(createdListing);
    }

    public async Task<List<ShareListingDto>> GetAvailableListingsAsync(string userId)
    {
        var now = DateTime.Now;

        // Giriş yapan kullanıcının kayıtlı konumu alınır.
        // İlan teslim noktaları bu konuma göre aynı mahalle / aynı ilçe / aynı şehir şeklinde etiketlenir.
        var currentUser = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == userId && x.IsActive);

        var listings = await _context.ShareListings
            .Include(x => x.StockItem)
                .ThenInclude(x => x.Product)
            .Include(x => x.StockItem)
                .ThenInclude(x => x.Unit)
            .Include(x => x.DeliveryPoint)
            .Include(x => x.ShareRequests)
            .Where(x =>
                x.IsActive &&
                x.DonorUserId != userId &&
                x.PickupEndTime > now &&

                x.Status != ShareListingStatus.QrGenerated &&
                x.Status != ShareListingStatus.Delivered &&
                x.Status != ShareListingStatus.Cancelled &&
                x.Status != ShareListingStatus.Expired &&

                (
                    x.Status == ShareListingStatus.Available ||
                    x.Status == ShareListingStatus.Requested ||

                    (
                        x.Status == ShareListingStatus.Approved &&
                        x.ShareRequests.Any(r =>
                            r.RequesterUserId == userId &&
                            r.Status == ShareRequestStatus.Approved)
                    )
                ))
            .OrderBy(x => x.PickupEndTime)
            .ToListAsync();

        var result = new List<ShareListingDto>();

        foreach (var listing in listings)
        {
            var dto = MapToListingDto(
                listing,
                currentUser?.City,
                currentUser?.District,
                currentUser?.Neighborhood
            );

            var currentUserRequest = listing.ShareRequests
                .OrderByDescending(x => x.RequestedAt)
                .FirstOrDefault(x =>
                    x.RequesterUserId == userId &&
                    (x.Status == ShareRequestStatus.Pending ||
                     x.Status == ShareRequestStatus.Approved));

            dto.HasCurrentUserRequest = currentUserRequest != null;
            dto.CurrentUserRequestId = currentUserRequest?.Id;
            dto.CurrentUserRequestStatus = currentUserRequest?.Status.ToString();

            result.Add(dto);
        }

        return result;
    }
    public async Task<List<ShareListingDto>> GetMyListingsAsync(string userId)
    {
        // Giriş yapan kullanıcının kayıtlı konumu alınır.
        // Kendi ilanlarında da seçilen teslim noktasının kullanıcıya yakınlığı gösterilir.
        var currentUser = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == userId && x.IsActive);

        var listings = await _context.ShareListings
            .Include(x => x.StockItem)
                .ThenInclude(x => x.Product)
            .Include(x => x.StockItem)
                .ThenInclude(x => x.Unit)
            .Include(x => x.DeliveryPoint)
            .Include(x => x.ShareRequests)
            .Where(x =>
                x.DonorUserId == userId &&
                x.Status != ShareListingStatus.Cancelled &&
                x.Status != ShareListingStatus.Expired &&
                x.Status != ShareListingStatus.Delivered &&
                x.Status != ShareListingStatus.QrGenerated &&
                !(x.Status == ShareListingStatus.Available && x.PickupEndTime <= DateTime.Now))
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        return listings
            .Select(x => MapToListingDto(
                x,
                currentUser?.City,
                currentUser?.District,
                currentUser?.Neighborhood
            ))
            .ToList();
    }
    public async Task<ShareListingDto?> GetListingByIdAsync(int listingId)
    {
        var listing = await GetListingEntityByIdAsync(listingId);

        return listing == null ? null : MapToListingDto(listing);
    }

    public async Task<ShareRequestDto?> CreateRequestAsync(string requesterUserId, int shareListingId)
    {
        // Talep oluşturulacak ilan aktif, alınabilir ve süresi geçmemiş durumda olmalıdır.
        var listing = await _context.ShareListings
     .Include(x => x.StockItem)
         .ThenInclude(x => x.Product)
     .Include(x => x.DeliveryPoint)
     .FirstOrDefaultAsync(x =>
                 x.Id == shareListingId &&
                x.IsActive &&
                x.Status == ShareListingStatus.Available &&
                x.PickupEndTime > DateTime.Now);

        if (listing == null)
            return null;

        // Kullanıcı kendi ilanına talep gönderemez.
        if (listing.DonorUserId == requesterUserId)
            return null;

        // Kullanıcı aynı ilana bekleyen veya onaylanmış aktif talebi varken tekrar talep gönderemez.
        // İptal edilen veya reddedilen talepten sonra yeniden talep gönderebilir.
        var alreadyRequested = await _context.ShareRequests
            .AnyAsync(x =>
                x.ShareListingId == shareListingId &&
                x.RequesterUserId == requesterUserId &&
                (x.Status == ShareRequestStatus.Pending ||
                 x.Status == ShareRequestStatus.Approved));

        if (alreadyRequested)
            return null;
        var matchScore = await _shareRequestMatchingService.CalculateMatchScoreAsync(
    requesterUserId,
    listing
);

        var request = new ShareRequest
        {
            ShareListingId = shareListingId,
            RequesterUserId = requesterUserId,
            MatchScore = matchScore,
            Status = ShareRequestStatus.Pending,
            RequestedAt = DateTime.Now,
            CreatedAt = DateTime.Now
        };

        await _context.ShareRequests.AddAsync(request);
        await _context.SaveChangesAsync();
        await _notificationService.CreateAsync(listing.DonorUserId, new CreateNotificationDto
        {
            Title = "Yeni paylaşım talebi",
            Message = $"{listing.StockItem.Product.Name} paylaşım ilanın için yeni bir talep geldi.",
            Type = NotificationType.ShareRequest,
            TargetUrl = $"/Sharing/MyListings?highlightListingId={listing.Id}"
        });

        var createdRequest = await GetRequestEntityByIdAsync(request.Id);

        return createdRequest == null ? null : MapToRequestDto(createdRequest);
    }

    public async Task<List<ShareRequestDto>> GetRequestsForMyListingAsync(string userId, int shareListingId)
    {
        // İlanın giriş yapan kullanıcıya ait olup olmadığı kontrol edilir.
        // Kullanıcı sadece kendi paylaşım ilanına gelen talepleri görebilir.
        var listingExists = await _context.ShareListings
            .AnyAsync(x =>
                x.Id == shareListingId &&
                x.DonorUserId == userId &&
                x.IsActive);

        if (!listingExists)
            return new List<ShareRequestDto>();

        // İlan talepleri sayfasında sadece aktif talepler gösterilir.
        // İptal edilen veya reddedilen talepler geçmiş kayıt olarak veritabanında kalır,
        // fakat kullanıcı arayüzünde kalabalık oluşturmaması için listelenmez.
        var requests = await _context.ShareRequests
            .Include(x => x.ShareListing)
                .ThenInclude(x => x.StockItem)
                    .ThenInclude(x => x.Product)
            .Where(x =>
                x.ShareListingId == shareListingId &&
                (x.Status == ShareRequestStatus.Pending ||
                 x.Status == ShareRequestStatus.Approved))
            .OrderByDescending(x => x.RequestedAt)
            .ToListAsync();

        if (!requests.Any())
            return new List<ShareRequestDto>();

        // Bu taleplerden hangileri için teslimat oluşturulduğu kontrol edilir.
        var requestIds = requests.Select(x => x.Id).ToList();

        var deliveries = await _context.Deliveries
            .Where(x => requestIds.Contains(x.ShareRequestId))
            .Select(x => new
            {
                x.Id,
                x.ShareRequestId
            })
            .ToListAsync();

        var requesterIds = requests
    .Select(x => x.RequesterUserId)
    .Distinct()
    .ToList();

        var requesterNames = await _context.Users
            .AsNoTracking()
            .Where(x => requesterIds.Contains(x.Id))
            .Select(x => new
            {
                x.Id,
                x.FullName
            })
            .ToDictionaryAsync(x => x.Id, x => x.FullName);

        var result = new List<ShareRequestDto>();

        foreach (var request in requests)
        {
            var dto = MapToRequestDto(request);

            var delivery = deliveries.FirstOrDefault(x => x.ShareRequestId == request.Id);

            dto.HasDelivery = delivery != null;
            dto.DeliveryId = delivery?.Id;

            dto.RequesterFullName = requesterNames.TryGetValue(request.RequesterUserId, out var fullName)
                ? fullName
                : "Kullanıcı";

            result.Add(dto);
        }

        return result;
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

        var rejectedRequesterIds = new List<string>();

        foreach (var otherRequest in otherRequests)
        {
            otherRequest.Status = ShareRequestStatus.Rejected;
            otherRequest.RespondedAt = DateTime.Now;
            otherRequest.UpdatedAt = DateTime.Now;

            rejectedRequesterIds.Add(otherRequest.RequesterUserId);
        }

        await _context.SaveChangesAsync();

        await _notificationService.CreateAsync(request.RequesterUserId, new CreateNotificationDto
        {
            Title = "Talebin onaylandı",
            Message = $"{request.ShareListing.StockItem.Product.Name} için gönderdiğin talep onaylandı. Teslimat oluşturulması bekleniyor.",
            Type = NotificationType.RequestApproved,
            TargetUrl = "/Sharing/Available"
        });

        // Aynı ilana gelen diğer bekleyen talepler otomatik reddedildiği için ilgili kullanıcılara bilgi verilir.
        foreach (var rejectedRequesterId in rejectedRequesterIds)
        {
            await _notificationService.CreateAsync(rejectedRequesterId, new CreateNotificationDto
            {
                Title = "Talebin reddedildi",
                Message = $"{request.ShareListing.StockItem.Product.Name} için gönderdiğin talep, başka bir talep onaylandığı için reddedildi.",
                Type = NotificationType.RequestRejected,
                TargetUrl = "/Sharing/Available"
            });
        }
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
        await _notificationService.CreateAsync(request.RequesterUserId, new CreateNotificationDto
        {
            Title = "Talebin reddedildi",
            Message = $"{request.ShareListing.StockItem.Product.Name} için gönderdiğin talep reddedildi.",
            Type = NotificationType.RequestRejected,
            TargetUrl = "/Sharing/Available"
        });
        var rejectedRequest = await GetRequestEntityByIdAsync(request.Id);

        return rejectedRequest == null ? null : MapToRequestDto(rejectedRequest);
    }

    public async Task<bool> CancelListingAsync(string userId, int listingId)
    {
        // Sadece ilan sahibi kendi paylaşım ilanını iptal edebilir.
        var listing = await _context.ShareListings
     .Include(x => x.StockItem)
         .ThenInclude(x => x.Product)
     .Include(x => x.ShareRequests)
     .FirstOrDefaultAsync(x =>
         x.Id == listingId &&
         x.DonorUserId == userId &&
         x.IsActive);

        if (listing == null)
            return false;

        // Teslimat sürecine geçmiş veya tamamlanmış ilanlar iptal edilemez.
        if (listing.Status == ShareListingStatus.Approved ||
            listing.Status == ShareListingStatus.QrGenerated ||
            listing.Status == ShareListingStatus.Delivered)
        {
            return false;
        }

        listing.Status = ShareListingStatus.Cancelled;
        listing.IsActive = false;
        listing.UpdatedAt = DateTime.Now;

        // İlan iptal edilince bekleyen talepler de iptal edilir.
        var cancelledRequesterIds = new List<string>();

        foreach (var request in listing.ShareRequests.Where(x => x.Status == ShareRequestStatus.Pending))
        {
            request.Status = ShareRequestStatus.Cancelled;
            request.RespondedAt = DateTime.Now;
            request.UpdatedAt = DateTime.Now;

            cancelledRequesterIds.Add(request.RequesterUserId);
        }

        await _context.SaveChangesAsync();

        // İlan iptal edilince bekleyen talep sahiplerine bilgi verilir.
        foreach (var requesterId in cancelledRequesterIds)
        {
            await _notificationService.CreateAsync(requesterId, new CreateNotificationDto
            {
                Title = "Paylaşım ilanı iptal edildi",
                Message = $"{listing.StockItem.Product.Name} için talep gönderdiğin paylaşım ilanı iptal edildi.",
                Type = NotificationType.System,
                TargetUrl = "/Sharing/Available"
            });
        }

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
    public async Task<bool> CancelRequestAsync(string requesterUserId, int requestId)
    {
        // Sadece talebi oluşturan kullanıcı kendi bekleyen talebini iptal edebilir.
        var request = await _context.ShareRequests
            .Include(x => x.ShareListing)
            .FirstOrDefaultAsync(x =>
                x.Id == requestId &&
                x.RequesterUserId == requesterUserId &&
                x.Status == ShareRequestStatus.Pending);

        if (request == null)
            return false;

        request.Status = ShareRequestStatus.Cancelled;
        request.RespondedAt = DateTime.Now;
        request.UpdatedAt = DateTime.Now;

        // Eğer ilanda artık bekleyen veya onaylanan talep kalmadıysa ilan tekrar Available durumuna döner.
        var hasActiveRequest = await _context.ShareRequests
            .AnyAsync(x =>
                x.ShareListingId == request.ShareListingId &&
                x.Id != request.Id &&
                (x.Status == ShareRequestStatus.Pending ||
                 x.Status == ShareRequestStatus.Approved));

        if (!hasActiveRequest && request.ShareListing.Status == ShareListingStatus.Requested)
        {
            request.ShareListing.Status = ShareListingStatus.Available;
            request.ShareListing.UpdatedAt = DateTime.Now;
        }

        await _context.SaveChangesAsync();

        return true;
    }
    private ShareListingDto MapToListingDto(
     ShareListing listing,
     string? userCity = null,
     string? userDistrict = null,
     string? userNeighborhood = null)
    {
        var locationPriority = CalculateLocationPriority(
            userCity,
            userDistrict,
            userNeighborhood,
            listing.DeliveryPoint?.City,
            listing.DeliveryPoint?.District,
            listing.DeliveryPoint?.Neighborhood
        );

        return new ShareListingDto
        {
            Id = listing.Id,
            StockItemId = listing.StockItemId,
            ProductName = listing.StockItem.Product.Name,
            Quantity = listing.Quantity,
            UnitName = listing.StockItem.Unit.ShortName,
            DonorUserId = listing.DonorUserId,
            DeliveryPointId = listing.DeliveryPointId,
            DeliveryPointName = listing.DeliveryPoint?.Name,
            City = listing.DeliveryPoint?.City,
            District = listing.DeliveryPoint?.District,
            Neighborhood = listing.DeliveryPoint?.Neighborhood,
            LocationPriority = locationPriority,
            LocationMatchText = GetLocationMatchText(locationPriority),
            Title = listing.Title,
            Description = listing.Description,
            PickupStartTime = listing.PickupStartTime,
            PickupEndTime = listing.PickupEndTime,
            Status = listing.Status.ToString(),
            RequestCount = listing.ShareRequests.Count(x =>
                x.Status == ShareRequestStatus.Pending ||
                x.Status == ShareRequestStatus.Approved)
        };
    }
    private static int CalculateLocationPriority(
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

        if (sameNeighborhood)
            return 1;

        if (sameDistrict)
            return 2;

        if (sameCity)
            return 3;

        return 99;
    }

    private static string GetLocationMatchText(int locationPriority)
    {
        return locationPriority switch
        {
            1 => "Aynı mahalle",
            2 => "Aynı ilçe",
            3 => "Aynı şehir",
            _ => "Diğer bölge"
        };
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
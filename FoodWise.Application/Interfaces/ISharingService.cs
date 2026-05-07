using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Paylaşım modülünün servis sözleşmesidir.
// Controller, paylaşım işlemlerini bu interface üzerinden servis katmanına iletir.
using FoodWise.Application.DTOs.Sharing;

namespace FoodWise.Application.Interfaces;

public interface ISharingService
{
    Task<ShareListingDto?> CreateListingAsync(string userId, CreateShareListingDto model);

    Task<List<ShareListingDto>> GetAvailableListingsAsync(string userId);

    Task<List<ShareListingDto>> GetMyListingsAsync(string userId);

    Task<ShareListingDto?> GetListingByIdAsync(int listingId);

    Task<ShareRequestDto?> CreateRequestAsync(string requesterUserId, int shareListingId);

    Task<List<ShareRequestDto>> GetRequestsForMyListingAsync(string userId, int shareListingId);

    Task<ShareRequestDto?> ApproveRequestAsync(string donorUserId, int requestId);

    Task<ShareRequestDto?> RejectRequestAsync(string donorUserId, int requestId);

    Task<bool> CancelListingAsync(string userId, int listingId);
}
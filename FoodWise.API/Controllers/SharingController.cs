// SharingController, ürün paylaşım ilanları ve paylaşım taleplerini yöneten endpointleri içerir.
// Bu controller token ile korunur; işlemler giriş yapan kullanıcıya göre yapılır.
using System.Security.Claims;
using FoodWise.Application.DTOs.Sharing;
using FoodWise.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodWise.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class SharingController : ControllerBase
{
    private readonly ISharingService _sharingService;

    public SharingController(ISharingService sharingService)
    {
        _sharingService = sharingService;
    }

    [HttpPost("listings")]
    public async Task<IActionResult> CreateListing(CreateShareListingDto model)
    {
        // Giriş yapan kullanıcının stokundaki ürün paylaşıma açılır.
        var userId = GetUserId();

        var result = await _sharingService.CreateListingAsync(userId, model);

        if (result == null)
            return BadRequest("Paylaşım ilanı oluşturulamadı. Stok ürünü, miktar veya teslim noktası bilgilerini kontrol edin.");

        return Ok(result);
    }

    [HttpGet("listings/available")]
    public async Task<IActionResult> GetAvailableListings()
    {
        // Kullanıcının kendi ilanları hariç aktif paylaşım ilanları listelenir.
        var userId = GetUserId();

        var result = await _sharingService.GetAvailableListingsAsync(userId);

        return Ok(result);
    }

    [HttpGet("listings/my")]
    public async Task<IActionResult> GetMyListings()
    {
        // Giriş yapan kullanıcının oluşturduğu paylaşım ilanları listelenir.
        var userId = GetUserId();

        var result = await _sharingService.GetMyListingsAsync(userId);

        return Ok(result);
    }

    [HttpGet("listings/{listingId}")]
    public async Task<IActionResult> GetListingById(int listingId)
    {
        var result = await _sharingService.GetListingByIdAsync(listingId);

        if (result == null)
            return NotFound("Paylaşım ilanı bulunamadı.");

        return Ok(result);
    }

    [HttpPost("listings/{listingId}/request")]
    public async Task<IActionResult> CreateRequest(int listingId)
    {
        // Giriş yapan kullanıcı başka bir kullanıcının ilanına talep oluşturur.
        var userId = GetUserId();

        var result = await _sharingService.CreateRequestAsync(userId, listingId);

        if (result == null)
            return BadRequest("Talep oluşturulamadı. İlan durumu veya kullanıcı bilgilerini kontrol edin.");

        return Ok(result);
    }

    [HttpGet("listings/{listingId}/requests")]
    public async Task<IActionResult> GetRequestsForMyListing(int listingId)
    {
        // İlan sahibi, kendi ilanına gelen talepleri görüntüler.
        var userId = GetUserId();

        var result = await _sharingService.GetRequestsForMyListingAsync(userId, listingId);

        return Ok(result);
    }

    [HttpPost("requests/{requestId}/approve")]
    public async Task<IActionResult> ApproveRequest(int requestId)
    {
        // İlan sahibi, gelen paylaşım talebini onaylar.
        var userId = GetUserId();

        var result = await _sharingService.ApproveRequestAsync(userId, requestId);

        if (result == null)
            return BadRequest("Talep onaylanamadı. Yetki veya talep durumunu kontrol edin.");

        return Ok(result);
    }

    [HttpPost("requests/{requestId}/reject")]
    public async Task<IActionResult> RejectRequest(int requestId)
    {
        // İlan sahibi, gelen paylaşım talebini reddeder.
        var userId = GetUserId();

        var result = await _sharingService.RejectRequestAsync(userId, requestId);

        if (result == null)
            return BadRequest("Talep reddedilemedi. Yetki veya talep durumunu kontrol edin.");

        return Ok(result);
    }

    [HttpDelete("listings/{listingId}")]
    public async Task<IActionResult> CancelListing(int listingId)
    {
        // İlan sahibi, kendi paylaşım ilanını iptal eder.
        var userId = GetUserId();

        var result = await _sharingService.CancelListingAsync(userId, listingId);

        if(!result)
    return BadRequest("Paylaşım ilanı iptal edilemedi. İlan teslimat sürecine geçmiş olabilir veya bu işlem için yetkiniz olmayabilir.");

        return Ok("Paylaşım ilanı başarıyla iptal edildi.");
    }
    [HttpPost("requests/{requestId}/cancel")]
    public async Task<IActionResult> CancelRequest(int requestId)
    {
        // Talebi oluşturan kullanıcı kendi bekleyen talebini iptal eder.
        var userId = GetUserId();

        var result = await _sharingService.CancelRequestAsync(userId, requestId);

        if (!result)
            return BadRequest("Talep iptal edilemedi. Sadece bekleyen talepler iptal edilebilir.");

        return Ok("Talep başarıyla iptal edildi.");
    }
    private string GetUserId()
    {
        // JWT token içindeki NameIdentifier claim'i Identity kullanıcı Id bilgisini taşır.
        return User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    }
}
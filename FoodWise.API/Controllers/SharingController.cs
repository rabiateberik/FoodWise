// SharingController, ürün paylaşım ilanları ve paylaşım taleplerini yöneten endpointleri içerir.
// İlan oluşturma, talep gönderme, onaylama, reddetme ve iptal işlemleri burada karşılanır.

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

    // Kullanıcının stokundaki bir ürünü paylaşım ilanı olarak oluşturur.
    [HttpPost("listings")]
    public async Task<IActionResult> CreateListing(CreateShareListingDto model)
    {
        var userId = GetUserId();

        var result = await _sharingService.CreateListingAsync(userId, model);

        if (result == null)
            return BadRequest("Paylaşım ilanı oluşturulamadı. Stok ürünü, miktar veya teslim noktası bilgilerini kontrol edin.");

        return Ok(result);
    }

    // Kullanıcının kendi ilanları hariç aktif paylaşım ilanlarını listeler.
    [HttpGet("listings/available")]
    public async Task<IActionResult> GetAvailableListings()
    {
        var userId = GetUserId();

        var result = await _sharingService.GetAvailableListingsAsync(userId);

        return Ok(result);
    }

    // Giriş yapan kullanıcının oluşturduğu paylaşım ilanlarını getirir.
    [HttpGet("listings/my")]
    public async Task<IActionResult> GetMyListings()
    {
        var userId = GetUserId();

        var result = await _sharingService.GetMyListingsAsync(userId);

        return Ok(result);
    }

    // Seçilen paylaşım ilanının detay bilgilerini getirir.
    [HttpGet("listings/{listingId}")]
    public async Task<IActionResult> GetListingById(int listingId)
    {
        var result = await _sharingService.GetListingByIdAsync(listingId);

        if (result == null)
            return NotFound("Paylaşım ilanı bulunamadı.");

        return Ok(result);
    }

    // Kullanıcı başka bir kullanıcının paylaşım ilanına talep gönderir.
    [HttpPost("listings/{listingId}/request")]
    public async Task<IActionResult> CreateRequest(int listingId)
    {
        var userId = GetUserId();

        var result = await _sharingService.CreateRequestAsync(userId, listingId);

        if (result == null)
            return BadRequest("Talep oluşturulamadı. İlan durumu veya kullanıcı bilgilerini kontrol edin.");

        return Ok(result);
    }

    // İlan sahibi, kendi ilanına gelen talepleri görüntüler.
    [HttpGet("listings/{listingId}/requests")]
    public async Task<IActionResult> GetRequestsForMyListing(int listingId)
    {
        var userId = GetUserId();

        var result = await _sharingService.GetRequestsForMyListingAsync(userId, listingId);

        return Ok(result);
    }

    // İlan sahibi gelen paylaşım talebini onaylar.
    // Talep onaylandığında teslimat süreci için uygun hale gelir.
    [HttpPost("requests/{requestId}/approve")]
    public async Task<IActionResult> ApproveRequest(int requestId)
    {
        var userId = GetUserId();

        var result = await _sharingService.ApproveRequestAsync(userId, requestId);

        if (result == null)
            return BadRequest("Talep onaylanamadı. Yetki veya talep durumunu kontrol edin.");

        return Ok(result);
    }

    // İlan sahibi gelen paylaşım talebini reddeder.
    [HttpPost("requests/{requestId}/reject")]
    public async Task<IActionResult> RejectRequest(int requestId)
    {
        var userId = GetUserId();

        var result = await _sharingService.RejectRequestAsync(userId, requestId);

        if (result == null)
            return BadRequest("Talep reddedilemedi. Yetki veya talep durumunu kontrol edin.");

        return Ok(result);
    }

    // İlan sahibi kendi paylaşım ilanını iptal eder.
    // Teslimat sürecine geçmiş ilanların iptali servis tarafında engellenir.
    [HttpDelete("listings/{listingId}")]
    public async Task<IActionResult> CancelListing(int listingId)
    {
        var userId = GetUserId();

        var result = await _sharingService.CancelListingAsync(userId, listingId);

        if (!result)
            return BadRequest("Paylaşım ilanı iptal edilemedi. İlan teslimat sürecine geçmiş olabilir veya bu işlem için yetkiniz olmayabilir.");

        return Ok("Paylaşım ilanı başarıyla iptal edildi.");
    }

    // Talebi oluşturan kullanıcı, kendi bekleyen talebini iptal eder.
    [HttpPost("requests/{requestId}/cancel")]
    public async Task<IActionResult> CancelRequest(int requestId)
    {
        var userId = GetUserId();

        var result = await _sharingService.CancelRequestAsync(userId, requestId);

        if (!result)
            return BadRequest("Talep iptal edilemedi. Sadece bekleyen talepler iptal edilebilir.");

        return Ok("Talep başarıyla iptal edildi.");
    }

    // JWT token içindeki kullanıcı Id bilgisini alır.
    private string GetUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    }
}


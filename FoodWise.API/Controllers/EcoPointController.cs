// EcoPointController, giriş yapan kullanıcının eco puan bilgilerini döner.
// Puan hesaplama ve geçmiş kayıtları servis katmanında yönetilir.

using System.Security.Claims;
using FoodWise.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodWise.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EcoPointController : ControllerBase
{
    private readonly IEcoPointService _ecoPointService;

    public EcoPointController(IEcoPointService ecoPointService)
    {
        _ecoPointService = ecoPointService;
    }

    // Kullanıcının toplam eco puanını ve genel puan özetini getirir.
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary()
    {
        var userId = GetUserId();

        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized("Kullanıcı bilgisi alınamadı.");

        var summary = await _ecoPointService.GetSummaryAsync(userId);

        return Ok(summary);
    }

    // Kullanıcının eco puan kazanma geçmişini listeler.
    [HttpGet("history")]
    public async Task<IActionResult> GetHistory()
    {
        var userId = GetUserId();

        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized("Kullanıcı bilgisi alınamadı.");

        var history = await _ecoPointService.GetHistoryAsync(userId);

        return Ok(history);
    }

    // JWT token içinden giriş yapan kullanıcının Id bilgisini alır.
    // Token yapısına göre NameIdentifier veya sub claim'i kullanılabilir.
    private string? GetUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");
    }
}


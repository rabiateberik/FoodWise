// EcoPointController, giriş yapan kullanıcının eco puan özetini ve puan geçmişini API üzerinden döner.
// Eco puan kazanma işlemleri servis katmanında yönetilir; controller sadece kullanıcıya ait verileri sunar.

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

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary()
    {
        var userId = GetUserId();

        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized("Kullanıcı bilgisi alınamadı.");

        var summary = await _ecoPointService.GetSummaryAsync(userId);

        return Ok(summary);
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetHistory()
    {
        var userId = GetUserId();

        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized("Kullanıcı bilgisi alınamadı.");

        var history = await _ecoPointService.GetHistoryAsync(userId);

        return Ok(history);
    }

    // JWT içinden giriş yapan kullanıcının Id bilgisini alır.
    // Projede token claim yapısına göre NameIdentifier veya sub alanı kullanılabilir.
    private string? GetUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");
    }
}
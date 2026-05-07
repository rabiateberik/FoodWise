
using System.Security.Claims;
using FoodWise.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodWise.API.Controllers;

[Route("api/carbon-report")]
[ApiController]
[Authorize]
public class CarbonReportController : ControllerBase
{
    private readonly ICarbonReportService _carbonReportService;

    public CarbonReportController(ICarbonReportService carbonReportService)
    {
        _carbonReportService = carbonReportService;
    }

    [HttpPost("generate")]
    public async Task<IActionResult> GenerateMonthlyReport(int month, int year)
    {
        if (!IsValidMonthAndYear(month, year))
            return BadRequest("Geçerli bir ay ve yıl bilgisi giriniz.");

        var userId = GetUserId();

        var result = await _carbonReportService.GenerateMonthlyReportAsync(userId, month, year);

        return Ok(result);
    }

    [HttpGet("monthly")]
    public async Task<IActionResult> GetMonthlyReport(int month, int year)
    {
        if (!IsValidMonthAndYear(month, year))
            return BadRequest("Geçerli bir ay ve yıl bilgisi giriniz.");

        var userId = GetUserId();

        var result = await _carbonReportService.GetMonthlyReportAsync(userId, month, year);

        if (result == null)
            return NotFound("Bu ay için rapor bulunamadı. Önce rapor oluşturunuz.");

        return Ok(result);
    }

    [HttpGet("my")]
    public async Task<IActionResult> GetMyReports()
    {
        var userId = GetUserId();

        var result = await _carbonReportService.GetMyReportsAsync(userId);

        return Ok(result);
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary()
    {
        var userId = GetUserId();

        var result = await _carbonReportService.GetSummaryAsync(userId);

        return Ok(result);
    }

    private string GetUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    }

    private static bool IsValidMonthAndYear(int month, int year)
    {
        return month >= 1 && month <= 12 && year >= 2024 && year <= DateTime.Now.Year + 1;
    }
}
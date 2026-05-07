// ProfileController, giriş yapan kullanıcının profil bilgilerini döndüren API endpointlerini içerir.

using System.Security.Claims;
using FoodWise.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodWise.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly IProfileService _profileService;

    public ProfileController(IProfileService profileService)
    {
        _profileService = profileService;
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMyProfile()
    {
        var userId = GetUserId();

        var result = await _profileService.GetMyProfileAsync(userId);

        if (result == null)
            return NotFound("Kullanıcı profili bulunamadı.");

        return Ok(result);
    }

    private string GetUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    }
}
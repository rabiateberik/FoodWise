// ProfileController, giriş yapan kullanıcının profil bilgilerini yönetir.
// Kullanıcı profil bilgilerini görüntüleyebilir, güncelleyebilir ve şifresini değiştirebilir.

using System.Security.Claims;
using FoodWise.Application.DTOs.Profile;
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

    [HttpPut("me")]
    public async Task<IActionResult> UpdateMyProfile(UpdateProfileDto model)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = GetUserId();

        var result = await _profileService.UpdateProfileAsync(userId, model);

        if (!result)
            return BadRequest("Profil bilgileri güncellenemedi.");

        return Ok("Profil bilgileri başarıyla güncellendi.");
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordDto model)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = GetUserId();

        var result = await _profileService.ChangePasswordAsync(userId, model);

        if (!result)
            return BadRequest("Şifre değiştirilemedi. Mevcut şifreyi ve yeni şifre bilgilerini kontrol edin.");

        return Ok("Şifre başarıyla değiştirildi.");
    }

    private string GetUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    }
    [HttpPost("delete-account")]
    public async Task<IActionResult> DeleteAccount(DeleteAccountDto model)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = GetUserId();

        var result = await _profileService.DeleteAccountAsync(userId, model);

        if (!result)
            return BadRequest("Hesap silinemedi. Şifreni ve onay metnini kontrol et.");

        return Ok("Hesap başarıyla pasif hale getirildi.");
    }
}
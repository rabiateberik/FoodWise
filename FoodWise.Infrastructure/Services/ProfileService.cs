// ProfileService, ASP.NET Identity kullanıcı tablosundan giriş yapan kullanıcının profil bilgilerini getirir.

using FoodWise.Application.DTOs.Profile;
using FoodWise.Application.Interfaces;
using FoodWise.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace FoodWise.Infrastructure.Services;

public class ProfileService : IProfileService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public ProfileService(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<ProfileDto?> GetMyProfileAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
            return null;

        return new ProfileDto
        {
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            City = user.City,
            District = user.District,
            Neighborhood = user.Neighborhood,
            NeedScore = user.NeedScore,
            ReliabilityScore = user.ReliabilityScore,
            CreatedAt = user.CreatedAt
        };
    }
}
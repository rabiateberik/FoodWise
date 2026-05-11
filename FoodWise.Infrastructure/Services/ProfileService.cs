// ProfileService, ASP.NET Identity kullanıcı tablosundan giriş yapan kullanıcının profil bilgilerini yönetir.
// Kullanıcı profil bilgilerini görüntüleyebilir, konum bilgilerini güncelleyebilir, şifresini değiştirebilir ve hesabını pasif hale getirebilir.

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

    public async Task<bool> UpdateProfileAsync(string userId, UpdateProfileDto model)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
            return false;

        // E-posta güvenlik nedeniyle bu ekrandan güncellenmez.
        // Kullanıcı sadece ad soyad ve konum bilgilerini günceller.
        user.FullName = model.FullName.Trim();
        user.City = string.IsNullOrWhiteSpace(model.City) ? null : model.City.Trim();
        user.District = string.IsNullOrWhiteSpace(model.District) ? null : model.District.Trim();
        user.Neighborhood = string.IsNullOrWhiteSpace(model.Neighborhood) ? null : model.Neighborhood.Trim();
        user.UpdatedAt = DateTime.Now;

        var result = await _userManager.UpdateAsync(user);

        return result.Succeeded;
    }

    public async Task<bool> ChangePasswordAsync(string userId, ChangePasswordDto model)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
            return false;

        if (model.NewPassword != model.ConfirmNewPassword)
            return false;

        // Şifre doğrudan veritabanında güncellenmez.
        // ASP.NET Identity mevcut şifreyi kontrol ederek güvenli şekilde değiştirir.
        var result = await _userManager.ChangePasswordAsync(
            user,
            model.CurrentPassword,
            model.NewPassword);

        return result.Succeeded;
    }

    public async Task<bool> DeleteAccountAsync(string userId, DeleteAccountDto model)
    {
        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
            return false;

        if (model.ConfirmText.Trim() != "HESABIMI SİL")
            return false;

        var isPasswordValid = await _userManager.CheckPasswordAsync(user, model.CurrentPassword);

        if (!isPasswordValid)
            return false;

        // Hesap fiziksel olarak silinmez.
        // Kullanıcıya ait paylaşım, teslimat, eco puan ve bildirim kayıtları korunur.
        user.IsActive = false;
        user.DeletedAt = DateTime.Now;
        user.UpdatedAt = DateTime.Now;

        // Kullanıcının tekrar giriş yapmasını engellemek için hesap kilitlenir.
        user.LockoutEnabled = true;
        user.LockoutEnd = DateTimeOffset.MaxValue;

        var result = await _userManager.UpdateAsync(user);

        return result.Succeeded;
    }
}
// ProfileService için servis sözleşmesidir.
// Profil görüntüleme, profil güncelleme ve şifre değiştirme işlemlerini tanımlar.

using FoodWise.Application.DTOs.Profile;

namespace FoodWise.Application.Interfaces;

public interface IProfileService
{
    Task<ProfileDto?> GetMyProfileAsync(string userId);

    Task<bool> UpdateProfileAsync(string userId, UpdateProfileDto model);

    Task<bool> ChangePasswordAsync(string userId, ChangePasswordDto model);
    Task<bool> DeleteAccountAsync(string userId, DeleteAccountDto model);
}
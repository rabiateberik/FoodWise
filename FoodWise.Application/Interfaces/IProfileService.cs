// Bu interface, kullanıcı profil bilgilerini getiren servis sözleşmesini tanımlar.

using FoodWise.Application.DTOs.Profile;

namespace FoodWise.Application.Interfaces;

public interface IProfileService
{
    Task<ProfileDto?> GetMyProfileAsync(string userId);
}
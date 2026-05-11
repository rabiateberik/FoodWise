// Bu interface, FoodWise.Web projesinin Profile API ile haberleşmesi için gereken metotları tanımlar.

using FoodWise.Web.ViewModels.Profile;

namespace FoodWise.Web.Services;

public interface IProfileWebService
{
    Task<ProfileViewModel?> GetMyProfileAsync(string token);

    // Kullanıcının profil ve konum bilgilerini API üzerinden günceller.
    Task<bool> UpdateProfileAsync(UpdateProfileViewModel model, string token);

    // Kullanıcının şifresini API üzerinden değiştirir.
    Task<bool> ChangePasswordAsync(ChangePasswordViewModel model, string token);
    //hesap silme
    Task<bool> DeleteAccountAsync(DeleteAccountViewModel model, string token);
}
// Bu interface, FoodWise.Web projesinin Profile API ile haberleşmesi için gereken metotları tanımlar.

using FoodWise.Web.ViewModels.Profile;

namespace FoodWise.Web.Services;

public interface IProfileWebService
{
    Task<ProfileViewModel?> GetMyProfileAsync(string token);
}
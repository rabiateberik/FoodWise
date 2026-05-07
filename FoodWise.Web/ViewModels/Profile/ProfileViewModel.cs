// Bu ViewModel, API'den gelen profil bilgilerini Web arayüzünde göstermek için kullanılır.

namespace FoodWise.Web.ViewModels.Profile;

public class ProfileViewModel
{
    public string UserId { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? City { get; set; }

    public string? District { get; set; }

    public string? Neighborhood { get; set; }

    public int NeedScore { get; set; }

    public int ReliabilityScore { get; set; }

    public DateTime CreatedAt { get; set; }
}
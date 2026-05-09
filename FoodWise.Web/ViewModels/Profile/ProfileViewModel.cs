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
    // Kullanıcının toplam eco puanını profil sayfasında göstermek için kullanılır.
    public int EcoPoint { get; set; }

    // Kullanıcının eco puan seviyesini profil sayfasında göstermek için kullanılır.
    public string EcoPointLevelName { get; set; } = string.Empty;

    // Kullanıcının kaç eco puan işlemi olduğunu göstermek için kullanılır.
    public int EcoPointHistoryCount { get; set; }
}
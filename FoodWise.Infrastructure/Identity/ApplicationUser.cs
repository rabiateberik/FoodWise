// Bu sınıf, FoodWise projesinde sisteme kayıt olan kullanıcıyı temsil eder.
// ASP.NET Core Identity'nin IdentityUser sınıfından türetilmiştir.
// Kullanıcının temel kimlik bilgilerine ek olarak şehir, ilçe ve mahalle bilgileri tutulur.
// FoodWise açık adres saklamaz; kullanıcı gizliliği için sadece bölgesel konum bilgisi alınır.

using Microsoft.AspNetCore.Identity;

namespace FoodWise.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = null!;

    // Kullanıcının bulunduğu şehir bilgisi.
    // Örnek: İstanbul, Ankara, Konya
    public string City { get; set; } = null!;

    // Kullanıcının bulunduğu ilçe bilgisi.
    // Örnek: Kadıköy, Çankaya, Selçuklu
    public string District { get; set; } = null!;

    // Kullanıcının mahalle bilgisi.
    // Açık adres yerine sadece mahalle seviyesi bilgi tutulur.
    public string? Neighborhood { get; set; }

    // İleride konuma yakın teslim noktası önerisi için kullanılabilir.
    public double? Latitude { get; set; }

    public double? Longitude { get; set; }

    // Kullanıcının ihtiyaç durumunu temsil eden puan.
    public int NeedScore { get; set; } = 0;

    // Kullanıcının paylaşım/teslimat güvenilirlik puanı.
    public int ReliabilityScore { get; set; } = 100;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime? UpdatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }
}
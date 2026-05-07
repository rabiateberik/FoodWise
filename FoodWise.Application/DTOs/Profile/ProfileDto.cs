// Bu DTO, giriş yapan kullanıcının profil bilgilerini API dışına taşımak için kullanılır.

namespace FoodWise.Application.DTOs.Profile;

public class ProfileDto
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
// DeliveryPointController, kullanıcıların paylaşım ilanı oluştururken
// aktif teslim noktalarını listeleyebilmesi için kullanılır.
// Kullanıcının kayıtlı şehir/ilçe/mahalle bilgisine göre yakın teslim noktaları öncelikli sıralanır.

using FoodWise.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace FoodWise.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class DeliveryPointController : ControllerBase
{
    private readonly FoodWiseDbContext _context;

    public DeliveryPointController(FoodWiseDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetActiveDeliveryPoints()
    {
        // Sadece aktif teslim noktaları listelenir.
        var deliveryPoints = await _context.DeliveryPoints
            .Where(x => x.IsActive)
            .OrderBy(x => x.City)
            .ThenBy(x => x.District)
            .ThenBy(x => x.Neighborhood)
            .ThenBy(x => x.Name)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.Description,
                x.City,
                x.District,
                x.Neighborhood,
                x.WorkingHours,
                x.StorageType
            })
            .ToListAsync();

        return Ok(deliveryPoints);
    }

    [HttpGet("nearby")]
    public async Task<IActionResult> GetNearbyDeliveryPoints([FromQuery] string? search)
    {
        var userId = GetUserId();

        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == userId && x.IsActive);

        if (user == null)
            return Unauthorized(new { Message = "Kullanıcı bilgisi bulunamadı." });

        var deliveryPoints = await _context.DeliveryPoints
            .AsNoTracking()
            .Where(x => x.IsActive)
            .ToListAsync();

        var normalizedSearch = NormalizeText(search);

        if (!string.IsNullOrWhiteSpace(normalizedSearch))
        {
            deliveryPoints = deliveryPoints
                .Where(x =>
                    ContainsSearchText(x.Name, normalizedSearch) ||
                    ContainsSearchText(x.Description, normalizedSearch) ||
                    ContainsSearchText(x.City, normalizedSearch) ||
                    ContainsSearchText(x.District, normalizedSearch) ||
                    ContainsSearchText(x.Neighborhood, normalizedSearch))
                .ToList();
        }

        var result = deliveryPoints
            .Select(x =>
            {
                var locationPriority = CalculateLocationPriority(
                    user.City,
                    user.District,
                    user.Neighborhood,
                    x.City,
                    x.District,
                    x.Neighborhood
                );

                return new
                {
                    x.Id,
                    x.Name,
                    x.Description,
                    x.City,
                    x.District,
                    x.Neighborhood,
                    x.WorkingHours,
                    x.StorageType,
                    x.Latitude,
                    x.Longitude,
                    LocationPriority = locationPriority,
                    LocationMatchText = GetLocationMatchText(locationPriority)
                };
            })
            .OrderBy(x => x.LocationPriority)
            .ThenBy(x => x.City)
            .ThenBy(x => x.District)
            .ThenBy(x => x.Neighborhood)
            .ThenBy(x => x.Name)
            .ToList();

        return Ok(result);
    }

    private string GetUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    }

    private static int CalculateLocationPriority(
        string? userCity,
        string? userDistrict,
        string? userNeighborhood,
        string? pointCity,
        string? pointDistrict,
        string? pointNeighborhood)
    {
        var normalizedUserCity = NormalizeText(userCity);
        var normalizedUserDistrict = NormalizeText(userDistrict);
        var normalizedUserNeighborhood = NormalizeText(userNeighborhood);

        var normalizedPointCity = NormalizeText(pointCity);
        var normalizedPointDistrict = NormalizeText(pointDistrict);
        var normalizedPointNeighborhood = NormalizeText(pointNeighborhood);

        var sameCity =
            !string.IsNullOrWhiteSpace(normalizedUserCity) &&
            normalizedUserCity == normalizedPointCity;

        var sameDistrict =
            sameCity &&
            !string.IsNullOrWhiteSpace(normalizedUserDistrict) &&
            normalizedUserDistrict == normalizedPointDistrict;

        var sameNeighborhood =
            sameDistrict &&
            !string.IsNullOrWhiteSpace(normalizedUserNeighborhood) &&
            normalizedUserNeighborhood == normalizedPointNeighborhood;

        if (sameNeighborhood)
            return 1;

        if (sameDistrict)
            return 2;

        if (sameCity)
            return 3;

        return 99;
    }

    private static string GetLocationMatchText(int locationPriority)
    {
        return locationPriority switch
        {
            1 => "Aynı mahalle",
            2 => "Aynı ilçe",
            3 => "Aynı şehir",
            _ => "Diğer bölge"
        };
    }

    private static bool ContainsSearchText(string? value, string normalizedSearch)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        return NormalizeText(value).Contains(normalizedSearch);
    }

    private static string NormalizeText(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        var text = input.Trim().ToLower();

        text = text
            .Replace("ç", "c")
            .Replace("ğ", "g")
            .Replace("ı", "i")
            .Replace("ö", "o")
            .Replace("ş", "s")
            .Replace("ü", "u");

        text = Regex.Replace(text, @"[^a-z0-9\s]", " ");
        text = Regex.Replace(text, @"\s+", " ").Trim();

        return text;
    }
}
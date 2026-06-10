using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Paylaşım ilanına gelen talep bilgilerini API cevabında göstermek için kullanılır.
namespace FoodWise.Application.DTOs.Sharing;

public class ShareRequestDto
{
    public int Id { get; set; }

    public int ShareListingId { get; set; }

    public string ShareListingTitle { get; set; } = null!;

    public string ProductName { get; set; } = null!;

    public string RequesterUserId { get; set; } = null!;

    public decimal? MatchScore { get; set; }

    public string Status { get; set; } = null!;

    public DateTime RequestedAt { get; set; }

    public DateTime? RespondedAt { get; set; }
    // Bu talep için teslimat oluşturulduysa true olur.
    public bool HasDelivery { get; set; }

    // Oluşturulan teslimat Id bilgisidir.
    public int? DeliveryId { get; set; }
    public string? RequesterFullName { get; set; }
}
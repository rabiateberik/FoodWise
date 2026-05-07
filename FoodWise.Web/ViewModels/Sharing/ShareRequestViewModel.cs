// Bu ViewModel, paylaşım ilanına gelen veya gönderilen talep bilgilerini Web arayüzünde göstermek için kullanılır.

namespace FoodWise.Web.ViewModels.Sharing;

public class ShareRequestViewModel
{
    public int Id { get; set; }

    public int ShareListingId { get; set; }

    public string ShareListingTitle { get; set; } = string.Empty;

    public string ProductName { get; set; } = string.Empty;

    public string RequesterUserId { get; set; } = string.Empty;

    public decimal? MatchScore { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTime RequestedAt { get; set; }

    public DateTime? RespondedAt { get; set; }
}
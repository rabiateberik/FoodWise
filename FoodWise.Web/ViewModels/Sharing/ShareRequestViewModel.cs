// Bu ViewModel, paylaşım ilanına gelen veya gönderilen talep bilgilerini Web arayüzünde göstermek için kullanılır.
// Talep eden kullanıcı adı ve eşleşme skoru web arayüzünde bu model üzerinden gösterilir.

namespace FoodWise.Web.ViewModels.Sharing;

public class ShareRequestViewModel
{
    public int Id { get; set; }

    public int ShareListingId { get; set; }

    public string ShareListingTitle { get; set; } = string.Empty;

    public string ProductName { get; set; } = string.Empty;

    public string RequesterUserId { get; set; } = string.Empty;

    // Talep eden kullanıcının görünen adıdır.
    // Backend ShareRequestDto içinden RequesterFullName olarak gelir.
    public string? RequesterFullName { get; set; }

    public decimal? MatchScore { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTime RequestedAt { get; set; }

    public DateTime? RespondedAt { get; set; }

    // Onaylanan talep için teslimat oluşturulup oluşturulmadığını gösterir.
    // Backend bu alanı gönderiyorsa web tarafında da okunabilir.
    public bool HasDelivery { get; set; }

    public int? DeliveryId { get; set; }

    public string RequesterDisplayName =>
        !string.IsNullOrWhiteSpace(RequesterFullName)
            ? RequesterFullName
            : "Kullanıcı";

    public decimal MatchScoreValue => MatchScore ?? 0;

    public string MatchScoreText =>
        MatchScore.HasValue
            ? $"%{MatchScore.Value:0}"
            : "-";

    public string MatchScoreLevel
    {
        get
        {
            if (!MatchScore.HasValue)
                return "Skor yok";

            if (MatchScore.Value >= 75)
                return "Yüksek eşleşme";

            if (MatchScore.Value >= 50)
                return "Orta eşleşme";

            return "Düşük eşleşme";
        }
    }

    public string MatchScoreCssClass
    {
        get
        {
            if (!MatchScore.HasValue)
                return "score-low";

            if (MatchScore.Value >= 75)
                return "score-high";

            if (MatchScore.Value >= 50)
                return "score-medium";

            return "score-low";
        }
    }

    public bool IsPending =>
        string.Equals(Status, "Pending", StringComparison.OrdinalIgnoreCase);

    public bool IsApproved =>
        string.Equals(Status, "Approved", StringComparison.OrdinalIgnoreCase);

    public bool IsRejected =>
        string.Equals(Status, "Rejected", StringComparison.OrdinalIgnoreCase);

    public bool IsCancelled =>
        string.Equals(Status, "Cancelled", StringComparison.OrdinalIgnoreCase);
}
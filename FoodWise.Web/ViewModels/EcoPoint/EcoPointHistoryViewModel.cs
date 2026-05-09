// EcoPointHistoryViewModel, kullanıcının eco puan kazanma geçmişini Web MVC tarafında göstermek için kullanılır.

namespace FoodWise.Web.ViewModels.EcoPoint;

public class EcoPointHistoryViewModel
{
    public int Id { get; set; }

    public int Point { get; set; }

    public string ActionTypeText { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public int? DeliveryId { get; set; }

    public DateTime CreatedAt { get; set; }
}
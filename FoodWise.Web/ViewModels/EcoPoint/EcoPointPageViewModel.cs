// EcoPointPageViewModel, Eco Puan sayfasında özet bilgileri ve puan geçmişini birlikte taşır.

namespace FoodWise.Web.ViewModels.EcoPoint;

public class EcoPointPageViewModel
{
    public EcoPointSummaryViewModel Summary { get; set; } = new();

    public List<EcoPointHistoryViewModel> History { get; set; } = new();
}
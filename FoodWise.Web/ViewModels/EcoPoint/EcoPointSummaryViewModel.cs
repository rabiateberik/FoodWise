// EcoPointSummaryViewModel, kullanıcının toplam eco puanını ve seviyesini Web MVC tarafında göstermek için kullanılır.

namespace FoodWise.Web.ViewModels.EcoPoint;

public class EcoPointSummaryViewModel
{
    public int TotalPoint { get; set; }

    public string LevelName { get; set; } = string.Empty;

    public int HistoryCount { get; set; }
}
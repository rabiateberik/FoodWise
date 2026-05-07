// Bu ViewModel, aylık karbon raporu verilerini Web arayüzünde göstermek için kullanılır.

namespace FoodWise.Web.ViewModels.CarbonReport;

public class CarbonReportViewModel
{
    public int Id { get; set; }

    public int Month { get; set; }

    public int Year { get; set; }

    public decimal SavedFoodKg { get; set; }

    public decimal EstimatedCarbonSaved { get; set; }

    public int SharedProductCount { get; set; }

    public int WastedProductCount { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
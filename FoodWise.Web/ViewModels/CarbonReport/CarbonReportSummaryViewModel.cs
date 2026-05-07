// Bu ViewModel, kullanıcının tüm karbon raporlarının özet değerlerini taşır.

namespace FoodWise.Web.ViewModels.CarbonReport;

public class CarbonReportSummaryViewModel
{
    public decimal TotalSavedFoodKg { get; set; }

    public decimal TotalEstimatedCarbonSaved { get; set; }

    public int TotalSharedProductCount { get; set; }

    public int TotalWastedProductCount { get; set; }

    public int ReportCount { get; set; }
}
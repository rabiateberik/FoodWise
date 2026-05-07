// Bu interface, FoodWise.Web projesinin CarbonReport API ile haberleşmesi için gereken metotları tanımlar.
// Controller doğrudan HttpClient kullanmaz; karbon raporu işlemleri bu servis üzerinden yapılır.

using FoodWise.Web.ViewModels.CarbonReport;

namespace FoodWise.Web.Services;

public interface ICarbonReportWebService
{
    Task<CarbonReportViewModel?> GenerateMonthlyReportAsync(int month, int year, string token);

    Task<List<CarbonReportViewModel>> GetMyReportsAsync(string token);

    Task<CarbonReportSummaryViewModel> GetSummaryAsync(string token);
}
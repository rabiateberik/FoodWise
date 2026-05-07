// Bu ViewModel, Karbon Raporu sayfasındaki özet, rapor listesi ve rapor oluşturma alanlarını tek modelde toplar.

using System.ComponentModel.DataAnnotations;

namespace FoodWise.Web.ViewModels.CarbonReport;

public class CarbonReportPageViewModel
{
    public CarbonReportSummaryViewModel Summary { get; set; } = new();

    public List<CarbonReportViewModel> Reports { get; set; } = new();

    [Required(ErrorMessage = "Ay seçimi zorunludur.")]
    [Range(1, 12, ErrorMessage = "Ay 1 ile 12 arasında olmalıdır.")]
    public int Month { get; set; } = DateTime.Now.Month;

    [Required(ErrorMessage = "Yıl seçimi zorunludur.")]
    [Range(2024, 2030, ErrorMessage = "Geçerli bir yıl giriniz.")]
    public int Year { get; set; } = DateTime.Now.Year;
}

using FoodWise.Application.DTOs.Report;

namespace FoodWise.Application.Interfaces;

// Karbon raporu işlemlerinin servis katmanında hangi metotlarla yapılacağını tanımlar.
// Controller, aylık rapor oluşturma ve rapor görüntüleme işlemleri için bu interface üzerinden servis katmanına erişir.
public interface ICarbonReportService
{
    Task<CarbonReportDto> GenerateMonthlyReportAsync(string userId, int month, int year);

    Task<CarbonReportDto?> GetMonthlyReportAsync(string userId, int month, int year);

    Task<List<CarbonReportDto>> GetMyReportsAsync(string userId);

    Task<CarbonReportSummaryDto> GetSummaryAsync(string userId);
}


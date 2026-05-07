using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FoodWise.Application.DTOs.Report;

namespace FoodWise.Application.Interfaces;

public interface ICarbonReportService
{
    Task<CarbonReportDto> GenerateMonthlyReportAsync(string userId, int month, int year);

    Task<CarbonReportDto?> GetMonthlyReportAsync(string userId, int month, int year);

    Task<List<CarbonReportDto>> GetMyReportsAsync(string userId);

    Task<CarbonReportSummaryDto> GetSummaryAsync(string userId);
}
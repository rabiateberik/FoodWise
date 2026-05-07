using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace FoodWise.Application.DTOs.Report;

public class CarbonReportSummaryDto
{
    public decimal TotalSavedFoodKg { get; set; }

    public decimal TotalEstimatedCarbonSaved { get; set; }

    public int TotalSharedProductCount { get; set; }

    public int TotalWastedProductCount { get; set; }

    public int ReportCount { get; set; }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoodWise.Application.DTOs.Report;

public class CarbonReportDto
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
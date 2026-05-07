using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FoodWise.Domain.Common;

namespace FoodWise.Domain.Entities;

public class CarbonReport : BaseEntity
{
    public string UserId { get; set; } = null!;

    public int Month { get; set; }

    public int Year { get; set; }

    public decimal SavedFoodKg { get; set; }

    public decimal EstimatedCarbonSaved { get; set; }

    public int SharedProductCount { get; set; }

    public int WastedProductCount { get; set; }
}

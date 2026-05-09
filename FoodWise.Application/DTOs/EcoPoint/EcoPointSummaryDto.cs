using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// EcoPointSummaryDto, kullanıcının toplam eco puan bilgisini ve seviyesini taşır.

namespace FoodWise.Application.DTOs.EcoPoint;

public class EcoPointSummaryDto
{
    public int TotalPoint { get; set; }

    public string LevelName { get; set; } = string.Empty;

    public int HistoryCount { get; set; }
}
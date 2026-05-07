using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoodWise.Domain.Enums;

public enum StockItemStatus
{
    Active = 1,
    Consumed = 2,
    Shared = 3,
    Wasted = 4,
    Expired = 5,
    Deleted = 6
}

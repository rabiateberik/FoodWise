using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoodWise.Domain.Enums;

public enum ShareListingStatus
{
    Available = 1,
    Requested = 2,
    Approved = 3,
    QrGenerated = 4,
    Delivered = 5,
    Cancelled = 6,
    Expired = 7
}
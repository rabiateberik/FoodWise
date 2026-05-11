using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoodWise.Domain.Enums;

public enum NotificationType
{
    RiskWarning = 1,
    RecipeSuggestion = 2,
    ShareRequest = 3,
    RequestApproved = 4,
    DeliveryCompleted = 5,
    Report = 6,

    RequestRejected = 7,
    DeliveryCreated = 8,
    System = 9,
    DeliveryDroppedOff = 10

}
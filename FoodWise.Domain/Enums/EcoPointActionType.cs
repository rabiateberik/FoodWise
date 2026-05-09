using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// EcoPointActionType, kullanıcının hangi işlemden eco puan kazandığını belirtir.

namespace FoodWise.Domain.Enums;

public enum EcoPointActionType
{
    StockAdded = 1,

    ShareListingCreated = 2,

    ShareRequestApproved = 3,

    DeliveryCompletedAsDonor = 4,

    DeliveryCompletedAsReceiver = 5,

    CarbonSavingBonus = 6
}
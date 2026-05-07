using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FoodWise.Domain.Common;
using FoodWise.Domain.Enums;

namespace FoodWise.Domain.Entities;

public class Notification : BaseEntity
{
    public string UserId { get; set; } = null!;

    public string Title { get; set; } = null!;

    public string Message { get; set; } = null!;

    public NotificationType Type { get; set; }

    public bool IsRead { get; set; } = false;
}
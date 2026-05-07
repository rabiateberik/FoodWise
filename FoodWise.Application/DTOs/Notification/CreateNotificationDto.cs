using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Yeni bildirim oluşturmak için kullanılan modeldir.
// İlk aşamada test bildirimi oluşturmak için kullanılacaktır.
using FoodWise.Domain.Enums;

namespace FoodWise.Application.DTOs.Notification;

public class CreateNotificationDto
{
    public string Title { get; set; } = null!;

    public string Message { get; set; } = null!;

    public NotificationType Type { get; set; }
}
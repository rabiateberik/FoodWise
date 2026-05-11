using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
// Kullanıcıya gösterilecek bildirim bilgilerini API cevabı olarak taşır.
namespace FoodWise.Application.DTOs.Notification;

public class NotificationDto
{
    public int Id { get; set; }

    public string Title { get; set; } = null!;

    public string Message { get; set; } = null!;

    public string Type { get; set; } = null!;

    public bool IsRead { get; set; }
    public string? TargetUrl { get; set; }
    public DateTime CreatedAt { get; set; }
}
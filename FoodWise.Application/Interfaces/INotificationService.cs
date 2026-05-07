using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Bildirim işlemlerinin servis sözleşmesidir.
// Controller, kullanıcı bildirimlerini bu interface üzerinden yönetir.
using FoodWise.Application.DTOs.Notification;

namespace FoodWise.Application.Interfaces;

public interface INotificationService
{
    Task<List<NotificationDto>> GetUserNotificationsAsync(string userId);

    Task<List<NotificationDto>> GetUnreadNotificationsAsync(string userId);

    Task<int> GetUnreadCountAsync(string userId);

    Task<NotificationDto> CreateAsync(string userId, CreateNotificationDto model);

    Task<NotificationDto?> MarkAsReadAsync(string userId, int notificationId);

    Task<bool> MarkAllAsReadAsync(string userId);

    Task<bool> DeleteAsync(string userId, int notificationId);
}
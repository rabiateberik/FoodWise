using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// NotificationService, kullanıcı bildirimlerini yönetir.
// Bildirim listeleme, okundu yapma, okunmamış sayısı ve bildirim oluşturma işlemleri burada yapılır.

using FoodWise.Application.DTOs.Notification;
using FoodWise.Application.Interfaces;
using FoodWise.Domain.Entities;
using FoodWise.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FoodWise.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly FoodWiseDbContext _context;

    public NotificationService(FoodWiseDbContext context)
    {
        _context = context;
    }

    // Kullanıcının tüm aktif bildirimlerini getirir.
    // Bildirimler en yeni kayıt en üstte olacak şekilde sıralanır.
    public async Task<List<NotificationDto>> GetUserNotificationsAsync(string userId)
    {
        var notifications = await _context.Notifications
            .Where(x => x.UserId == userId && x.IsActive)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        return notifications.Select(MapToDto).ToList();
    }

    // Kullanıcının sadece okunmamış aktif bildirimlerini listeler.
    public async Task<List<NotificationDto>> GetUnreadNotificationsAsync(string userId)
    {
        var notifications = await _context.Notifications
            .Where(x => x.UserId == userId && x.IsActive && !x.IsRead)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        return notifications.Select(MapToDto).ToList();
    }

    // Bildirim rozeti veya dashboard için okunmamış bildirim sayısını döndürür.
    public async Task<int> GetUnreadCountAsync(string userId)
    {
        return await _context.Notifications
            .CountAsync(x => x.UserId == userId && x.IsActive && !x.IsRead);
    }

    // Kullanıcıya yeni bildirim oluşturur.
    // Risk, paylaşım ve teslimat servisleri bu metodu kullanarak bildirim üretebilir.
    public async Task<NotificationDto> CreateAsync(string userId, CreateNotificationDto model)
    {
        var notification = new Notification
        {
            UserId = userId,
            Title = model.Title,
            Message = model.Message,
            Type = model.Type,
            TargetUrl = model.TargetUrl,
            IsRead = false,
            IsActive = true,
            CreatedAt = DateTime.Now
        };

        await _context.Notifications.AddAsync(notification);
        await _context.SaveChangesAsync();

        return MapToDto(notification);
    }

    // Belirli bir bildirimi okundu olarak işaretler.
    // Kullanıcı sadece kendi bildirimleri üzerinde işlem yapabilir.
    public async Task<NotificationDto?> MarkAsReadAsync(string userId, int notificationId)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(x =>
                x.Id == notificationId &&
                x.UserId == userId &&
                x.IsActive);

        if (notification == null)
            return null;

        notification.IsRead = true;
        notification.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        return MapToDto(notification);
    }

    // Kullanıcının tüm okunmamış aktif bildirimlerini okundu hale getirir.
    public async Task<bool> MarkAllAsReadAsync(string userId)
    {
        var notifications = await _context.Notifications
            .Where(x => x.UserId == userId && x.IsActive && !x.IsRead)
            .ToListAsync();

        if (!notifications.Any())
            return false;

        foreach (var notification in notifications)
        {
            notification.IsRead = true;
            notification.UpdatedAt = DateTime.Now;
        }

        await _context.SaveChangesAsync();

        return true;
    }

    // Bildirimi fiziksel olarak silmek yerine pasif hale getirir.
    // Böylece veri tamamen kaybolmaz ve gerektiğinde geçmiş kayıt olarak tutulabilir.
    public async Task<bool> DeleteAsync(string userId, int notificationId)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(x =>
                x.Id == notificationId &&
                x.UserId == userId &&
                x.IsActive);

        if (notification == null)
            return false;

        notification.IsActive = false;
        notification.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        return true;
    }

    // Notification entity'sini API cevabında kullanılacak NotificationDto modeline dönüştürür.
    private static NotificationDto MapToDto(Notification notification)
    {
        return new NotificationDto
        {
            Id = notification.Id,
            Title = notification.Title,
            Message = notification.Message,
            Type = notification.Type.ToString(),
            IsRead = notification.IsRead,
            CreatedAt = notification.CreatedAt,
            TargetUrl = notification.TargetUrl
        };
    }
}


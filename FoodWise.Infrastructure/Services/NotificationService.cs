using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// NotificationService, kullanıcı bildirimlerini yönetir.
// Bildirim listeleme, okundu yapma, okunmamış sayısı ve test bildirimi oluşturma işlemleri burada yapılır.
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

    public async Task<List<NotificationDto>> GetUserNotificationsAsync(string userId)
    {
        // Kullanıcının aktif bildirimleri en yeni bildirim üstte olacak şekilde listelenir.
        var notifications = await _context.Notifications
            .Where(x => x.UserId == userId && x.IsActive)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        return notifications.Select(MapToDto).ToList();
    }

    public async Task<List<NotificationDto>> GetUnreadNotificationsAsync(string userId)
    {
        // Sadece okunmamış aktif bildirimler getirilir.
        var notifications = await _context.Notifications
            .Where(x => x.UserId == userId && x.IsActive && !x.IsRead)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        return notifications.Select(MapToDto).ToList();
    }

    public async Task<int> GetUnreadCountAsync(string userId)
    {
        // Dashboard veya bildirim ikonu için okunmamış bildirim sayısı döndürülür.
        return await _context.Notifications
            .CountAsync(x => x.UserId == userId && x.IsActive && !x.IsRead);
    }

    public async Task<NotificationDto> CreateAsync(string userId, CreateNotificationDto model)
    {
        // Yeni bildirim oluşturulur.
        // İleride risk, paylaşım ve teslimat servisleri de bu metodu kullanabilir.
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

    public async Task<NotificationDto?> MarkAsReadAsync(string userId, int notificationId)
    {
        // Kullanıcı sadece kendi bildirimini okundu yapabilir.
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

    public async Task<bool> MarkAllAsReadAsync(string userId)
    {
        // Kullanıcının tüm okunmamış bildirimleri okundu yapılır.
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

    public async Task<bool> DeleteAsync(string userId, int notificationId)
    {
        // Bildirim fiziksel olarak silinmez, pasif yapılır.
        // Böylece ileride raporlama veya loglama için veri korunabilir.
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

    private static NotificationDto MapToDto(Notification notification)
    {
        // Entity, API cevabında kullanılacak DTO modeline dönüştürülür.
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
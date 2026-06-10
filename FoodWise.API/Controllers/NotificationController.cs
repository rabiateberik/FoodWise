// NotificationController, kullanıcı bildirimlerini yöneten endpointleri içerir.
// Bildirimler token ile korunan kullanıcıya özel veriler olduğu için controller [Authorize] ile korunur.
using System.Security.Claims;
using FoodWise.Application.DTOs.Notification;
using FoodWise.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodWise.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class NotificationController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpGet]
    public async Task<IActionResult> GetMyNotifications()
    {
        // Giriş yapan kullanıcının tüm aktif bildirimleri listelenir.
        var userId = GetUserId();

        var result = await _notificationService.GetUserNotificationsAsync(userId);

        return Ok(result);
    }

    [HttpGet("unread")]
    public async Task<IActionResult> GetUnreadNotifications()
    {
        // Giriş yapan kullanıcının okunmamış bildirimleri listelenir.
        var userId = GetUserId();

        var result = await _notificationService.GetUnreadNotificationsAsync(userId);

        return Ok(result);
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        // Dashboard veya bildirim rozeti için okunmamış bildirim sayısı döndürülür.
        var userId = GetUserId();

        var result = await _notificationService.GetUnreadCountAsync(userId);

        return Ok(new { unreadCount = result });
    }

    [HttpPost("test")]
    public async Task<IActionResult> CreateTestNotification(CreateNotificationDto model)
    {
        // Test amaçlı bildirim oluşturur.
        // İleride bu işlem risk, paylaşım ve teslimat servisleri tarafından otomatik yapılacaktır.
        var userId = GetUserId();

        var result = await _notificationService.CreateAsync(userId, model);

        return Ok(result);
    }

    [HttpPatch("{notificationId}/read")]
    public async Task<IActionResult> MarkAsRead(int notificationId)
    {
        // Belirli bir bildirimi okundu olarak işaretler.
        var userId = GetUserId();

        var result = await _notificationService.MarkAsReadAsync(userId, notificationId);

        if (result == null)
            return NotFound("Bildirim bulunamadı.");

        return Ok(result);
    }

    [HttpPatch("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        // Kullanıcının tüm okunmamış bildirimlerini okundu yapar.
        var userId = GetUserId();

        var result = await _notificationService.MarkAllAsReadAsync(userId);

        if (!result)
            return Ok("Okunmamış bildirim bulunamadı.");

        return Ok("Tüm bildirimler okundu olarak işaretlendi.");
    }

    [HttpDelete("{notificationId}")]
    public async Task<IActionResult> Delete(int notificationId)
    {
        // Bildirimi pasif hale getirir.
        var userId = GetUserId();

        var result = await _notificationService.DeleteAsync(userId, notificationId);

        if (!result)
            return NotFound("Silinecek bildirim bulunamadı.");

        return Ok("Bildirim silindi.");
    }

    private string GetUserId() 
    {
        // JWT token içindeki NameIdentifier claim'i Identity kullanıcı Id bilgisini taşır.
        return User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    }
}
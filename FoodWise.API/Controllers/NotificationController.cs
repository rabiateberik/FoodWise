// NotificationController, kullanıcıya ait bildirim işlemlerini yöneten endpointleri içerir.
// Bildirimler kullanıcıya özel olduğu için controller token doğrulaması ile korunur.

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

    // Giriş yapan kullanıcının tüm aktif bildirimlerini listeler.
    [HttpGet]
    public async Task<IActionResult> GetMyNotifications()
    {
        var userId = GetUserId();

        var result = await _notificationService.GetUserNotificationsAsync(userId);

        return Ok(result);
    }

    // Kullanıcının sadece okunmamış bildirimlerini getirir.
    [HttpGet("unread")]
    public async Task<IActionResult> GetUnreadNotifications()
    {
        var userId = GetUserId();

        var result = await _notificationService.GetUnreadNotificationsAsync(userId);

        return Ok(result);
    }

    // Bildirim rozeti için okunmamış bildirim sayısını döndürür.
    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        var userId = GetUserId();

        var result = await _notificationService.GetUnreadCountAsync(userId);

        return Ok(new { unreadCount = result });
    }

    // Test amaçlı bildirim oluşturmak için kullanılır.
    // Normal akışta bildirimler risk, paylaşım veya teslimat işlemlerinden otomatik üretilebilir.
    [HttpPost("test")]
    public async Task<IActionResult> CreateTestNotification(CreateNotificationDto model)
    {
        var userId = GetUserId();

        var result = await _notificationService.CreateAsync(userId, model);

        return Ok(result);
    }

    // Seçilen bildirimi okundu olarak işaretler.
    [HttpPatch("{notificationId}/read")]
    public async Task<IActionResult> MarkAsRead(int notificationId)
    {
        var userId = GetUserId();

        var result = await _notificationService.MarkAsReadAsync(userId, notificationId);

        if (result == null)
            return NotFound("Bildirim bulunamadı.");

        return Ok(result);
    }

    // Kullanıcının tüm okunmamış bildirimlerini tek işlemde okundu yapar.
    [HttpPatch("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var userId = GetUserId();

        var result = await _notificationService.MarkAllAsReadAsync(userId);

        if (!result)
            return Ok("Okunmamış bildirim bulunamadı.");

        return Ok("Tüm bildirimler okundu olarak işaretlendi.");
    }

    // Bildirimi tamamen silmek yerine pasif hale getirir.
    [HttpDelete("{notificationId}")]
    public async Task<IActionResult> Delete(int notificationId)
    {
        var userId = GetUserId();

        var result = await _notificationService.DeleteAsync(userId, notificationId);

        if (!result)
            return NotFound("Silinecek bildirim bulunamadı.");

        return Ok("Bildirim silindi.");
    }

    // JWT token içindeki kullanıcı Id bilgisini alır.
    private string GetUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    }
}


// DeliveryController, QR destekli teslim kutusu akışını yöneten endpointleri içerir.
// Teslimat oluşturma, kutuya bırakma, QR okutma ve teslimatı tamamlama işlemleri buradan yapılır.
using System.Security.Claims;
using FoodWise.Application.DTOs.Delivery;
using FoodWise.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodWise.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class DeliveryController : ControllerBase
{
    private readonly IDeliveryService _deliveryService;

    public DeliveryController(IDeliveryService deliveryService)
    {
        _deliveryService = deliveryService;
    }

    [HttpPost("create/{shareRequestId}")]
    public async Task<IActionResult> CreateDelivery(int shareRequestId)
    {
        // Onaylanan paylaşım talebi için teslimat oluşturulur ve boş kutu atanır.
        var userId = GetUserId();

        var result = await _deliveryService.CreateDeliveryAsync(userId, shareRequestId);

        if (result == null)
            return BadRequest("Teslimat oluşturulamadı. Talep durumu, yetki veya boş kutu durumunu kontrol edin.");

        return Ok(result);
    }

    [HttpPost("{deliveryId}/drop-off")]
    public async Task<IActionResult> MarkAsDroppedOff(int deliveryId, DropOffDeliveryDto model)
    {
        // Ürün sahibi ürünü teslim kutusuna bıraktığını işaretler.
        var userId = GetUserId();

        var result = await _deliveryService.MarkAsDroppedOffAsync(userId, deliveryId, model);

        if (result == null)
            return BadRequest("Ürün kutuya bırakıldı olarak işaretlenemedi.");

        return Ok(result);
    }

    [HttpPost("scan-box")]
    public async Task<IActionResult> ScanBoxQr(ScanDeliveryBoxDto model)
    {
        // Alıcı kutudaki QR kodu okutur, sistem aktif teslimatı kontrol eder.
        var userId = GetUserId();

        var result = await _deliveryService.ScanBoxQrAsync(userId, model);

        if (result == null)
            return NotFound("Bu QR kod için size ait aktif bir teslimat bulunamadı.");

        return Ok(result);
    }

    [HttpPost("{deliveryId}/complete")]
    public async Task<IActionResult> CompleteDelivery(int deliveryId)
    {
        // Alıcı ürünü teslim aldığını onaylar.
        var userId = GetUserId();

        var result = await _deliveryService.CompleteDeliveryAsync(userId, deliveryId);

        if (result == null)
            return BadRequest("Teslimat tamamlanamadı. Yetki, durum veya süre bilgisini kontrol edin.");

        return Ok(result);
    }

    [HttpGet("my-donated")]
    public async Task<IActionResult> GetMyDonatedDeliveries()
    {
        // Ürün sahibinin teslimatlarını listeler.
        var userId = GetUserId();

        var result = await _deliveryService.GetMyDonatedDeliveriesAsync(userId);

        return Ok(result);
    }

    [HttpGet("my-received")]
    public async Task<IActionResult> GetMyReceivedDeliveries()
    {
        // Alıcının teslim alacağı veya teslim aldığı ürünleri listeler.
        var userId = GetUserId();

        var result = await _deliveryService.GetMyReceivedDeliveriesAsync(userId);

        return Ok(result);
    }

    private string GetUserId()
    {
        // JWT token içindeki NameIdentifier claim'i Identity kullanıcı Id bilgisini taşır.
        return User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    }
}
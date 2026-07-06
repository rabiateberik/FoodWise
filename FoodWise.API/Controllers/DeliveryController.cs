// DeliveryController, QR destekli teslimat sürecini yöneten endpointleri içerir.
// Teslimat oluşturma, kutuya bırakma, QR okutma ve teslimatı tamamlama işlemleri burada karşılanır.

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

    // Onaylanmış paylaşım talebi için yeni teslimat kaydı oluşturur.
    // Uygun boş teslimat kutusu varsa servis tarafında teslimata atanır.
    [HttpPost("create/{shareRequestId}")]
    public async Task<IActionResult> CreateDelivery(int shareRequestId)
    {
        var userId = GetUserId();

        var result = await _deliveryService.CreateDeliveryAsync(userId, shareRequestId);

        if (result == null)
            return BadRequest("Teslimat oluşturulamadı. Talep durumu, yetki veya boş kutu durumunu kontrol edin.");

        return Ok(result);
    }

    // Ürün sahibi, ürünü teslimat kutusuna bıraktığında bu endpoint kullanılır.
    [HttpPost("{deliveryId}/drop-off")]
    public async Task<IActionResult> MarkAsDroppedOff(int deliveryId, DropOffDeliveryDto model)
    {
        var userId = GetUserId();

        var result = await _deliveryService.MarkAsDroppedOffAsync(userId, deliveryId, model);

        if (result == null)
            return BadRequest("Ürün kutuya bırakıldı olarak işaretlenemedi.");

        return Ok(result);
    }

    // Alıcı QR kodu okuttuğunda ilgili kutuda kendisine ait aktif teslimat olup olmadığı kontrol edilir.
    [HttpPost("scan-box")]
    public async Task<IActionResult> ScanBoxQr(ScanDeliveryBoxDto model)
    {
        var userId = GetUserId();

        var result = await _deliveryService.ScanBoxQrAsync(userId, model);

        if (result == null)
            return NotFound("Bu QR kod için size ait aktif bir teslimat bulunamadı.");

        return Ok(result);
    }

    // Alıcı ürünü teslim aldıktan sonra teslimatı tamamlar.
    [HttpPost("{deliveryId}/complete")]
    public async Task<IActionResult> CompleteDelivery(int deliveryId)
    {
        var userId = GetUserId();

        var result = await _deliveryService.CompleteDeliveryAsync(userId, deliveryId);

        if (result == null)
            return BadRequest("Teslimat tamamlanamadı. Yetki, durum veya süre bilgisini kontrol edin.");

        return Ok(result);
    }

    // Kullanıcının bağışladığı ürünlere ait teslimat kayıtlarını listeler.
    [HttpGet("my-donated")]
    public async Task<IActionResult> GetMyDonatedDeliveries()
    {
        var userId = GetUserId();

        var result = await _deliveryService.GetMyDonatedDeliveriesAsync(userId);

        return Ok(result);
    }

    // Kullanıcının teslim alacağı veya teslim aldığı ürünlere ait kayıtları listeler.
    [HttpGet("my-received")]
    public async Task<IActionResult> GetMyReceivedDeliveries()
    {
        var userId = GetUserId();

        var result = await _deliveryService.GetMyReceivedDeliveriesAsync(userId);

        return Ok(result);
    }

    // JWT token içindeki kullanıcı Id bilgisini alır.
    // Böylece her kullanıcı sadece kendi teslimat işlemlerini gerçekleştirebilir.
    private string GetUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    }
}


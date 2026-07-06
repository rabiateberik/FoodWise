// StockController, kullanıcının stok ürünlerini yönetmesini sağlayan API endpointlerini içerir.
// Stok listeleme, ekleme, güncelleme, silme ve riskli ürünleri görüntüleme işlemleri burada karşılanır.

using System.Security.Claims;
using FoodWise.Application.DTOs.Stock;
using FoodWise.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodWise.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class StockController : ControllerBase
{
    private readonly IStockService _stockService;

    public StockController(IStockService stockService)
    {
        _stockService = stockService;
    }

    // Giriş yapan kullanıcının tüm stok ürünlerini listeler.
    [HttpGet]
    public async Task<IActionResult> GetUserStock()
    {
        var userId = GetUserId();

        var result = await _stockService.GetUserStockAsync(userId);

        return Ok(result);
    }

    // Kullanıcının risk seviyesi yüksek olan stok ürünlerini getirir.
    [HttpGet("risky")]
    public async Task<IActionResult> GetRiskyStockItems()
    {
        var userId = GetUserId();

        var result = await _stockService.GetRiskyStockItemsAsync(userId);

        return Ok(result);
    }

    // Son tüketim tarihi geçmiş stok ürünlerini listeler.
    [HttpGet("expired")]
    public async Task<IActionResult> GetExpiredStockItems()
    {
        var userId = GetUserId();

        var result = await _stockService.GetExpiredStockItemsAsync(userId);

        return Ok(result);
    }

    // Seçilen stok ürününün detayını getirir.
    // Kullanıcı sadece kendi stok ürününe erişebilir.
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var userId = GetUserId();

        var result = await _stockService.GetByIdAsync(id, userId);

        if (result == null)
            return NotFound("Stok ürünü bulunamadı.");

        return Ok(result);
    }

    // Giriş yapan kullanıcı adına yeni stok ürünü oluşturur.
    [HttpPost]
    public async Task<IActionResult> Create(CreateStockItemDto model)
    {
        var userId = GetUserId();

        var result = await _stockService.CreateAsync(userId, model);

        return Ok(result);
    }

    // Kullanıcının kendi stok ürününü günceller.
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, UpdateStockItemDto model)
    {
        var userId = GetUserId();

        var result = await _stockService.UpdateAsync(id, userId, model);

        if (result == null)
            return NotFound("Güncellenecek stok ürünü bulunamadı.");

        return Ok(result);
    }

    // Kullanıcının kendi stok ürününü siler.
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = GetUserId();

        var result = await _stockService.DeleteAsync(id, userId);

        if (!result)
            return NotFound("Silinecek stok ürünü bulunamadı.");

        return Ok("Stok ürünü başarıyla silindi.");
    }

    // JWT token içindeki kullanıcı Id bilgisini alır.
    private string GetUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    }
}


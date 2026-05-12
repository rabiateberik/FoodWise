// StockController, kullanıcının stok ürünlerini yönetmesini sağlayan API endpointlerini içerir.
// Bu controller token ile korunur; sadece giriş yapan kullanıcı kendi stoklarını yönetebilir.
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

    [HttpGet]
    public async Task<IActionResult> GetUserStock()
    {
        // Token içinden giriş yapan kullanıcının Id bilgisi alınır.
        var userId = GetUserId();

        var result = await _stockService.GetUserStockAsync(userId);

        return Ok(result);
    }

    [HttpGet("risky")]
    public async Task<IActionResult> GetRiskyStockItems()
    {
        // Kullanıcının yüksek/kritik riskli stok ürünleri listelenir.
        var userId = GetUserId();

        var result = await _stockService.GetRiskyStockItemsAsync(userId);

        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var userId = GetUserId();

        var result = await _stockService.GetByIdAsync(id, userId);

        if (result == null)
            return NotFound("Stok ürünü bulunamadı.");

        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateStockItemDto model)
    {
        // Yeni stok ürünü giriş yapan kullanıcı adına oluşturulur.
        var userId = GetUserId();

        var result = await _stockService.CreateAsync(userId, model);

        return Ok(result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, UpdateStockItemDto model)
    {
        var userId = GetUserId();

        var result = await _stockService.UpdateAsync(id, userId, model);

        if (result == null)
            return NotFound("Güncellenecek stok ürünü bulunamadı.");

        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = GetUserId();

        var result = await _stockService.DeleteAsync(id, userId);

        if (!result)
            return NotFound("Silinecek stok ürünü bulunamadı.");

        return Ok("Stok ürünü başarıyla silindi.");
    }
    [HttpGet("expired")]
    public async Task<IActionResult> GetExpiredStockItems()
    {
        var userId = GetUserId();

        var result = await _stockService.GetExpiredStockItemsAsync(userId);

        return Ok(result);
    }
    private string GetUserId()
    {
        // JWT token içindeki NameIdentifier claim'i Identity kullanıcı Id bilgisini taşır.
        return User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    }
}
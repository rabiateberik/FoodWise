// AdminController, admin paneli için gerekli API endpointlerini içerir.
// Bu endpointlere sadece Admin rolüne sahip kullanıcılar erişebilir.

using FoodWise.Application.DTOs.Admin;
using FoodWise.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodWise.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IAdminService _adminService;

    public AdminController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboardSummary()
    {
        var result = await _adminService.GetDashboardSummaryAsync();

        return Ok(result);
    }
    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories()
    {
        var result = await _adminService.GetCategoriesAsync();

        return Ok(result);
    }

    [HttpGet("categories/{id}")]
    public async Task<IActionResult> GetCategoryById(int id)
    {
        var result = await _adminService.GetCategoryByIdAsync(id);

        if (result == null)
            return NotFound("Kategori bulunamadı.");

        return Ok(result);
    }

    [HttpPost("categories")]
    public async Task<IActionResult> CreateCategory(CreateAdminCategoryDto model)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _adminService.CreateCategoryAsync(model);

        if (result == null)
            return BadRequest("Bu kategori adı zaten kullanılıyor.");

        return Ok(result);
    }

    [HttpPut("categories/{id}")]
    public async Task<IActionResult> UpdateCategory(int id, UpdateAdminCategoryDto model)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _adminService.UpdateCategoryAsync(id, model);

        if (result == null)
            return BadRequest("Kategori bulunamadı veya bu kategori adı zaten kullanılıyor.");

        return Ok(result);
    }

    [HttpPatch("categories/{id}/toggle-status")]
    public async Task<IActionResult> ToggleCategoryStatus(int id)
    {
        var result = await _adminService.ToggleCategoryStatusAsync(id);

        if (!result)
            return NotFound("Kategori bulunamadı.");

        return Ok(new
        {
            message = "Kategori durumu güncellendi."
        });
    }
    [HttpGet("products")]
    public async Task<IActionResult> GetProducts()
    {
        var result = await _adminService.GetProductsAsync();

        return Ok(result);
    }

    [HttpGet("products/{id}")]
    public async Task<IActionResult> GetProductById(int id)
    {
        var result = await _adminService.GetProductByIdAsync(id);

        if (result == null)
            return NotFound("Ürün bulunamadı.");

        return Ok(result);
    }

    [HttpPost("products")]
    public async Task<IActionResult> CreateProduct(CreateAdminProductDto model)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _adminService.CreateProductAsync(model);

        if (result == null)
            return BadRequest("Ürün eklenemedi. Aynı isimde ürün olabilir veya kategori pasif olabilir.");

        return Ok(result);
    }

    [HttpPut("products/{id}")]
    public async Task<IActionResult> UpdateProduct(int id, UpdateAdminProductDto model)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _adminService.UpdateProductAsync(id, model);

        if (result == null)
            return BadRequest("Ürün güncellenemedi. Aynı isimde ürün olabilir veya kategori pasif olabilir.");

        return Ok(result);
    }

    [HttpPatch("products/{id}/toggle-status")]
    public async Task<IActionResult> ToggleProductStatus(int id)
    {
        var result = await _adminService.ToggleProductStatusAsync(id);

        if (!result)
            return NotFound("Ürün bulunamadı.");

        return Ok(new
        {
            message = "Ürün aktif/pasif durumu güncellendi."
        });
    }

    [HttpPatch("products/{id}/toggle-approval")]
    public async Task<IActionResult> ToggleProductApproval(int id)
    {
        var result = await _adminService.ToggleProductApprovalAsync(id);

        if (!result)
            return NotFound("Ürün bulunamadı.");

        return Ok(new
        {
            message = "Ürün onay durumu güncellendi."
        });
    }
    //teslimat noktası yönetimi için gerekli endpointler
    [HttpGet("delivery-points")]
    public async Task<IActionResult> GetDeliveryPoints()
    {
        var result = await _adminService.GetDeliveryPointsAsync();

        return Ok(result);
    }

    [HttpGet("delivery-points/{id}")]
    public async Task<IActionResult> GetDeliveryPointById(int id)
    {
        var result = await _adminService.GetDeliveryPointByIdAsync(id);

        if (result == null)
            return NotFound("Teslimat noktası bulunamadı.");

        return Ok(result);
    }

    [HttpPost("delivery-points")]
    public async Task<IActionResult> CreateDeliveryPoint(CreateAdminDeliveryPointDto model)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _adminService.CreateDeliveryPointAsync(model);

        if (result == null)
            return BadRequest("Teslimat noktası eklenemedi. Aynı isimde kayıt olabilir.");

        return Ok(result);
    }

    [HttpPut("delivery-points/{id}")]
    public async Task<IActionResult> UpdateDeliveryPoint(int id, UpdateAdminDeliveryPointDto model)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _adminService.UpdateDeliveryPointAsync(id, model);

        if (result == null)
            return BadRequest("Teslimat noktası güncellenemedi. Aynı isimde kayıt olabilir.");

        return Ok(result);
    }
    //Teslimat noktası aktif/pasif durumunu değiştirmek için kullanılır.
    [HttpPatch("delivery-points/{id}/toggle-status")]
    public async Task<IActionResult> ToggleDeliveryPointStatus(int id)
    {
        var result = await _adminService.ToggleDeliveryPointStatusAsync(id);

        if (!result)
            return NotFound("Teslimat noktası bulunamadı.");

        return Ok(new
        {
            message = "Teslimat noktası aktif/pasif durumu güncellendi."
        });
    }
    [HttpGet("users")]
    public async Task<IActionResult> GetUsers()
    {
        var result = await _adminService.GetUsersAsync();

        return Ok(result);
    }

    [HttpGet("users/{id}")]
    public async Task<IActionResult> GetUserById(string id)
    {
        var result = await _adminService.GetUserByIdAsync(id);

        if (result == null)
            return NotFound("Kullanıcı bulunamadı.");

        return Ok(result);
    }

    [HttpPatch("users/{id}/toggle-status")]
    public async Task<IActionResult> ToggleUserStatus(string id)
    {
        var result = await _adminService.ToggleUserStatusAsync(id);

        if (!result)
            return BadRequest("Kullanıcı durumu güncellenemedi. Admin hesabı pasifleştirilemez.");

        return Ok(new
        {
            message = "Kullanıcı aktif/pasif durumu güncellendi."
        });
    }
    [HttpGet("users/{id}/stocks")]
    public async Task<IActionResult> GetUserStocks(string id)
    {
        var result = await _adminService.GetUserStocksAsync(id);

        return Ok(result);
    }

    [HttpGet("users/{id}/share-listings")]
    public async Task<IActionResult> GetUserShareListings(string id)
    {
        var result = await _adminService.GetUserShareListingsAsync(id);

        return Ok(result);
    }

    [HttpGet("users/{id}/deliveries")]
    public async Task<IActionResult> GetUserDeliveries(string id)
    {
        var result = await _adminService.GetUserDeliveriesAsync(id);

        return Ok(result);
    }
}
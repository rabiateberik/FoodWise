// AdminController, yönetim panelindeki işlemler için API endpointlerini içerir.
// Bu controller sadece Admin rolüne sahip kullanıcılar tarafından kullanılabilir.

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

    // Admin panelinde gösterilecek genel özet bilgileri getirir.
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboardSummary()
    {
        var result = await _adminService.GetDashboardSummaryAsync();

        return Ok(result);
    }

    // Sistemde kayıtlı ürün kategorilerini listeler.
    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories()
    {
        var result = await _adminService.GetCategoriesAsync();

        return Ok(result);
    }

    // Seçilen kategoriye ait detay bilgilerini getirir.
    [HttpGet("categories/{id}")]
    public async Task<IActionResult> GetCategoryById(int id)
    {
        var result = await _adminService.GetCategoryByIdAsync(id);

        if (result == null)
            return NotFound("Kategori bulunamadı.");

        return Ok(result);
    }

    // Yeni kategori oluşturur. Aynı isimde kategori varsa servis null döndürür.
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

    // Mevcut kategori bilgilerini günceller.
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

    // Kategorinin aktif/pasif durumunu değiştirir.
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

    // Ürün yönetimi için ürünleri listeler.
    [HttpGet("products")]
    public async Task<IActionResult> GetProducts()
    {
        var result = await _adminService.GetProductsAsync();

        return Ok(result);
    }

    // Seçilen ürünün detay bilgilerini getirir.
    [HttpGet("products/{id}")]
    public async Task<IActionResult> GetProductById(int id)
    {
        var result = await _adminService.GetProductByIdAsync(id);

        if (result == null)
            return NotFound("Ürün bulunamadı.");

        return Ok(result);
    }

    // Yeni ürün ekler. Ürün adı veya kategori durumu servis tarafında kontrol edilir.
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

    // Mevcut ürün bilgilerini günceller.
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

    // Ürünün sistemde aktif veya pasif olma durumunu değiştirir.
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

    // Kullanıcıların eklediği ürünlerin onay durumunu değiştirir.
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

    // Teslimat noktalarını yönetmek için kullanılan endpointler.
    [HttpGet("delivery-points")]
    public async Task<IActionResult> GetDeliveryPoints()
    {
        var result = await _adminService.GetDeliveryPointsAsync();

        return Ok(result);
    }

    // Seçilen teslimat noktasının detayını getirir.
    [HttpGet("delivery-points/{id}")]
    public async Task<IActionResult> GetDeliveryPointById(int id)
    {
        var result = await _adminService.GetDeliveryPointByIdAsync(id);

        if (result == null)
            return NotFound("Teslimat noktası bulunamadı.");

        return Ok(result);
    }

    // Yeni teslimat noktası ekler.
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

    // Teslimat noktası bilgilerini günceller.
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

    // Teslimat noktasının aktif/pasif durumunu değiştirir.
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

    // Sistemdeki kullanıcıları listeler.
    [HttpGet("users")]
    public async Task<IActionResult> GetUsers()
    {
        var result = await _adminService.GetUsersAsync();

        return Ok(result);
    }

    // Seçilen kullanıcının detay bilgilerini getirir.
    [HttpGet("users/{id}")]
    public async Task<IActionResult> GetUserById(string id)
    {
        var result = await _adminService.GetUserByIdAsync(id);

        if (result == null)
            return NotFound("Kullanıcı bulunamadı.");

        return Ok(result);
    }

    // Kullanıcı hesabını aktif veya pasif hale getirir.
    // Admin hesabının pasifleştirilmesi servis tarafında engellenir.
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

    // Kullanıcının stok kayıtlarını admin paneli için getirir.
    [HttpGet("users/{id}/stocks")]
    public async Task<IActionResult> GetUserStocks(string id)
    {
        var result = await _adminService.GetUserStocksAsync(id);

        return Ok(result);
    }

    // Kullanıcının oluşturduğu paylaşım ilanlarını getirir.
    [HttpGet("users/{id}/share-listings")]
    public async Task<IActionResult> GetUserShareListings(string id)
    {
        var result = await _adminService.GetUserShareListingsAsync(id);

        return Ok(result);
    }

    // Kullanıcının teslimat geçmişini getirir.
    [HttpGet("users/{id}/deliveries")]
    public async Task<IActionResult> GetUserDeliveries(string id)
    {
        var result = await _adminService.GetUserDeliveriesAsync(id);

        return Ok(result);
    }

    // Tüm paylaşım ilanlarını admin paneli için listeler.
    [HttpGet("share-listings")]
    public async Task<IActionResult> GetShareListings()
    {
        var result = await _adminService.GetShareListingsAsync();

        return Ok(result);
    }

    // Sistemdeki teslimat kayıtlarını listeler.
    [HttpGet("deliveries")]
    public async Task<IActionResult> GetDeliveries()
    {
        var result = await _adminService.GetDeliveriesAsync();

        return Ok(result);
    }
}


// AdminController, FoodWise admin paneli giriş ve dashboard işlemlerini yönetir.
// Admin paneline sadece Admin rolüne sahip kullanıcılar erişebilir.

using FoodWise.Web.Helpers;
using FoodWise.Web.Services;
using FoodWise.Web.ViewModels.Admin;
using FoodWise.Web.ViewModels.Auth;
using Microsoft.AspNetCore.Mvc;

namespace FoodWise.Web.Controllers;

public class AdminController : Controller
{
    private readonly IAuthWebService _authWebService;
    private readonly IAdminWebService _adminWebService;

    public AdminController(
        IAuthWebService authWebService,
        IAdminWebService adminWebService)
    {
        _authWebService = authWebService;
        _adminWebService = adminWebService;
    }

    [HttpGet]
    public IActionResult Login()
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (AdminAuthHelper.IsAdmin(token))
            return RedirectToAction(nameof(Dashboard));

        return View(new LoginViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var response = await _authWebService.LoginAsync(model);

        if (response == null || !response.Success || string.IsNullOrWhiteSpace(response.Token))
        {
            ViewBag.ErrorMessage = response?.Message ?? "Giriş işlemi başarısız.";
            return View(model);
        }

        if (!AdminAuthHelper.IsAdmin(response.Token))
        {
            ViewBag.ErrorMessage = "Bu alana erişim yetkiniz bulunmamaktadır.";
            return View(model);
        }

        HttpContext.Session.SetString("JWToken", response.Token);
        HttpContext.Session.SetString("FullName", response.FullName ?? "Admin");
        HttpContext.Session.SetString("Email", response.Email ?? model.Email);

        return RedirectToAction(nameof(Dashboard));
    }

    [HttpGet]
    public async Task<IActionResult> Dashboard()
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (!AdminAuthHelper.IsAdmin(token))
            return RedirectToAction(nameof(Login));

        var dashboardSummary = await _adminWebService.GetDashboardSummaryAsync(token!);

        if (dashboardSummary == null)
        {
            TempData["ErrorMessage"] = "Admin dashboard verileri alınamadı.";
            return View(new AdminDashboardViewModel());
        }

        ViewData["Title"] = "Admin Paneli";

        return View(dashboardSummary);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();

        return RedirectToAction(nameof(Login));
    }
    [HttpGet]
    public async Task<IActionResult> Categories()
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (!AdminAuthHelper.IsAdmin(token))
            return RedirectToAction(nameof(Login));

        var categories = await _adminWebService.GetCategoriesAsync(token!);

        return View(categories);
    }

    [HttpGet]
    public IActionResult CreateCategory()
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (!AdminAuthHelper.IsAdmin(token))
            return RedirectToAction(nameof(Login));

        return View(new CreateAdminCategoryViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCategory(CreateAdminCategoryViewModel model)
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (!AdminAuthHelper.IsAdmin(token))
            return RedirectToAction(nameof(Login));

        if (!ModelState.IsValid)
            return View(model);

        var result = await _adminWebService.CreateCategoryAsync(model, token!);

        TempData[result ? "SuccessMessage" : "ErrorMessage"] = result
            ? "Kategori başarıyla eklendi."
            : "Kategori eklenemedi. Aynı isimde kategori olabilir.";

        return result
            ? RedirectToAction(nameof(Categories))
            : View(model);
    }

    [HttpGet]
    public async Task<IActionResult> EditCategory(int id)
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (!AdminAuthHelper.IsAdmin(token))
            return RedirectToAction(nameof(Login));

        var category = await _adminWebService.GetCategoryByIdAsync(id, token!);

        if (category == null)
        {
            TempData["ErrorMessage"] = "Kategori bulunamadı.";
            return RedirectToAction(nameof(Categories));
        }

        var model = new UpdateAdminCategoryViewModel
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditCategory(UpdateAdminCategoryViewModel model)
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (!AdminAuthHelper.IsAdmin(token))
            return RedirectToAction(nameof(Login));

        if (!ModelState.IsValid)
            return View(model);

        var result = await _adminWebService.UpdateCategoryAsync(model, token!);

        TempData[result ? "SuccessMessage" : "ErrorMessage"] = result
            ? "Kategori başarıyla güncellendi."
            : "Kategori güncellenemedi. Aynı isimde kategori olabilir.";

        return result
            ? RedirectToAction(nameof(Categories))
            : View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleCategoryStatus(int id)
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (!AdminAuthHelper.IsAdmin(token))
            return RedirectToAction(nameof(Login));

        var result = await _adminWebService.ToggleCategoryStatusAsync(id, token!);

        TempData[result ? "SuccessMessage" : "ErrorMessage"] = result
            ? "Kategori durumu güncellendi."
            : "Kategori durumu güncellenemedi.";

        return RedirectToAction(nameof(Categories));
    }

    [HttpGet]
    public async Task<IActionResult> Products()
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (!AdminAuthHelper.IsAdmin(token))
            return RedirectToAction(nameof(Login));

        var products = await _adminWebService.GetProductsAsync(token!);

        return View(products);
    }

    [HttpGet]
    public async Task<IActionResult> CreateProduct()
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (!AdminAuthHelper.IsAdmin(token))
            return RedirectToAction(nameof(Login));

        var model = new CreateAdminProductViewModel
        {
            CategoryOptions = await GetActiveCategoryOptionsAsync(token!)
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateProduct(CreateAdminProductViewModel model)
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (!AdminAuthHelper.IsAdmin(token))
            return RedirectToAction(nameof(Login));

        if (!ModelState.IsValid)
        {
            model.CategoryOptions = await GetActiveCategoryOptionsAsync(token!);
            return View(model);
        }

        var result = await _adminWebService.CreateProductAsync(model, token!);

        TempData[result ? "SuccessMessage" : "ErrorMessage"] = result
            ? "Ürün başarıyla eklendi."
            : "Ürün eklenemedi. Aynı isimde ürün olabilir veya kategori pasif olabilir.";

        if (!result)
        {
            model.CategoryOptions = await GetActiveCategoryOptionsAsync(token!);
            return View(model);
        }

        return RedirectToAction(nameof(Products));
    }

    [HttpGet]
    public async Task<IActionResult> EditProduct(int id)
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (!AdminAuthHelper.IsAdmin(token))
            return RedirectToAction(nameof(Login));

        var product = await _adminWebService.GetProductByIdAsync(id, token!);

        if (product == null)
        {
            TempData["ErrorMessage"] = "Ürün bulunamadı.";
            return RedirectToAction(nameof(Products));
        }

        var model = new UpdateAdminProductViewModel
        {
            Id = product.Id,
            CategoryId = product.CategoryId,
            Name = product.Name,
            DefaultShelfLifeDays = product.DefaultShelfLifeDays,
            OpenedShelfLifeDays = product.OpenedShelfLifeDays,
            CarbonFactor = product.CarbonFactor,
            IsSensitiveFood = product.IsSensitiveFood,
            IsApproved = product.IsApproved,
            CategoryOptions = await GetActiveCategoryOptionsAsync(token!, product.CategoryId)
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditProduct(UpdateAdminProductViewModel model)
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (!AdminAuthHelper.IsAdmin(token))
            return RedirectToAction(nameof(Login));

        if (!ModelState.IsValid)
        {
            model.CategoryOptions = await GetActiveCategoryOptionsAsync(token!, model.CategoryId);
            return View(model);
        }

        var result = await _adminWebService.UpdateProductAsync(model, token!);

        TempData[result ? "SuccessMessage" : "ErrorMessage"] = result
            ? "Ürün başarıyla güncellendi."
            : "Ürün güncellenemedi. Aynı isimde ürün olabilir veya kategori pasif olabilir.";

        if (!result)
        {
            model.CategoryOptions = await GetActiveCategoryOptionsAsync(token!, model.CategoryId);
            return View(model);
        }

        return RedirectToAction(nameof(Products));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleProductStatus(int id)
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (!AdminAuthHelper.IsAdmin(token))
            return RedirectToAction(nameof(Login));

        var result = await _adminWebService.ToggleProductStatusAsync(id, token!);

        TempData[result ? "SuccessMessage" : "ErrorMessage"] = result
            ? "Ürün aktif/pasif durumu güncellendi."
            : "Ürün durumu güncellenemedi.";

        return RedirectToAction(nameof(Products));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleProductApproval(int id)
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (!AdminAuthHelper.IsAdmin(token))
            return RedirectToAction(nameof(Login));

        var result = await _adminWebService.ToggleProductApprovalAsync(id, token!);

        TempData[result ? "SuccessMessage" : "ErrorMessage"] = result
            ? "Ürün onay durumu güncellendi."
            : "Ürün onay durumu güncellenemedi.";

        return RedirectToAction(nameof(Products));
    }

    private async Task<List<AdminCategoryViewModel>> GetActiveCategoryOptionsAsync(
        string token,
        int? selectedCategoryId = null)
    {
        // Ürün formunda sadece aktif kategoriler gösterilir.
        // Düzenlenen ürün pasif kategoriye bağlıysa mevcut kategorisi de listede tutulur.
        var categories = await _adminWebService.GetCategoriesAsync(token);

        return categories
            .Where(x => x.IsActive || x.Id == selectedCategoryId)
            .OrderBy(x => x.Name)
            .ToList();
    }
    // DeliveryPoint yönetimi için gerekli actionlar
    [HttpGet]
    public async Task<IActionResult> DeliveryPoints()
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (!AdminAuthHelper.IsAdmin(token))
            return RedirectToAction(nameof(Login));

        var deliveryPoints = await _adminWebService.GetDeliveryPointsAsync(token!);

        return View(deliveryPoints);
    }

    [HttpGet]
    public IActionResult CreateDeliveryPoint()
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (!AdminAuthHelper.IsAdmin(token))
            return RedirectToAction(nameof(Login));

        return View(new CreateAdminDeliveryPointViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateDeliveryPoint(CreateAdminDeliveryPointViewModel model)
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (!AdminAuthHelper.IsAdmin(token))
            return RedirectToAction(nameof(Login));

        if (!ModelState.IsValid)
            return View(model);

        var result = await _adminWebService.CreateDeliveryPointAsync(model, token!);

        TempData[result ? "SuccessMessage" : "ErrorMessage"] = result
            ? "Teslimat noktası başarıyla eklendi."
            : "Teslimat noktası eklenemedi. Aynı isimde kayıt olabilir.";

        return result
            ? RedirectToAction(nameof(DeliveryPoints))
            : View(model);
    }

    [HttpGet]
    public async Task<IActionResult> EditDeliveryPoint(int id)
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (!AdminAuthHelper.IsAdmin(token))
            return RedirectToAction(nameof(Login));

        var deliveryPoint = await _adminWebService.GetDeliveryPointByIdAsync(id, token!);

        if (deliveryPoint == null)
        {
            TempData["ErrorMessage"] = "Teslimat noktası bulunamadı.";
            return RedirectToAction(nameof(DeliveryPoints));
        }

        var model = new UpdateAdminDeliveryPointViewModel
        {
            Id = deliveryPoint.Id,
            Name = deliveryPoint.Name,
            Description = deliveryPoint.Description,
            City = deliveryPoint.City ?? string.Empty,
            District = deliveryPoint.District ?? string.Empty,
            Neighborhood = deliveryPoint.Neighborhood,
            Latitude = deliveryPoint.Latitude,
            Longitude = deliveryPoint.Longitude,
            WorkingHours = deliveryPoint.WorkingHours,
            StorageType = deliveryPoint.StorageType
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditDeliveryPoint(UpdateAdminDeliveryPointViewModel model)
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (!AdminAuthHelper.IsAdmin(token))
            return RedirectToAction(nameof(Login));

        if (!ModelState.IsValid)
            return View(model);

        var result = await _adminWebService.UpdateDeliveryPointAsync(model, token!);

        TempData[result ? "SuccessMessage" : "ErrorMessage"] = result
            ? "Teslimat noktası başarıyla güncellendi."
            : "Teslimat noktası güncellenemedi. Aynı isimde kayıt olabilir.";

        return result
            ? RedirectToAction(nameof(DeliveryPoints))
            : View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleDeliveryPointStatus(int id)
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (!AdminAuthHelper.IsAdmin(token))
            return RedirectToAction(nameof(Login));

        var result = await _adminWebService.ToggleDeliveryPointStatusAsync(id, token!);

        TempData[result ? "SuccessMessage" : "ErrorMessage"] = result
            ? "Teslimat noktası aktif/pasif durumu güncellendi."
            : "Teslimat noktası durumu güncellenemedi.";

        return RedirectToAction(nameof(DeliveryPoints));
    }

    [HttpGet]
    public async Task<IActionResult> DeliveryBoxes(int? deliveryPointId)
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (!AdminAuthHelper.IsAdmin(token))
            return RedirectToAction(nameof(Login));

        var deliveryBoxes = await _adminWebService.GetDeliveryBoxesAsync(token!, deliveryPointId);

        ViewBag.DeliveryPointId = deliveryPointId;

        return View(deliveryBoxes);
    }

    [HttpGet]
    public async Task<IActionResult> CreateDeliveryBox(int? deliveryPointId)
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (!AdminAuthHelper.IsAdmin(token))
            return RedirectToAction(nameof(Login));

        var model = new CreateAdminDeliveryBoxViewModel
        {
            DeliveryPointId = deliveryPointId ?? 0,
            DeliveryPointOptions = await GetActiveDeliveryPointOptionsAsync(token!, deliveryPointId)
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateDeliveryBox(CreateAdminDeliveryBoxViewModel model)
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (!AdminAuthHelper.IsAdmin(token))
            return RedirectToAction(nameof(Login));

        if (!ModelState.IsValid)
        {
            model.DeliveryPointOptions = await GetActiveDeliveryPointOptionsAsync(token!, model.DeliveryPointId);
            return View(model);
        }

        var result = await _adminWebService.CreateDeliveryBoxAsync(model, token!);

        TempData[result ? "SuccessMessage" : "ErrorMessage"] = result
            ? "Teslim kutusu başarıyla eklendi."
            : "Teslim kutusu eklenemedi. Aynı kutu kodu veya QR değeri kullanılıyor olabilir.";

        if (!result)
        {
            model.DeliveryPointOptions = await GetActiveDeliveryPointOptionsAsync(token!, model.DeliveryPointId);
            return View(model);
        }

        return RedirectToAction(nameof(DeliveryBoxes), new { deliveryPointId = model.DeliveryPointId });
    }

    [HttpGet]
    public async Task<IActionResult> EditDeliveryBox(int id)
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (!AdminAuthHelper.IsAdmin(token))
            return RedirectToAction(nameof(Login));

        var deliveryBox = await _adminWebService.GetDeliveryBoxByIdAsync(id, token!);

        if (deliveryBox == null)
        {
            TempData["ErrorMessage"] = "Teslim kutusu bulunamadı.";
            return RedirectToAction(nameof(DeliveryBoxes));
        }

        var model = new UpdateAdminDeliveryBoxViewModel
        {
            Id = deliveryBox.Id,
            DeliveryPointId = deliveryBox.DeliveryPointId,
            BoxCode = deliveryBox.BoxCode,
            QrCodeValue = deliveryBox.QrCodeValue,
            Description = deliveryBox.Description,
            DeliveryPointOptions = await GetActiveDeliveryPointOptionsAsync(token!, deliveryBox.DeliveryPointId)
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditDeliveryBox(UpdateAdminDeliveryBoxViewModel model)
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (!AdminAuthHelper.IsAdmin(token))
            return RedirectToAction(nameof(Login));

        if (!ModelState.IsValid)
        {
            model.DeliveryPointOptions = await GetActiveDeliveryPointOptionsAsync(token!, model.DeliveryPointId);
            return View(model);
        }

        var result = await _adminWebService.UpdateDeliveryBoxAsync(model, token!);

        TempData[result ? "SuccessMessage" : "ErrorMessage"] = result
            ? "Teslim kutusu başarıyla güncellendi."
            : "Teslim kutusu güncellenemedi. Aynı kutu kodu veya QR değeri kullanılıyor olabilir.";

        if (!result)
        {
            model.DeliveryPointOptions = await GetActiveDeliveryPointOptionsAsync(token!, model.DeliveryPointId);
            return View(model);
        }

        return RedirectToAction(nameof(DeliveryBoxes), new { deliveryPointId = model.DeliveryPointId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleDeliveryBoxStatus(int id, int? deliveryPointId)
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (!AdminAuthHelper.IsAdmin(token))
            return RedirectToAction(nameof(Login));

        var result = await _adminWebService.ToggleDeliveryBoxStatusAsync(id, token!);

        TempData[result ? "SuccessMessage" : "ErrorMessage"] = result
            ? "Teslim kutusu aktif/pasif durumu güncellendi."
            : "Teslim kutusu dolu olduğu için pasifleştirilemez veya kayıt bulunamadı.";

        return RedirectToAction(nameof(DeliveryBoxes), new { deliveryPointId });
    }

    private async Task<List<AdminDeliveryPointViewModel>> GetActiveDeliveryPointOptionsAsync(
        string token,
        int? selectedDeliveryPointId = null)
    {
        // Kutu formunda sadece aktif teslim noktaları gösterilir.
        // Düzenlenen kutu pasif noktaya bağlıysa mevcut noktası da listede tutulur.
        var deliveryPoints = await _adminWebService.GetDeliveryPointsAsync(token);

        return deliveryPoints
            .Where(x => x.IsActive || x.Id == selectedDeliveryPointId)
            .OrderBy(x => x.City)
            .ThenBy(x => x.District)
            .ThenBy(x => x.Name)
            .ToList();
    }
    [HttpGet]
    public async Task<IActionResult> Users()
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (!AdminAuthHelper.IsAdmin(token))
            return RedirectToAction(nameof(Login));

        var users = await _adminWebService.GetUsersAsync(token!);

        return View(users);
    }

    [HttpGet]
    public async Task<IActionResult> UserDetails(string id)
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (!AdminAuthHelper.IsAdmin(token))
            return RedirectToAction(nameof(Login));

        var user = await _adminWebService.GetUserByIdAsync(id, token!);

        if (user == null)
        {
            TempData["ErrorMessage"] = "Kullanıcı bulunamadı.";
            return RedirectToAction(nameof(Users));
        }

        return View(user);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleUserStatus(string id)
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (!AdminAuthHelper.IsAdmin(token))
            return RedirectToAction(nameof(Login));

        var result = await _adminWebService.ToggleUserStatusAsync(id, token!);

        TempData[result ? "SuccessMessage" : "ErrorMessage"] = result
            ? "Kullanıcı aktif/pasif durumu güncellendi."
            : "Kullanıcı durumu güncellenemedi. Admin hesabı pasifleştirilemez.";

        return RedirectToAction(nameof(Users));
    }
    [HttpGet]
    public async Task<IActionResult> UserStocks(string id)
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (!AdminAuthHelper.IsAdmin(token))
            return RedirectToAction(nameof(Login));

        var stocks = await _adminWebService.GetUserStocksAsync(id, token!);

        ViewBag.UserId = id;

        return View(stocks);
    }

    [HttpGet]
    public async Task<IActionResult> UserShareListings(string id)
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (!AdminAuthHelper.IsAdmin(token))
            return RedirectToAction(nameof(Login));

        var listings = await _adminWebService.GetUserShareListingsAsync(id, token!);

        ViewBag.UserId = id;

        return View(listings);
    }

    [HttpGet]
    public async Task<IActionResult> UserDeliveries(string id)
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (!AdminAuthHelper.IsAdmin(token))
            return RedirectToAction(nameof(Login));

        var deliveries = await _adminWebService.GetUserDeliveriesAsync(id, token!);

        ViewBag.UserId = id;

        return View(deliveries);
    }
}
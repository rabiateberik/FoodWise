
// AdminController, FoodWise admin panelindeki sayfa işlemlerini yönetir.
// Admin girişi, dashboard, kategori, ürün, teslim noktası, teslim kutusu,
// kullanıcı ve paylaşım/teslimat izleme ekranları bu controller üzerinden çalışır.
// Controller doğrudan API'ye gitmez; API işlemleri AdminWebService üzerinden yapılır.

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

    // Admin giriş sayfasını açar.
    // Eğer Session içinde geçerli admin token varsa kullanıcı doğrudan dashboard sayfasına yönlendirilir.
    [HttpGet]
    public IActionResult Login()
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (AdminAuthHelper.IsAdmin(token))
            return RedirectToAction(nameof(Dashboard));

        return View(new LoginViewModel());
    }

    // Admin giriş formundan gelen bilgileri API'ye gönderir.
    // Giriş başarılı olsa bile token içindeki rol Admin değilse panele erişim verilmez.
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

        // Token içindeki rol bilgisi kontrol edilerek sadece Admin kullanıcıların giriş yapması sağlanır.
        if (!AdminAuthHelper.IsAdmin(response.Token))
        {
            ViewBag.ErrorMessage = "Bu alana erişim yetkiniz bulunmamaktadır.";
            return View(model);
        }

        // Admin bilgileri Session içine kaydedilir.
        HttpContext.Session.SetString("JWToken", response.Token);
        HttpContext.Session.SetString("FullName", response.FullName ?? "Admin");
        HttpContext.Session.SetString("Email", response.Email ?? model.Email);

        return RedirectToAction(nameof(Dashboard));
    }

    // Admin panelinin ana dashboard sayfasını açar.
    // Dashboard özet verileri Admin API üzerinden alınır.
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

    // Admin oturumunu kapatır ve Session içindeki bilgileri temizler.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();

        return RedirectToAction(nameof(Login));
    }

    // Kategori listesini admin panelinde gösterir.
    [HttpGet]
    public async Task<IActionResult> Categories()
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (!AdminAuthHelper.IsAdmin(token))
            return RedirectToAction(nameof(Login));

        var categories = await _adminWebService.GetCategoriesAsync(token!);

        return View(categories);
    }

    // Yeni kategori oluşturma formunu açar.
    [HttpGet]
    public IActionResult CreateCategory()
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (!AdminAuthHelper.IsAdmin(token))
            return RedirectToAction(nameof(Login));

        return View(new CreateAdminCategoryViewModel());
    }

    // Yeni kategori oluşturma isteğini Admin API'ye gönderir.
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

    // Kategori düzenleme formunu mevcut kategori bilgileriyle açar.
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

    // Kategori güncelleme isteğini Admin API'ye gönderir.
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

    // Kategorinin aktif/pasif durumunu değiştirir.
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

    // Ürün listesini admin panelinde gösterir.
    [HttpGet]
    public async Task<IActionResult> Products()
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (!AdminAuthHelper.IsAdmin(token))
            return RedirectToAction(nameof(Login));

        var products = await _adminWebService.GetProductsAsync(token!);

        return View(products);
    }

    // Yeni ürün oluşturma formunu açar.
    // Formdaki kategori seçenekleri API'den alınan aktif kategorilerle doldurulur.
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

    // Yeni ürün oluşturma isteğini Admin API'ye gönderir.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateProduct(CreateAdminProductViewModel model)
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (!AdminAuthHelper.IsAdmin(token))
            return RedirectToAction(nameof(Login));

        if (!ModelState.IsValid)
        {
            // Form tekrar açılırken kategori dropdown listesinin boş kalmaması için seçenekler yeniden yüklenir.
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

    // Ürün düzenleme formunu mevcut ürün bilgileriyle açar.
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

    // Ürün güncelleme isteğini Admin API'ye gönderir.
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

    // Ürünün aktif/pasif durumunu değiştirir.
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

    // Ürünün admin onay durumunu değiştirir.
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

    // Ürün oluşturma ve düzenleme formlarında kullanılacak kategori listesini hazırlar.
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

    // Teslim noktalarını admin panelinde listeler.
    [HttpGet]
    public async Task<IActionResult> DeliveryPoints()
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (!AdminAuthHelper.IsAdmin(token))
            return RedirectToAction(nameof(Login));

        var deliveryPoints = await _adminWebService.GetDeliveryPointsAsync(token!);

        return View(deliveryPoints);
    }

    // Yeni teslim noktası oluşturma formunu açar.
    [HttpGet]
    public IActionResult CreateDeliveryPoint()
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (!AdminAuthHelper.IsAdmin(token))
            return RedirectToAction(nameof(Login));

        return View(new CreateAdminDeliveryPointViewModel());
    }

    // Yeni teslim noktası oluşturma isteğini Admin API'ye gönderir.
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

    // Teslim noktası düzenleme formunu mevcut bilgilerle açar.
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

    // Teslim noktası güncelleme isteğini Admin API'ye gönderir.
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

    // Teslim noktasının aktif/pasif durumunu değiştirir.
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

    // Teslim kutularını listeler.
    // deliveryPointId gelirse sadece seçilen teslim noktasına ait kutular gösterilir.
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

    // Yeni teslim kutusu oluşturma formunu açar.
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

    // Yeni teslim kutusu oluşturma isteğini Admin API'ye gönderir.
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

    // Teslim kutusu düzenleme formunu mevcut kutu bilgileriyle açar.
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

    // Teslim kutusu güncelleme isteğini Admin API'ye gönderir.
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

    // Teslim kutusunun aktif/pasif durumunu değiştirir.
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

    // Teslim kutusu formlarında kullanılacak teslim noktası listesini hazırlar.
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

    // Sistemdeki kullanıcıları admin panelinde listeler.
    [HttpGet]
    public async Task<IActionResult> Users()
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (!AdminAuthHelper.IsAdmin(token))
            return RedirectToAction(nameof(Login));

        var users = await _adminWebService.GetUsersAsync(token!);

        return View(users);
    }

    // Seçilen kullanıcının detay bilgilerini gösterir.
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

    // Kullanıcının aktif/pasif durumunu değiştirir.
    // Backend tarafında admin hesabının pasifleştirilmesi engellenir.
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

    // Seçilen kullanıcının stok ürünlerini admin panelinde gösterir.
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

    // Seçilen kullanıcının paylaşım ilanlarını admin panelinde gösterir.
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

    // Seçilen kullanıcının teslimat kayıtlarını admin panelinde gösterir.
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

    // Sistemdeki paylaşım ilanlarını admin panelinde izlemek için listeler.
    [HttpGet]
    public async Task<IActionResult> ShareListings()
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (!AdminAuthHelper.IsAdmin(token))
            return RedirectToAction(nameof(Login));

        var listings = await _adminWebService.GetShareListingsAsync(token!);

        return View(listings);
    }

    // Sistemdeki teslimat kayıtlarını admin panelinde izlemek için listeler.
    [HttpGet]
    public async Task<IActionResult> Deliveries()
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (!AdminAuthHelper.IsAdmin(token))
            return RedirectToAction(nameof(Login));

        var deliveries = await _adminWebService.GetDeliveriesAsync(token!);

        return View(deliveries);
    }
}


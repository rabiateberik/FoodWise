
// StockController, Web arayüzünde stok yönetimi sayfalarını kontrol eder.
// Kullanıcı stoklarını listeleme, riskli/geçmiş ürünleri gösterme,
// stok ekleme, düzenleme ve silme işlemleri bu controller üzerinden yapılır.
// Controller API'ye doğrudan gitmez; tüm stok işlemleri IStockWebService üzerinden FoodWise.API'ye gönderilir.

using FoodWise.Web.Services;
using FoodWise.Web.ViewModels.Stock;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FoodWise.Web.Controllers;

public class StockController : Controller
{
    private readonly IStockWebService _stockWebService;

    public StockController(IStockWebService stockWebService)
    {
        _stockWebService = stockWebService;
    }

    // Kullanıcının stok listesini gösterir.
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (string.IsNullOrWhiteSpace(token))
            return RedirectToAction("Login", "Auth");

        var stockItems = await _stockWebService.GetMyStockAsync(token);

        return View(stockItems);
    }

    // Son kullanma tarihi yaklaşan veya riskli kabul edilen stok ürünlerini gösterir.
    [HttpGet]
    public async Task<IActionResult> Risky()
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (string.IsNullOrWhiteSpace(token))
            return RedirectToAction("Login", "Auth");

        var riskyItems = await _stockWebService.GetRiskyStockAsync(token);

        return View(riskyItems);
    }

    // Yeni stok ürünü ekleme formunu açar.
    [HttpGet]
    public IActionResult Create()
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (string.IsNullOrWhiteSpace(token))
            return RedirectToAction("Login", "Auth");

        var model = new CreateStockItemViewModel();

        // Ürün, birim ve saklama koşulu dropdown listeleri doldurulur.
        FillSelectLists(model);

        return View(model);
    }

    // Yeni stok ürünü ekleme formundan gelen bilgileri API'ye gönderir.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateStockItemViewModel model)
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (string.IsNullOrWhiteSpace(token))
            return RedirectToAction("Login", "Auth");

        if (!ModelState.IsValid)
        {
            // Form hatalıysa dropdown listelerinin boş kalmaması için tekrar doldurulur.
            FillSelectLists(model);
            return View(model);
        }

        // Yeni stok ürünü API üzerinden giriş yapan kullanıcı adına oluşturulur.
        var result = await _stockWebService.CreateAsync(model, token);

        if (!result)
        {
            ModelState.AddModelError(string.Empty, "Stok ürünü eklenirken bir hata oluştu.");
            FillSelectLists(model);
            return View(model);
        }

        TempData["SuccessMessage"] = "Stok ürünü başarıyla eklendi.";
        return RedirectToAction(nameof(Index));
    }

    // Stok ürünü düzenleme formunu mevcut bilgilerle açar.
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (string.IsNullOrWhiteSpace(token))
            return RedirectToAction("Login", "Auth");

        // Düzenleme formunu doldurmak için mevcut stok ürünü API üzerinden alınır.
        var stockItem = await _stockWebService.GetByIdAsync(id, token);

        if (stockItem == null)
        {
            TempData["ErrorMessage"] = "Düzenlenecek stok ürünü bulunamadı.";
            return RedirectToAction(nameof(Index));
        }

        // API'den gelen stok bilgileri düzenleme formunun ViewModel yapısına aktarılır.
        var model = new EditStockItemViewModel
        {
            Id = stockItem.Id,
            ProductId = stockItem.ProductId,
            ProductName = stockItem.ProductName,
            UnitId = stockItem.UnitId,
            Quantity = stockItem.Quantity,
            ExpirationDate = stockItem.ExpirationDate,
            OpenedDate = stockItem.OpenedDate,
            StorageCondition = GetStorageConditionValue(stockItem.StorageCondition),
            Note = stockItem.Note
        };

        FillSelectLists(model);

        return View(model);
    }

    // Stok ürünü düzenleme formundan gelen güncelleme bilgisini API'ye gönderir.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditStockItemViewModel model)
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (string.IsNullOrWhiteSpace(token))
            return RedirectToAction("Login", "Auth");

        if (!ModelState.IsValid)
        {
            FillSelectLists(model);
            return View(model);
        }

        // Kullanıcının stok ürünü API üzerinden güncellenir.
        var result = await _stockWebService.UpdateAsync(model.Id, model, token);

        if (!result)
        {
            ModelState.AddModelError(string.Empty, "Stok ürünü güncellenirken bir hata oluştu.");
            FillSelectLists(model);
            return View(model);
        }

        TempData["SuccessMessage"] = "Stok ürünü başarıyla güncellendi.";
        return RedirectToAction(nameof(Index));
    }

    // Kullanıcının seçtiği stok ürününü silme isteğini API'ye gönderir.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (string.IsNullOrWhiteSpace(token))
            return RedirectToAction("Login", "Auth");

        var result = await _stockWebService.DeleteAsync(id, token);

        if (!result)
        {
            TempData["ErrorMessage"] = "Bu ürün aktif bir paylaşım ilanında kullanılıyor. Silmeden önce paylaşım ilanını iptal etmelisin.";
            return RedirectToAction(nameof(Index));
        }

        TempData["SuccessMessage"] = "Stok ürünü başarıyla silindi.";
        return RedirectToAction(nameof(Index));
    }

    // Son kullanma tarihi geçmiş stok ürünlerini gösterir.
    [HttpGet]
    public async Task<IActionResult> Expired()
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (string.IsNullOrWhiteSpace(token))
            return RedirectToAction("Login", "Auth");

        var expiredItems = await _stockWebService.GetExpiredStockAsync(token);

        return View(expiredItems);
    }

    // Stok ekleme formunda kullanılacak dropdown listelerini doldurur.
    private static void FillSelectLists(CreateStockItemViewModel model)
    {
        // Ürün ve birim listeleri seed data ile uyumlu olacak şekilde geçici olarak Web tarafında tutulur.
        // İleride Product/ReferenceData API eklendiğinde bu listeler API'den dinamik alınabilir.
        model.Products = GetProductSelectList();
        model.Units = GetUnitSelectList();
        model.StorageConditions = GetStorageConditionSelectList();
    }

    // Stok düzenleme formunda kullanılacak dropdown listelerini doldurur.
    private static void FillSelectLists(EditStockItemViewModel model)
    {
        // Seçili değerler modeldeki ProductId, UnitId ve StorageCondition değerlerine göre otomatik işaretlenir.
        model.Products = GetProductSelectList();
        model.Units = GetUnitSelectList();
        model.StorageConditions = GetStorageConditionSelectList();
    }

    // Ürün dropdown listesinde gösterilecek ürünleri döndürür.
    private static List<SelectListItem> GetProductSelectList()
    {
        // Bu değerler Products tablosundaki gerçek Id değerleriyle aynı olmalıdır.
        // İleride ürünler API'den çekildiğinde bu manuel liste kaldırılacaktır.
        return new List<SelectListItem>
        {
            new("Süt", "1"),
            new("Yoğurt", "2"),
            new("Peynir", "3"),
            new("Yumurta", "4"),
            new("Domates", "5"),
            new("Salatalık", "6"),
            new("Muz", "7"),
            new("Ekmek", "8"),
            new("Pirinç", "9")
        };
    }

    // Birim dropdown listesinde gösterilecek birimleri döndürür.
    private static List<SelectListItem> GetUnitSelectList()
    {
        // Bu değerler Units tablosundaki gerçek Id değerleriyle aynı olmalıdır.
        return new List<SelectListItem>
        {
            new("Kilogram", "1"),
            new("Gram", "2"),
            new("Litre", "3"),
            new("Mililitre", "4"),
            new("Adet", "5"),
            new("Paket", "6")
        };
    }

    // Saklama koşulu dropdown listesinde gösterilecek değerleri döndürür.
    private static List<SelectListItem> GetStorageConditionSelectList()
    {
        // Saklama koşulu değerleri API tarafındaki enum değerleriyle uyumlu olacak şekilde gönderilir.
        return new List<SelectListItem>
        {
            new("Oda Sıcaklığı", "1"),
            new("Buzdolabı", "2"),
            new("Dondurucu", "3"),
            new("Kuru Depolama", "4"),
            new("Bilinmiyor", "5")
        };
    }

    // API'den gelen saklama koşulu bilgisini formdaki dropdown değerine çevirir.
    private static string GetStorageConditionValue(string? storageCondition)
    {
        // API'den enum adı, Türkçe değer veya sayısal değer gelebileceği için hepsi aynı dropdown değeriyle eşleştirilir.
        return storageCondition switch
        {
            "RoomTemperature" or "Oda Sıcaklığı" or "1" => "1",
            "Refrigerated" or "Buzdolabı" or "2" => "2",
            "Frozen" or "Dondurucu" or "3" => "3",
            "DryStorage" or "Kuru Depolama" or "4" => "4",
            "Unknown" or "Bilinmiyor" or "5" => "5",
            _ => "5"
        };
    }
}

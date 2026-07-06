
// SharingController, Web arayüzünde paylaşım ilanı işlemlerini yönetir.
// Kullanıcı kendi stok ürününü paylaşım ilanına dönüştürebilir, mevcut ilanları görebilir,
// ilanlara talep gönderebilir ve kendi ilanlarına gelen talepleri onaylayıp reddedebilir.
// Controller API'ye doğrudan gitmez; tüm işlemleri ISharingWebService üzerinden FoodWise.API'ye gönderir.

using FoodWise.Web.Services;
using FoodWise.Web.ViewModels.Sharing;
using Microsoft.AspNetCore.Mvc;

namespace FoodWise.Web.Controllers;

public class SharingController : Controller
{
    private readonly ISharingWebService _sharingWebService;

    public SharingController(ISharingWebService sharingWebService)
    {
        _sharingWebService = sharingWebService;
    }

    // Kullanıcının oluşturduğu paylaşım ilanlarını listeler.
    [HttpGet]
    public async Task<IActionResult> MyListings(int? highlightListingId)
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (string.IsNullOrWhiteSpace(token))
            return RedirectToAction("Login", "Auth");

        var listings = await _sharingWebService.GetMyListingsAsync(token);

        // Yeni işlem yapılan ilanın sayfada vurgulanması için kullanılır.
        ViewBag.HighlightListingId = highlightListingId;

        return View(listings);
    }

    // Stok ürününden paylaşım ilanı oluşturma formunu açar.
    [HttpGet]
    public async Task<IActionResult> Create(int stockItemId, string? productName)
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (string.IsNullOrWhiteSpace(token))
            return RedirectToAction("Login", "Auth");

        // Form ilk açıldığında ürün adı, başlık ve teslim zamanları varsayılan olarak hazırlanır.
        var model = new CreateShareListingViewModel
        {
            StockItemId = stockItemId,
            ProductName = productName,
            Title = !string.IsNullOrWhiteSpace(productName)
                ? $"{productName} paylaşımı"
                : "Gıda paylaşımı",
            PickupStartTime = DateTime.Now.AddHours(1),
            PickupEndTime = DateTime.Now.AddHours(24)
        };

        // Kullanıcının konumuna göre uygun teslim noktaları API'den alınır.
        await FillDeliveryPointsAsync(model, token);

        return View(model);
    }

    // Paylaşım ilanı oluşturma formundan gelen bilgileri API'ye gönderir.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateShareListingViewModel model)
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (string.IsNullOrWhiteSpace(token))
            return RedirectToAction("Login", "Auth");

        // Teslim bitiş zamanı başlangıç zamanından önce olamaz.
        if (model.PickupEndTime <= model.PickupStartTime)
        {
            ModelState.AddModelError(
                nameof(model.PickupEndTime),
                "Teslim bitiş zamanı başlangıç zamanından sonra olmalıdır.");
        }

        if (!ModelState.IsValid)
        {
            // Form hatalıysa teslim noktaları tekrar yüklenir.
            // Aksi halde sayfa tekrar açıldığında dropdown/list alanı boş kalabilir.
            await FillDeliveryPointsAsync(model, token);
            return View(model);
        }

        var result = await _sharingWebService.CreateListingAsync(model, token);

        if (!result)
        {
            ModelState.AddModelError(
                string.Empty,
                "Paylaşım ilanı oluşturulamadı. Ürün zaten aktif bir paylaşım ilanında olabilir veya miktar/teslim noktası bilgilerini kontrol etmelisin.");

            await FillDeliveryPointsAsync(model, token);
            return View(model);
        }

        TempData["SuccessMessage"] = "Paylaşım ilanı başarıyla oluşturuldu.";
        return RedirectToAction(nameof(MyListings));
    }

    // Kullanıcının talep gönderebileceği aktif paylaşım ilanlarını listeler.
    [HttpGet]
    public async Task<IActionResult> Available()
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (string.IsNullOrWhiteSpace(token))
            return RedirectToAction("Login", "Auth");

        var listings = await _sharingWebService.GetAvailableListingsAsync(token);

        return View(listings);
    }

    // Seçilen paylaşım ilanına talep gönderme isteğini API'ye iletir.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateRequest(int listingId)
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (string.IsNullOrWhiteSpace(token))
            return RedirectToAction("Login", "Auth");

        var result = await _sharingWebService.CreateRequestAsync(listingId, token);

        if (result)
        {
            TempData["SuccessMessage"] = "Paylaşım talebin başarıyla gönderildi.";
        }
        else
        {
            TempData["ErrorMessage"] = "Talep gönderilemedi. Kendi ilanına talep gönderemezsin veya daha önce talep göndermiş olabilirsin.";
        }

        return RedirectToAction(nameof(Available));
    }

    // İlan sahibinin kendi ilanına gelen talepleri görüntülemesini sağlar.
    [HttpGet]
    public async Task<IActionResult> Requests(int listingId)
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (string.IsNullOrWhiteSpace(token))
            return RedirectToAction("Login", "Auth");

        var requests = await _sharingWebService.GetRequestsForListingAsync(listingId, token);

        ViewBag.ListingId = listingId;

        return View(requests);
    }

    // İlan sahibinin gelen paylaşım talebini onaylama isteğini API'ye gönderir.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveRequest(int requestId, int listingId)
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (string.IsNullOrWhiteSpace(token))
            return RedirectToAction("Login", "Auth");

        var result = await _sharingWebService.ApproveRequestAsync(requestId, token);

        if (result)
        {
            TempData["SuccessMessage"] = "Talep başarıyla onaylandı.";
        }
        else
        {
            TempData["ErrorMessage"] = "Talep onaylanamadı.";
        }

        return RedirectToAction(nameof(Requests), new { listingId });
    }

    // İlan sahibinin gelen paylaşım talebini reddetme isteğini API'ye gönderir.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectRequest(int requestId, int listingId)
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (string.IsNullOrWhiteSpace(token))
            return RedirectToAction("Login", "Auth");

        var result = await _sharingWebService.RejectRequestAsync(requestId, token);

        if (result)
        {
            TempData["SuccessMessage"] = "Talep reddedildi.";
        }
        else
        {
            TempData["ErrorMessage"] = "Talep reddedilemedi.";
        }

        return RedirectToAction(nameof(Requests), new { listingId });
    }

    // Kullanıcının kendi paylaşım ilanını iptal etme isteğini API'ye gönderir.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int listingId)
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (string.IsNullOrWhiteSpace(token))
            return RedirectToAction("Login", "Auth");

        // Teslimat sürecine geçmiş ilanların iptal edilip edilmeyeceği API tarafında kontrol edilir.
        var result = await _sharingWebService.CancelListingAsync(listingId, token);

        if (result)
        {
            TempData["SuccessMessage"] = "Paylaşım ilanı başarıyla iptal edildi.";
        }
        else
        {
            TempData["ErrorMessage"] = "Paylaşım ilanı iptal edilemedi. İlan teslimat sürecine geçmiş olabilir.";
        }

        return RedirectToAction(nameof(MyListings));
    }

    // Talep sahibinin kendi gönderdiği paylaşım talebini iptal etmesini sağlar.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelRequest(int requestId)
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (string.IsNullOrWhiteSpace(token))
            return RedirectToAction("Login", "Auth");

        var result = await _sharingWebService.CancelRequestAsync(requestId, token);

        if (!result)
        {
            TempData["ErrorMessage"] = "Talep iptal edilemedi.";
            return RedirectToAction(nameof(Available));
        }

        TempData["SuccessMessage"] = "Talep başarıyla iptal edildi.";
        return RedirectToAction(nameof(Available));
    }

    // Paylaşım ilanı formunda gösterilecek teslim noktalarını hazırlar.
    private async Task FillDeliveryPointsAsync(CreateShareListingViewModel model, string token)
    {
        // Teslim noktaları sabit olarak tutulmaz; API'den alınır.
        // API, kullanıcının şehir/ilçe/mahalle bilgisine göre yakın teslim noktalarını öncelikli döndürür.
        model.DeliveryPoints = await _sharingWebService.GetDeliveryPointsAsync(
            token,
            model.DeliveryPointSearch
        );
    }
}


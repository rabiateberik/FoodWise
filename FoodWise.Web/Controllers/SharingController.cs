// SharingController, Web arayüzünde paylaşım ilanı oluşturma ve kullanıcının ilanlarını listeleme işlemlerini yönetir.
// API ile doğrudan değil, ISharingWebService üzerinden haberleşir.

using FoodWise.Web.Services;
using FoodWise.Web.ViewModels.Sharing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FoodWise.Web.Controllers;

public class SharingController : Controller
{
    private readonly ISharingWebService _sharingWebService;

    public SharingController(ISharingWebService sharingWebService)
    {
        _sharingWebService = sharingWebService;
    }

    [HttpGet]
    public async Task<IActionResult> MyListings()
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (string.IsNullOrWhiteSpace(token))
            return RedirectToAction("Login", "Auth");

        var listings = await _sharingWebService.GetMyListingsAsync(token);

        ViewBag.FullName = HttpContext.Session.GetString("FullName");
        ViewBag.Email = HttpContext.Session.GetString("Email");

        return View(listings);
    }

    [HttpGet]
    public IActionResult Create(int stockItemId, string? productName)
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (string.IsNullOrWhiteSpace(token))
            return RedirectToAction("Login", "Auth");

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

        FillDeliveryPoints(model);

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateShareListingViewModel model)
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (string.IsNullOrWhiteSpace(token))
            return RedirectToAction("Login", "Auth");

        if (model.PickupEndTime <= model.PickupStartTime)
            ModelState.AddModelError(nameof(model.PickupEndTime), "Teslim bitiş zamanı başlangıç zamanından sonra olmalıdır.");

        if (!ModelState.IsValid)
        {
            FillDeliveryPoints(model);
            return View(model);
        }

        var result = await _sharingWebService.CreateListingAsync(model, token);

        if (!result)
        {
            ModelState.AddModelError(string.Empty, "Paylaşım ilanı oluşturulamadı. Miktar, stok ürünü veya teslim noktası bilgisini kontrol edin.");
            FillDeliveryPoints(model);
            return View(model);
        }

        TempData["SuccessMessage"] = "Paylaşım ilanı başarıyla oluşturuldu.";
        return RedirectToAction(nameof(MyListings));
    }
    [HttpGet]
    public async Task<IActionResult> Available()
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (string.IsNullOrWhiteSpace(token))
            return RedirectToAction("Login", "Auth");

        var listings = await _sharingWebService.GetAvailableListingsAsync(token);

        ViewBag.FullName = HttpContext.Session.GetString("FullName");
        ViewBag.Email = HttpContext.Session.GetString("Email");

        return View(listings);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateRequest(int listingId)
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (string.IsNullOrWhiteSpace(token))
            return RedirectToAction("Login", "Auth");

        var result = await _sharingWebService.CreateRequestAsync(listingId, token);

        TempData["SuccessMessage"] = result
            ? "Paylaşım talebin başarıyla gönderildi."
            : "Talep gönderilemedi. Kendi ilanına talep gönderemezsin veya daha önce talep göndermiş olabilirsin.";

        return RedirectToAction(nameof(Available));
    }

    [HttpGet]
    public async Task<IActionResult> Requests(int listingId)
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (string.IsNullOrWhiteSpace(token))
            return RedirectToAction("Login", "Auth");

        var requests = await _sharingWebService.GetRequestsForListingAsync(listingId, token);

        ViewBag.ListingId = listingId;
        ViewBag.FullName = HttpContext.Session.GetString("FullName");
        ViewBag.Email = HttpContext.Session.GetString("Email");

        return View(requests);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveRequest(int requestId, int listingId)
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (string.IsNullOrWhiteSpace(token))
            return RedirectToAction("Login", "Auth");

        var result = await _sharingWebService.ApproveRequestAsync(requestId, token);

        TempData["SuccessMessage"] = result
            ? "Talep başarıyla onaylandı."
            : "Talep onaylanamadı.";

        return RedirectToAction(nameof(Requests), new { listingId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectRequest(int requestId, int listingId)
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (string.IsNullOrWhiteSpace(token))
            return RedirectToAction("Login", "Auth");

        var result = await _sharingWebService.RejectRequestAsync(requestId, token);

        TempData["SuccessMessage"] = result
            ? "Talep reddedildi."
            : "Talep reddedilemedi.";

        return RedirectToAction(nameof(Requests), new { listingId });
    }
    private static void FillDeliveryPoints(CreateShareListingViewModel model)
    {
        // Teslim noktaları şu an seed data ile sabit tutulur.
        // İleride DeliveryPoint API eklenirse bu liste API'den dinamik alınabilir.
        model.DeliveryPoints = new List<SelectListItem>
        {
            new("Kampüs Kütüphane Girişi", "1"),
            new("Yurt Danışma Noktası", "2"),
            new("Kafeterya Önü", "3")
        };
    }
}
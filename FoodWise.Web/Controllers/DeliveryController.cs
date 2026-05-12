// DeliveryController, Web arayüzünde QR destekli teslimat işlemlerini yönetir.
// Bağışlanan ve alınacak teslimatlar listelenir; drop-off, QR okutma ve teslim tamamlama işlemleri yapılır.

using FoodWise.Web.Services;
using FoodWise.Web.ViewModels.Delivery;
using Microsoft.AspNetCore.Mvc;

namespace FoodWise.Web.Controllers;

public class DeliveryController : Controller
{
    private readonly IDeliveryWebService _deliveryWebService;

    public DeliveryController(IDeliveryWebService deliveryWebService)
    {
        _deliveryWebService = deliveryWebService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (string.IsNullOrWhiteSpace(token))
            return RedirectToAction("Login", "Auth");

        var model = await CreateDeliveryPageModelAsync(token);

        // Tüm Teslimatlar sekmesinde sadece aktif süreçler gösterilir.
        model.DonatedDeliveries = model.DonatedDeliveries
            .Where(IsActiveDelivery)
            .ToList();

        model.ReceivedDeliveries = model.ReceivedDeliveries
            .Where(IsActiveDelivery)
            .ToList();

        ViewBag.ActiveDeliveryTab = "Index";
        ViewBag.DeliveryTabTitle = "Tüm Teslimatlar";
        ViewBag.DeliveryTabDescription = "Aktif teslimat süreçlerini buradan takip edebilirsin.";

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Outgoing()
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (string.IsNullOrWhiteSpace(token))
            return RedirectToAction("Login", "Auth");

        var model = await CreateDeliveryPageModelAsync(token);

        // Teslim Edeceklerim sekmesinde tamamlanan/iptal/süresi dolan kayıtlar gösterilmez.
        model.DonatedDeliveries = model.DonatedDeliveries
            .Where(IsActiveDelivery)
            .ToList();

        model.ReceivedDeliveries = new List<DeliveryViewModel>();

        ViewBag.ActiveDeliveryTab = "Outgoing";
        ViewBag.DeliveryTabTitle = "Teslim Edeceklerim";
        ViewBag.DeliveryTabDescription = "Bağışladığın aktif teslimatları buradan yönetebilirsin.";

        return View("Index", model);
    }

    [HttpGet]
    public async Task<IActionResult> Incoming()
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (string.IsNullOrWhiteSpace(token))
            return RedirectToAction("Login", "Auth");

        var model = await CreateDeliveryPageModelAsync(token);

        // Teslim Alacaklarım sekmesinde tamamlanan/iptal/süresi dolan kayıtlar gösterilmez.
        model.ReceivedDeliveries = model.ReceivedDeliveries
            .Where(IsActiveDelivery)
            .ToList();

        model.DonatedDeliveries = new List<DeliveryViewModel>();

        ViewBag.ActiveDeliveryTab = "Incoming";
        ViewBag.DeliveryTabTitle = "Teslim Alacaklarım";
        ViewBag.DeliveryTabDescription = "Teslim alacağın aktif ürünleri buradan takip edebilirsin.";

        return View("Index", model);
    }

    [HttpGet]
    public async Task<IActionResult> Completed()
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (string.IsNullOrWhiteSpace(token))
            return RedirectToAction("Login", "Auth");

        var model = await CreateDeliveryPageModelAsync(token);

        // Tamamlananlar sekmesinde sadece teslimatı tamamlanan kayıtlar gösterilir.
        model.DonatedDeliveries = model.DonatedDeliveries
            .Where(IsCompletedDelivery)
            .ToList();

        model.ReceivedDeliveries = model.ReceivedDeliveries
            .Where(IsCompletedDelivery)
            .ToList();

        ViewBag.ActiveDeliveryTab = "Completed";
        ViewBag.DeliveryTabTitle = "Tamamlananlar";
        ViewBag.DeliveryTabDescription = "Başarıyla tamamlanan teslimat geçmişini buradan görüntüleyebilirsin.";

        return View("Index", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateFromRequest(int shareRequestId)
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (string.IsNullOrWhiteSpace(token))
            return RedirectToAction("Login", "Auth");

        var result = await _deliveryWebService.CreateDeliveryAsync(shareRequestId, token);

        TempData["SuccessMessage"] = result != null
            ? "Teslimat başarıyla oluşturuldu ve teslim kutusu atandı."
            : "Teslimat oluşturulamadı. Talep durumu, yetki veya boş kutu durumunu kontrol edin.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DropOff(DropOffDeliveryViewModel model)
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (string.IsNullOrWhiteSpace(token))
            return RedirectToAction("Login", "Auth");

        var result = await _deliveryWebService.MarkAsDroppedOffAsync(model, token);

        TempData["SuccessMessage"] = result != null
            ? "Ürün kutuya bırakıldı olarak işaretlendi."
            : "Ürün kutuya bırakıldı olarak işaretlenemedi.";

        return RedirectToAction(nameof(Outgoing));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ScanBox(ScanDeliveryBoxViewModel model)
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (string.IsNullOrWhiteSpace(token))
            return RedirectToAction("Login", "Auth");

        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "QR kod değeri boş olamaz.";
            return RedirectToAction(nameof(Incoming));
        }

        var result = await _deliveryWebService.ScanBoxQrAsync(model, token);

        TempData["SuccessMessage"] = result != null
            ? "QR kod doğrulandı. Teslimatı tamamlayabilirsin."
            : "Bu QR kod için sana ait aktif teslimat bulunamadı.";

        return RedirectToAction(nameof(Incoming));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(int deliveryId)
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (string.IsNullOrWhiteSpace(token))
            return RedirectToAction("Login", "Auth");

        var result = await _deliveryWebService.CompleteDeliveryAsync(deliveryId, token);

        TempData["SuccessMessage"] = result != null
            ? "Teslimat başarıyla tamamlandı."
            : "Teslimat tamamlanamadı. Yetki, durum veya süre bilgisini kontrol edin.";

        return RedirectToAction(nameof(Incoming));
    }

    private async Task<DeliveryPageViewModel> CreateDeliveryPageModelAsync(string token)
    {
        var donatedDeliveries = await _deliveryWebService.GetMyDonatedDeliveriesAsync(token);
        var receivedDeliveries = await _deliveryWebService.GetMyReceivedDeliveriesAsync(token);

        return new DeliveryPageViewModel
        {
            DonatedDeliveries = donatedDeliveries,
            ReceivedDeliveries = receivedDeliveries
        };
    }

    private static bool IsActiveDelivery(DeliveryViewModel delivery)
    {
        if (string.IsNullOrWhiteSpace(delivery.Status))
            return true;

        return !delivery.Status.Equals("Delivered", StringComparison.OrdinalIgnoreCase) &&
               !delivery.Status.Equals("Completed", StringComparison.OrdinalIgnoreCase) &&
               !delivery.Status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase) &&
               !delivery.Status.Equals("Expired", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsCompletedDelivery(DeliveryViewModel delivery)
    {
        if (string.IsNullOrWhiteSpace(delivery.Status))
            return false;

        return delivery.Status.Equals("Delivered", StringComparison.OrdinalIgnoreCase) ||
               delivery.Status.Equals("Completed", StringComparison.OrdinalIgnoreCase);
    }
}
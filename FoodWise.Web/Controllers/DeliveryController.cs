// DeliveryController, Web arayüzünde QR destekli teslimat işlemlerini yönetir.
// Bağışlanan ve alınacak teslimatlar listelenir; drop-off, QR okutma ve teslim tamamlama işlemleri yapılır.
// Tüm Teslimatlar sekmesi kaldırılmıştır; varsayılan teslimat ekranı Teslim Edeceklerim olarak açılır.

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
    public IActionResult Index()
    {
        // Tüm Teslimatlar ekranı kaldırıldı.
        // Teslimatlar menüsüne gelen kullanıcı varsayılan olarak Teslim Edeceklerim ekranına yönlendirilir.
        return RedirectToAction(nameof(Outgoing));
    }

    [HttpGet]
    public async Task<IActionResult> Outgoing()
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (string.IsNullOrWhiteSpace(token))
            return RedirectToAction("Login", "Auth");

        var model = await CreateDeliveryPageModelAsync(token);

        // Teslim Edeceklerim sekmesinde yalnızca aktif bağış teslimatları gösterilir.
        model.DonatedDeliveries = model.DonatedDeliveries
            .Where(IsActiveDelivery)
            .ToList();

        model.ReceivedDeliveries = new List<DeliveryViewModel>();

        ViewBag.ActiveDeliveryTab = "Outgoing";
        ViewBag.DeliveryTabTitle = "Teslim Edeceklerim";
        ViewBag.DeliveryTabDescription = "Paylaştığın ürünlerin teslimat ve kutuya bırakma sürecini buradan takip edebilirsin.";

        return View("Index", model);
    }

    [HttpGet]
    public async Task<IActionResult> Incoming()
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (string.IsNullOrWhiteSpace(token))
            return RedirectToAction("Login", "Auth");

        var model = await CreateDeliveryPageModelAsync(token);

        // Teslim Alacaklarım sekmesinde yalnızca aktif alım teslimatları gösterilir.
        model.ReceivedDeliveries = model.ReceivedDeliveries
            .Where(IsActiveDelivery)
            .ToList();

        model.DonatedDeliveries = new List<DeliveryViewModel>();

        ViewBag.ActiveDeliveryTab = "Incoming";
        ViewBag.DeliveryTabTitle = "Teslim Alacaklarım";
        ViewBag.DeliveryTabDescription = "Onaylanan taleplerin için teslim alma sürecini buradan takip edebilirsin.";

        return View("Index", model);
    }

    [HttpGet]
    public async Task<IActionResult> Completed()
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (string.IsNullOrWhiteSpace(token))
            return RedirectToAction("Login", "Auth");

        var model = await CreateDeliveryPageModelAsync(token);

        // Tamamlananlar sekmesinde teslim alınmış veya tamamlanmış kayıtlar gösterilir.
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

        if (result != null)
        {
            TempData["SuccessMessage"] = "Teslimat başarıyla oluşturuldu ve teslim kutusu atandı.";
            return RedirectToAction(nameof(Outgoing));
        }

        TempData["ErrorMessage"] = "Teslimat oluşturulamadı. Talep durumu, yetki veya boş kutu durumunu kontrol edin.";
        return RedirectToAction("MyListings", "Sharing");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DropOff(DropOffDeliveryViewModel model)
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (string.IsNullOrWhiteSpace(token))
            return RedirectToAction("Login", "Auth");

        var result = await _deliveryWebService.MarkAsDroppedOffAsync(model, token);

        if (result != null)
        {
            TempData["SuccessMessage"] = "Ürün kutuya bırakıldı olarak işaretlendi.";
        }
        else
        {
            TempData["ErrorMessage"] = "Ürün kutuya bırakıldı olarak işaretlenemedi.";
        }

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

        if (result != null)
        {
            TempData["SuccessMessage"] = "QR kod doğrulandı. Teslimatı tamamlayabilirsin.";
        }
        else
        {
            TempData["ErrorMessage"] = "Bu QR kod için sana ait aktif teslimat bulunamadı.";
        }

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

        if (result != null)
        {
            TempData["SuccessMessage"] = "Teslimat başarıyla tamamlandı.";
        }
        else
        {
            TempData["ErrorMessage"] = "Teslimat tamamlanamadı. Yetki, durum veya süre bilgisini kontrol edin.";
        }

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
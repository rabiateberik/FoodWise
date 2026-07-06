
// DeliveryController, Web arayüzündeki QR destekli teslimat sürecini yönetir.
// Bağışçının teslim edeceği ürünler, alıcının teslim alacağı ürünler ve tamamlanan teslimatlar bu controller üzerinden görüntülenir.
// Controller teslimat durumunu doğrudan değiştirmez; tüm işlemleri IDeliveryWebService aracılığıyla FoodWise.API'ye gönderir.

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

    // Teslimat ana sayfasına gelen kullanıcı varsayılan olarak Teslim Edeceklerim sekmesine yönlendirilir.
    // Index doğrudan Outgoing action'ına gider.
    [HttpGet]
    public IActionResult Index()
    {
        return RedirectToAction(nameof(Outgoing));
    }

    // Kullanıcının bağışçı olduğu aktif teslimatları listeler.
    [HttpGet]
    public async Task<IActionResult> Outgoing()
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (string.IsNullOrWhiteSpace(token))
            return RedirectToAction("Login", "Auth");

        var model = await CreateDeliveryPageModelAsync(token);

        // Teslim Edeceklerim sekmesinde yalnızca devam eden bağış teslimatları gösterilir.
        model.DonatedDeliveries = model.DonatedDeliveries
            .Where(IsActiveDelivery)
            .ToList();

        // Bu sekmede alıcı teslimatları gösterilmez.
        model.ReceivedDeliveries = new List<DeliveryViewModel>();

        ViewBag.ActiveDeliveryTab = "Outgoing";
        ViewBag.DeliveryTabTitle = "Teslim Edeceklerim";
        ViewBag.DeliveryTabDescription = "Paylaştığın ürünlerin teslimat ve kutuya bırakma sürecini buradan takip edebilirsin.";

        return View("Index", model);
    }

    // Kullanıcının alıcı olduğu aktif teslimatları listeler.
    [HttpGet]
    public async Task<IActionResult> Incoming()
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (string.IsNullOrWhiteSpace(token))
            return RedirectToAction("Login", "Auth");

        var model = await CreateDeliveryPageModelAsync(token);

        // Teslim Alacaklarım sekmesinde yalnızca devam eden alım teslimatları gösterilir.
        model.ReceivedDeliveries = model.ReceivedDeliveries
            .Where(IsActiveDelivery)
            .ToList();

        // Bu sekmede bağış teslimatları gösterilmez.
        model.DonatedDeliveries = new List<DeliveryViewModel>();

        ViewBag.ActiveDeliveryTab = "Incoming";
        ViewBag.DeliveryTabTitle = "Teslim Alacaklarım";
        ViewBag.DeliveryTabDescription = "Onaylanan taleplerin için teslim alma sürecini buradan takip edebilirsin.";

        return View("Index", model);
    }

    // Kullanıcının tamamlanan teslimatlarını listeler.
    [HttpGet]
    public async Task<IActionResult> Completed()
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (string.IsNullOrWhiteSpace(token))
            return RedirectToAction("Login", "Auth");

        var model = await CreateDeliveryPageModelAsync(token);

        // Tamamlananlar sekmesinde hem bağışlanan hem de teslim alınan tamamlanmış kayıtlar gösterilir.
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

    // Onaylanmış paylaşım talebinden teslimat oluşturma isteğini API'ye gönderir.
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

    // Bağışçının ürünü teslim kutusuna bıraktığını API'ye bildirir.
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

    // Alıcının teslimat kutusu üzerindeki QR kodu okutarak teslimatı doğrulamasını sağlar.
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

    // QR doğrulaması yapılan teslimatı tamamlamak için API'ye istek gönderir.
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

    // Teslimat sayfasında kullanılacak bağış ve alım teslimat listelerini hazırlar.
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

    // Teslimatın devam eden bir teslimat olup olmadığını kontrol eder.
    // Tamamlanmış, iptal edilmiş veya süresi geçmiş teslimatlar aktif listelerde gösterilmez.
    private static bool IsActiveDelivery(DeliveryViewModel delivery)
    {
        if (string.IsNullOrWhiteSpace(delivery.Status))
            return true;

        return !delivery.Status.Equals("Delivered", StringComparison.OrdinalIgnoreCase) &&
               !delivery.Status.Equals("Completed", StringComparison.OrdinalIgnoreCase) &&
               !delivery.Status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase) &&
               !delivery.Status.Equals("Expired", StringComparison.OrdinalIgnoreCase);
    }

    // Teslimatın tamamlanan teslimatlar sekmesinde gösterilip gösterilmeyeceğini kontrol eder.
    private static bool IsCompletedDelivery(DeliveryViewModel delivery)
    {
        if (string.IsNullOrWhiteSpace(delivery.Status))
            return false;

        return delivery.Status.Equals("Delivered", StringComparison.OrdinalIgnoreCase) ||
               delivery.Status.Equals("Completed", StringComparison.OrdinalIgnoreCase);
    }
}

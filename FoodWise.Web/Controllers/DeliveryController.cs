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

        var donatedDeliveries = await _deliveryWebService.GetMyDonatedDeliveriesAsync(token);
        var receivedDeliveries = await _deliveryWebService.GetMyReceivedDeliveriesAsync(token);

        var model = new DeliveryPageViewModel
        {
            DonatedDeliveries = donatedDeliveries,
            ReceivedDeliveries = receivedDeliveries
        };

        ViewBag.FullName = HttpContext.Session.GetString("FullName");
        ViewBag.Email = HttpContext.Session.GetString("Email");

        return View(model);
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

        return RedirectToAction(nameof(Index));
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
            TempData["SuccessMessage"] = "QR kod değeri boş olamaz.";
            return RedirectToAction(nameof(Index));
        }

        var result = await _deliveryWebService.ScanBoxQrAsync(model, token);

        TempData["SuccessMessage"] = result != null
            ? "QR kod doğrulandı. Teslimatı tamamlayabilirsin."
            : "Bu QR kod için sana ait aktif teslimat bulunamadı.";

        return RedirectToAction(nameof(Index));
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

        return RedirectToAction(nameof(Index));
    }
}
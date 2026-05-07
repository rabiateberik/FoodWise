// StockController, Web arayüzünde stok listeleme ve stok ekleme işlemlerini yönetir.
// API ile doğrudan değil, IStockWebService üzerinden haberleşir.

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

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (string.IsNullOrWhiteSpace(token))
            return RedirectToAction("Login", "Auth");

        var stockItems = await _stockWebService.GetMyStockAsync(token);

        ViewBag.FullName = HttpContext.Session.GetString("FullName");
        ViewBag.Email = HttpContext.Session.GetString("Email");

        return View(stockItems);
    }
    [HttpGet]
    public async Task<IActionResult> Risky()
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (string.IsNullOrWhiteSpace(token))
            return RedirectToAction("Login", "Auth");

        var riskyItems = await _stockWebService.GetRiskyStockAsync(token);

        ViewBag.FullName = HttpContext.Session.GetString("FullName");
        ViewBag.Email = HttpContext.Session.GetString("Email");

        return View(riskyItems);
    }
    [HttpGet]
    public IActionResult Create()
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (string.IsNullOrWhiteSpace(token))
            return RedirectToAction("Login", "Auth");

        var model = new CreateStockItemViewModel();
        FillSelectLists(model);

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateStockItemViewModel model)
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (string.IsNullOrWhiteSpace(token))
            return RedirectToAction("Login", "Auth");

        if (!ModelState.IsValid)
        {
            FillSelectLists(model);
            return View(model);
        }

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

    private static void FillSelectLists(CreateStockItemViewModel model)
    {
        // Ürün ve birim listeleri seed data ile uyumlu olacak şekilde geçici olarak Web tarafında tutulur.
        // İleride Product API eklendiğinde bu listeler API'den dinamik alınabilir.

        model.Products = new List<SelectListItem>
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

        model.Units = new List<SelectListItem>
        {
            new("Kilogram", "1"),
            new("Gram", "2"),
            new("Litre", "3"),
            new("Mililitre", "4"),
            new("Adet", "5")
        };

        model.StorageConditions = new List<SelectListItem>
        {
            new("Oda Sıcaklığı", "1"),
            new("Buzdolabı", "2"),
            new("Dondurucu", "3"),
            new("Kuru Depolama", "4"),
            new("Bilinmiyor", "5")
        };
    }
}

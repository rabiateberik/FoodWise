// CarbonReportController, Web arayüzünde karbon raporlarını listeleme ve aylık rapor oluşturma işlemlerini yönetir.
// API ile doğrudan değil, ICarbonReportWebService üzerinden haberleşir.

using FoodWise.Web.Services;
using FoodWise.Web.ViewModels.CarbonReport;
using Microsoft.AspNetCore.Mvc;

namespace FoodWise.Web.Controllers;

public class CarbonReportController : Controller
{
    private readonly ICarbonReportWebService _carbonReportWebService;

    public CarbonReportController(ICarbonReportWebService carbonReportWebService)
    {
        _carbonReportWebService = carbonReportWebService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (string.IsNullOrWhiteSpace(token))
            return RedirectToAction("Login", "Auth");

        await GenerateCurrentMonthReportIfPossibleAsync(token);

        var model = await CreatePageModelAsync(token);

        SetUserViewBag();

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Generate(CarbonReportPageViewModel model)
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (string.IsNullOrWhiteSpace(token))
            return RedirectToAction("Login", "Auth");

        if (!ModelState.IsValid)
        {
            var pageModel = await CreatePageModelAsync(token);
            pageModel.Month = model.Month;
            pageModel.Year = model.Year;

            SetUserViewBag();

            return View("Index", pageModel);
        }

        var result = await _carbonReportWebService.GenerateMonthlyReportAsync(
            model.Month,
            model.Year,
            token
        );

        TempData["SuccessMessage"] = result != null
            ? "Karbon raporu başarıyla oluşturuldu/güncellendi."
            : "Karbon raporu oluşturulurken bir hata oluştu.";

        return RedirectToAction(nameof(Index));
    }

    private async Task GenerateCurrentMonthReportIfPossibleAsync(string token)
    {
        var now = DateTime.Now;

        try
        {
            await _carbonReportWebService.GenerateMonthlyReportAsync(
                now.Month,
                now.Year,
                token
            );
        }
        catch
        {
            // Sayfa açılışında otomatik rapor oluşturma başarısız olursa
            // kullanıcı sayfayı yine görebilsin. Manuel oluşturma butonu hata mesajını gösterebilir.
        }
    }

    private async Task<CarbonReportPageViewModel> CreatePageModelAsync(string token)
    {
        var reports = await _carbonReportWebService.GetMyReportsAsync(token);
        var summary = await _carbonReportWebService.GetSummaryAsync(token);

        return new CarbonReportPageViewModel
        {
            Reports = reports,
            Summary = summary,
            Month = DateTime.Now.Month,
            Year = DateTime.Now.Year
        };
    }

    private void SetUserViewBag()
    {
        ViewBag.FullName = HttpContext.Session.GetString("FullName");
        ViewBag.Email = HttpContext.Session.GetString("Email");
    }
}
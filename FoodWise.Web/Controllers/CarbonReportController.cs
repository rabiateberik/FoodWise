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

        var model = await CreatePageModelAsync(token);

        ViewBag.FullName = HttpContext.Session.GetString("FullName");
        ViewBag.Email = HttpContext.Session.GetString("Email");

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

            return View("Index", pageModel);
        }

        var result = await _carbonReportWebService.GenerateMonthlyReportAsync(model.Month, model.Year, token);

        TempData["SuccessMessage"] = result != null
            ? "Karbon raporu başarıyla oluşturuldu."
            : "Karbon raporu oluşturulurken bir hata oluştu.";

        return RedirectToAction(nameof(Index));
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
}
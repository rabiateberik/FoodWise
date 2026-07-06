
// CarbonReportController, Web arayüzünde karbon raporu sayfasını yönetir.
// Kullanıcının karbon raporlarını listeleme, özet bilgileri gösterme ve aylık rapor oluşturma işlemleri burada yapılır.
// Controller API'ye doğrudan gitmez; tüm API haberleşmesi ICarbonReportWebService üzerinden yürütülür.

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

    // Karbon raporu ana sayfasını açar.
    // Kullanıcı giriş yapmamışsa login sayfasına yönlendirilir.
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (string.IsNullOrWhiteSpace(token))
            return RedirectToAction("Login", "Auth");

        // Sayfa açılırken mevcut ayın raporu oluşturulmaya/güncellenmeye çalışılır.
        await GenerateCurrentMonthReportIfPossibleAsync(token);

        // Rapor listesi, özet bilgiler ve varsayılan ay-yıl bilgileri hazırlanır.
        var model = await CreatePageModelAsync(token);

        SetUserViewBag();

        return View(model);
    }

    // Kullanıcının seçtiği ay ve yıl için karbon raporu oluşturma isteğini alır.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Generate(CarbonReportPageViewModel model)
    {
        var token = HttpContext.Session.GetString("JWToken");

        if (string.IsNullOrWhiteSpace(token))
            return RedirectToAction("Login", "Auth");

        if (!ModelState.IsValid)
        {
            // Form hatalıysa sayfa modeli yeniden hazırlanır.
            // Böylece rapor listesi ve özet alanları boş kalmaz.
            var pageModel = await CreatePageModelAsync(token);
            pageModel.Month = model.Month;
            pageModel.Year = model.Year;

            SetUserViewBag();

            return View("Index", pageModel);
        }

        // Seçilen ay ve yıl bilgisi API'ye gönderilerek rapor oluşturulur veya güncellenir.
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

    // Sayfa açılışında mevcut ay için otomatik rapor oluşturmayı dener.
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
            // Otomatik rapor oluşturma başarısız olsa bile sayfanın açılması engellenmez.
            // Kullanıcı isterse manuel oluşturma işlemini tekrar deneyebilir.
        }
    }

    // Karbon raporu sayfasında kullanılacak ana ViewModel'i hazırlar.
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

    // Layout veya sayfa içinde kullanılacak temel kullanıcı bilgilerini ViewBag'e taşır.
    private void SetUserViewBag()
    {
        ViewBag.FullName = HttpContext.Session.GetString("FullName");
        ViewBag.Email = HttpContext.Session.GetString("Email");
    }
}


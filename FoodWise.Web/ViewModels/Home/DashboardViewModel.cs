// Bu ViewModel, Dashboard ekranında gösterilecek özet verileri taşır.
// Stok, riskli ürün, eco puan ve karbon tasarrufu bilgileri bu model üzerinden ekrana basılır.

namespace FoodWise.Web.ViewModels.Home;

public class DashboardViewModel
{
    public string FullName { get; set; } = "Kullanıcı";

    public string Email { get; set; } = string.Empty;

    public int TotalStockCount { get; set; }

    public int RiskyStockCount { get; set; }

    public decimal CarbonSavedKg { get; set; }

    // Dashboard üzerinde kullanıcının eco puan bilgisini göstermek için kullanılır.
    public int EcoPoint { get; set; }

    public string EcoPointLevelName { get; set; } = string.Empty;
}
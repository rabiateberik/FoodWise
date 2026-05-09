// Bu interface, FoodWise.Web projesinin Stock API ile haberleşmesi için gereken metotları tanımlar.
// Controller doğrudan HttpClient kullanmaz; stok işlemleri bu servis üzerinden yapılır.

using FoodWise.Web.ViewModels.Stock;

namespace FoodWise.Web.Services;

public interface IStockWebService
{
    Task<List<StockItemViewModel>> GetMyStockAsync(string token);

    Task<List<StockItemViewModel>> GetRiskyStockAsync(string token);

    Task<StockItemViewModel?> GetByIdAsync(int id, string token);

    Task<bool> CreateAsync(CreateStockItemViewModel model, string token);

    Task<bool> UpdateAsync(int id, EditStockItemViewModel model, string token);

    Task<bool> DeleteAsync(int id, string token);
}
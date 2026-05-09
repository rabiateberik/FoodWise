// IEcoPointWebService, Web MVC tarafının EcoPoint API endpointleriyle iletişim kurmasını sağlar.

using FoodWise.Web.ViewModels.EcoPoint;

namespace FoodWise.Web.Services;

public interface IEcoPointWebService
{
    Task<EcoPointSummaryViewModel> GetSummaryAsync(string token);

    Task<List<EcoPointHistoryViewModel>> GetHistoryAsync(string token);
}
// Bu interface, FoodWise.Web projesinin Delivery API ile haberleşmesi için gereken metotları tanımlar.
// Controller doğrudan HttpClient kullanmaz; teslimat işlemleri bu servis üzerinden yapılır.

using FoodWise.Web.ViewModels.Delivery;

namespace FoodWise.Web.Services;

public interface IDeliveryWebService
{
    Task<DeliveryViewModel?> CreateDeliveryAsync(int shareRequestId, string token);

    Task<List<DeliveryViewModel>> GetMyDonatedDeliveriesAsync(string token);

    Task<List<DeliveryViewModel>> GetMyReceivedDeliveriesAsync(string token);

    Task<DeliveryViewModel?> MarkAsDroppedOffAsync(DropOffDeliveryViewModel model, string token);

    Task<DeliveryViewModel?> ScanBoxQrAsync(ScanDeliveryBoxViewModel model, string token);

    Task<DeliveryViewModel?> CompleteDeliveryAsync(int deliveryId, string token);
}
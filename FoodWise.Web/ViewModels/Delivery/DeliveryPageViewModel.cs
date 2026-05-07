// Bu ViewModel, Teslimatlar sayfasında bağışlanan ve alınacak teslimatları tek ekranda göstermek için kullanılır.

namespace FoodWise.Web.ViewModels.Delivery;

public class DeliveryPageViewModel
{
    public List<DeliveryViewModel> DonatedDeliveries { get; set; } = new();

    public List<DeliveryViewModel> ReceivedDeliveries { get; set; } = new();

    public ScanDeliveryBoxViewModel ScanModel { get; set; } = new();
}
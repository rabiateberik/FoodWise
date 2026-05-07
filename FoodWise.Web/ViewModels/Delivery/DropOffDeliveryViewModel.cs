// Bu ViewModel, bağışçının ürünü teslim kutusuna bıraktığını işaretlemesi için kullanılır.

namespace FoodWise.Web.ViewModels.Delivery;

public class DropOffDeliveryViewModel
{
    public int DeliveryId { get; set; }

    public string? DropOffImageUrl { get; set; }
}
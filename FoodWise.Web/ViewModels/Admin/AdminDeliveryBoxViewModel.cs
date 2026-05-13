// AdminDeliveryBoxViewModel, admin panelinde teslim kutularını listelemek için kullanılır.

namespace FoodWise.Web.ViewModels.Admin;

public class AdminDeliveryBoxViewModel
{
    public int Id { get; set; }

    public int DeliveryPointId { get; set; }

    public string DeliveryPointName { get; set; } = string.Empty;

    public string? City { get; set; }

    public string? District { get; set; }

    public string? Neighborhood { get; set; }

    public string BoxCode { get; set; } = string.Empty;

    public string QrCodeValue { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsOccupied { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }
}
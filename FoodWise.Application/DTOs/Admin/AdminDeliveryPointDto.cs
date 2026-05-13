// AdminDeliveryPointDto, admin panelinde teslimat noktalarını listelemek için kullanılır.

namespace FoodWise.Application.DTOs.Admin;

public class AdminDeliveryPointDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? City { get; set; }

    public string? District { get; set; }

    public string? Neighborhood { get; set; }

    public double? Latitude { get; set; }

    public double? Longitude { get; set; }

    public string? WorkingHours { get; set; }

    public string? StorageType { get; set; }

    public bool IsActive { get; set; }

    public int DeliveryBoxCount { get; set; }

    public DateTime CreatedAt { get; set; }
}
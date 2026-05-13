// AdminDashboardDto, admin panelinde gösterilecek genel sistem özet verilerini taşır.

namespace FoodWise.Application.DTOs.Admin;

public class AdminDashboardDto
{
    public int TotalUserCount { get; set; }

    public int ActiveUserCount { get; set; }

    public int PassiveUserCount { get; set; }

    public int TotalCategoryCount { get; set; }

    public int TotalProductCount { get; set; }

    public int UserCreatedProductCount { get; set; }

    public int TotalDeliveryPointCount { get; set; }

    public int TotalDeliveryBoxCount { get; set; }

    public int TotalShareListingCount { get; set; }

    public int ActiveShareListingCount { get; set; }

    public int CompletedDeliveryCount { get; set; }

    public int ExpiredDeliveryCount { get; set; }

    public decimal TotalCarbonSavedKg { get; set; }

    public int TotalEcoPoint { get; set; }
}
// AdminProductViewModel, admin panelinde ürünleri listelemek için kullanılır.

namespace FoodWise.Web.ViewModels.Admin;

public class AdminProductViewModel
{
    public int Id { get; set; }

    public int CategoryId { get; set; }

    public string CategoryName { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int DefaultShelfLifeDays { get; set; }

    public int? OpenedShelfLifeDays { get; set; }

    public decimal CarbonFactor { get; set; }

    public bool IsSensitiveFood { get; set; }

    public bool IsSystemDefined { get; set; }

    public bool IsApproved { get; set; }

    public bool IsActive { get; set; }

    public string? CreatedByUserId { get; set; }

    public int StockItemCount { get; set; }

    public DateTime CreatedAt { get; set; }
}
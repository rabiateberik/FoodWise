using FoodWise.Domain.Common;
using FoodWise.Domain.Enums;

namespace FoodWise.Domain.Entities;

public class UserRecipeInteraction : BaseEntity
{
    public string UserId { get; set; } = null!;

    public int RecipeId { get; set; }

    public Recipe Recipe { get; set; } = null!;

    public RecipeInteractionType InteractionType { get; set; }

    // Tarif önerisi riskli bir stok ürünü üzerinden açıldıysa tutulur.
    // Genel tarif önerilerinde null kalabilir.
    public int? StockItemId { get; set; }

    public StockItem? StockItem { get; set; }

    // İleride model eğitimi için kullanıcı o anki öneri skorunu da saklayabiliriz.
    public int? RecommendationScore { get; set; }
}
// EcoPointHistory, kullanıcıların kazandığı eco puan hareketlerini tutar.
// Eco puanlar özellikle tamamlanan teslimat işlemlerinden sonra oluşturulur.

using FoodWise.Domain.Enums;

namespace FoodWise.Domain.Entities;

public class EcoPointHistory
{
    public int Id { get; set; }

    // Puanı kazanan kullanıcının ASP.NET Identity kullanıcı Id bilgisidir.
    // ApplicationUser Infrastructure katmanında olduğu için burada navigation property eklenmedi.
    public string UserId { get; set; } = string.Empty;

    // Kazanılan eco puan miktarıdır.
    public int Point { get; set; }

    // Puanın hangi işlem sonucunda kazanıldığını belirtir.
    public EcoPointActionType ActionType { get; set; }

    // Kullanıcıya gösterilecek açıklama metnidir.
    public string Description { get; set; } = string.Empty;

    // Eco puanın ilişkili olduğu teslimat kaydıdır.
    // Her eco puan teslimata bağlı olmak zorunda olmadığı için nullable bırakıldı.
    public int? DeliveryId { get; set; }

    // Delivery Domain katmanında olduğu için navigation property ekleyebiliriz.
    public Delivery? Delivery { get; set; }

    // Puan kaydının oluşturulma tarihidir.
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
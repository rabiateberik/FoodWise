using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Teslimat sürecindeki durumları temsil eder.
// Pending: Teslimat kaydı oluşturuldu.
// QrGenerated: QR/kutu doğrulama bilgisi hazırlandı.
// DroppedOff: Ürün sahibi ürünü teslim kutusuna bıraktı.
// Delivered: Alıcı ürünü kutudan teslim aldı.
// Expired: Teslimat süresi doldu.
// Cancelled: Teslimat iptal edildi.
namespace FoodWise.Domain.Enums;

public enum DeliveryStatus
{
    Pending = 1,
    QrGenerated = 2,
    DroppedOff = 3,
    Delivered = 4,
    Expired = 5,
    Cancelled = 6
}

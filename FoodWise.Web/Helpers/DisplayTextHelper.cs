// DisplayTextHelper, API'den gelen enum ve durum değerlerini Web arayüzünde Türkçe göstermek için kullanılır.

namespace FoodWise.Web.Helpers;

public static class DisplayTextHelper
{
    public static string GetRiskLevelText(string? riskLevel)
    {
        return riskLevel switch
        {
            "Critical" => "Kritik",
            "High" => "Yüksek",
            "Medium" => "Orta",
            "Low" => "Düşük",
            _ => riskLevel ?? "-"
        };
    }

    public static string GetStorageConditionText(string? storageCondition)
    {
        return storageCondition switch
        {
            "RoomTemperature" => "Oda Sıcaklığı",
            "Refrigerator" => "Buzdolabı",
            "Freezer" => "Dondurucu",
            "DryStorage" => "Kuru Depolama",
            "Unknown" => "Bilinmiyor",
            _ => storageCondition ?? "-"
        };
    }

    // Paylaşım ilanı durumlarını kullanıcıya Türkçe göstermek için kullanılır.
    public static string GetShareListingStatusText(string? status)
    {
        return status switch
        {
            "Available" => "Yayında",
            "Requested" => "Talep Var",
            "Approved" => "Onaylandı",
            "QrGenerated" => "Teslimat Oluşturuldu",
            "Delivered" => "Teslim Edildi",
            "Cancelled" => "İptal Edildi",
            "Expired" => "Süresi Doldu",
            "Reserved" => "Rezerve Edildi",
            _ => status ?? "-"
        };
    }

    public static string GetShareRequestStatusText(string? status)
    {
        return status switch
        {
            "Pending" => "Beklemede",
            "Approved" => "Onaylandı",
            "Rejected" => "Reddedildi",
            "Cancelled" => "İptal Edildi",
            _ => status ?? "-"
        };
    }

    public static string GetDeliveryStatusText(string? status)
    {
        return status switch
        {
            "Pending" => "Beklemede",
            "QrGenerated" => "QR Oluşturuldu",
            "DroppedOff" => "Kutuya Bırakıldı",
            "Delivered" => "Teslim Edildi",
            "Expired" => "Süresi Doldu",
            "Cancelled" => "İptal Edildi",
            _ => status ?? "-"
        };
    }

    public static string GetNotificationTypeText(string? type)
    {
        return type switch
        {
            "RiskWarning" => "Risk Uyarısı",
            "RecipeSuggestion" => "Tarif Önerisi",
            "ShareRequest" => "Paylaşım Talebi",
            "RequestApproved" => "Talep Onaylandı",
            "RequestRejected" => "Talep Reddedildi",
            "DeliveryCreated" => "Teslimat Oluşturuldu",
            "DeliveryDroppedOff" => "Ürün Kutuya Bırakıldı",
            "DeliveryCompleted" => "Teslimat Tamamlandı",
            "Report" => "Rapor",
            "System" => "Sistem",
            _ => type ?? "-"
        };
    }

    public static string GetUnitText(string? unitName)
    {
        return unitName switch
        {
            "Kilogram" => "Kilogram",
            "Gram" => "Gram",
            "Litre" => "Litre",
            "Mililitre" => "Mililitre",
            "Piece" => "Adet",
            "Adet" => "Adet",
            "Package" => "Paket",
            "Paket" => "Paket",
            _ => unitName ?? "-"
        };
    }
}
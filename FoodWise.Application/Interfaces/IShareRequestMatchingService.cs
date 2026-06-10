// IShareRequestMatchingService, paylaşım talebi oluşturulurken kullanıcı ile ilan arasındaki eşleşme skorunu hesaplar.
// İlk aşamada kural tabanlı çalışır; ileride Python/AI modeli ile değiştirilebilir.

using FoodWise.Domain.Entities;

namespace FoodWise.Application.Interfaces;

public interface IShareRequestMatchingService
{
    Task<int> CalculateMatchScoreAsync(string requesterUserId, ShareListing listing);
}
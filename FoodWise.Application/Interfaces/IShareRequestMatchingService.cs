
using FoodWise.Domain.Entities;

namespace FoodWise.Application.Interfaces;

// Paylaşım talebi oluşturulurken kullanıcı ile ilan arasındaki eşleşme skorunu hesaplayan metodu tanımlar.
// İlk aşamada kural tabanlı çalışır, ileride ML modeliyle genişletilebilir.
public interface IShareRequestMatchingService
{
    // Talep eden kullanıcı ile paylaşım ilanı arasındaki uygunluk skorunu hesaplar.
    Task<int> CalculateMatchScoreAsync(string requesterUserId, ShareListing listing);
}


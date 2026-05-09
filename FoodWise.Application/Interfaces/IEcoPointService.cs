using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// IEcoPointService, eco puan geçmişi oluşturma ve kullanıcı eco puan bilgilerini getirme işlemlerini tanımlar.

using FoodWise.Application.DTOs.EcoPoint;
using FoodWise.Domain.Enums;

namespace FoodWise.Application.Interfaces;

public interface IEcoPointService
{
    Task AddPointAsync(
        string userId,
        int point,
        EcoPointActionType actionType,
        string description,
        int? deliveryId = null);

    Task<EcoPointSummaryDto> GetSummaryAsync(string userId);

    Task<List<EcoPointHistoryDto>> GetHistoryAsync(string userId);
}
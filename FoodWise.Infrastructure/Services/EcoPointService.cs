using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// EcoPointService, kullanıcıların eco puan kazanma geçmişini yönetir.
// Toplam eco puanı, seviye bilgisini ve puan geçmişini veritabanından hesaplar.

using FoodWise.Application.DTOs.EcoPoint;
using FoodWise.Application.Interfaces;
using FoodWise.Domain.Entities;
using FoodWise.Domain.Enums;
using FoodWise.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FoodWise.Infrastructure.Services;

public class EcoPointService : IEcoPointService
{
    private readonly FoodWiseDbContext _context;

    public EcoPointService(FoodWiseDbContext context)
    {
        _context = context;
    }

    public async Task AddPointAsync(
        string userId,
        int point,
        EcoPointActionType actionType,
        string description,
        int? deliveryId = null)
    {
        if (string.IsNullOrWhiteSpace(userId) || point <= 0)
            return;

        // Aynı teslimat için aynı kullanıcıya aynı işlem türünden tekrar puan verilmesini engeller.
        // Örneğin teslimat tamamlama işlemi yanlışlıkla iki kez tetiklenirse çift puan yazılmaz.
        if (deliveryId.HasValue)
        {
            var alreadyExists = await _context.EcoPointHistories.AnyAsync(x =>
                x.UserId == userId &&
                x.DeliveryId == deliveryId.Value &&
                x.ActionType == actionType);

            if (alreadyExists)
                return;
        }

        var history = new EcoPointHistory
        {
            UserId = userId,
            Point = point,
            ActionType = actionType,
            Description = description,
            DeliveryId = deliveryId,
            CreatedAt = DateTime.UtcNow
        };

        await _context.EcoPointHistories.AddAsync(history);
        await _context.SaveChangesAsync();
    }

    public async Task<EcoPointSummaryDto> GetSummaryAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return new EcoPointSummaryDto
            {
                TotalPoint = 0,
                LevelName = GetLevelName(0),
                HistoryCount = 0
            };
        }

        var histories = await _context.EcoPointHistories
            .Where(x => x.UserId == userId)
            .ToListAsync();

        var totalPoint = histories.Sum(x => x.Point);

        return new EcoPointSummaryDto
        {
            TotalPoint = totalPoint,
            LevelName = GetLevelName(totalPoint),
            HistoryCount = histories.Count
        };
    }

    public async Task<List<EcoPointHistoryDto>> GetHistoryAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return new List<EcoPointHistoryDto>();

        return await _context.EcoPointHistories
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new EcoPointHistoryDto
            {
                Id = x.Id,
                Point = x.Point,
                ActionType = x.ActionType,
                ActionTypeText = GetActionTypeText(x.ActionType),
                Description = x.Description,
                DeliveryId = x.DeliveryId,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync();
    }

    private static string GetLevelName(int totalPoint)
    {
        return totalPoint switch
        {
            >= 500 => "Eco Kahraman",
            >= 250 => "Sürdürülebilirlik Elçisi",
            >= 100 => "Yeşil Kullanıcı",
            >= 50 => "Yeşil Adım",
            _ => "Eco Başlangıç"
        };
    }

    private static string GetActionTypeText(EcoPointActionType actionType)
    {
        return actionType switch
        {
            EcoPointActionType.StockAdded => "Stok Eklendi",
            EcoPointActionType.ShareListingCreated => "Paylaşım İlanı Oluşturuldu",
            EcoPointActionType.ShareRequestApproved => "Paylaşım Talebi Onaylandı",
            EcoPointActionType.DeliveryCompletedAsDonor => "Bağış Teslimatı Tamamlandı",
            EcoPointActionType.DeliveryCompletedAsReceiver => "Teslim Alma Tamamlandı",
            EcoPointActionType.CarbonSavingBonus => "Karbon Tasarrufu Bonusu",
            _ => "Eco Puan İşlemi"
        };
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Stok işlemlerinin sözleşmesidir.
// Controller doğrudan veritabanına erişmez, bu interface üzerinden servis katmanına ulaşır.
using FoodWise.Application.DTOs.Stock;

namespace FoodWise.Application.Interfaces;

public interface IStockService
{
    Task<List<StockItemDto>> GetUserStockAsync(string userId);

    Task<List<StockItemDto>> GetRiskyStockItemsAsync(string userId);

    Task<StockItemDto?> GetByIdAsync(int id, string userId);

    Task<StockItemDto> CreateAsync(string userId, CreateStockItemDto model);

    Task<StockItemDto?> UpdateAsync(int id, string userId, UpdateStockItemDto model);

    Task<bool> DeleteAsync(int id, string userId);
}
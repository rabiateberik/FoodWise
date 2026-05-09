using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// EcoPointHistoryDto, kullanıcının eco puan geçmişini API ve Web tarafına taşımak için kullanılır.

using FoodWise.Domain.Enums;

namespace FoodWise.Application.DTOs.EcoPoint;

public class EcoPointHistoryDto
{
    public int Id { get; set; }

    public int Point { get; set; }

    public EcoPointActionType ActionType { get; set; }

    public string ActionTypeText { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public int? DeliveryId { get; set; }

    public DateTime CreatedAt { get; set; }
}
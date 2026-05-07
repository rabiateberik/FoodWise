using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Ürün sahibi ürünü teslim kutusuna bıraktığında gönderilecek isteği temsil eder.
namespace FoodWise.Application.DTOs.Delivery;

public class DropOffDeliveryDto
{
    public string? DropOffImageUrl { get; set; }
}
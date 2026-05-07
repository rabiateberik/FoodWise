using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Alıcı kullanıcının kutudaki QR kodu okuttuğunda göndereceği QR değerini temsil eder.
namespace FoodWise.Application.DTOs.Delivery;

public class ScanDeliveryBoxDto
{
    public string QrCodeValue { get; set; } = null!;
}
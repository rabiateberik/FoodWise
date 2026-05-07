using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Teslimat ve QR destekli kutu doğrulama işlemlerinin servis sözleşmesidir.
using FoodWise.Application.DTOs.Delivery;

namespace FoodWise.Application.Interfaces;

public interface IDeliveryService
{
    Task<DeliveryDto?> CreateDeliveryAsync(string donorUserId, int shareRequestId);

    Task<DeliveryDto?> MarkAsDroppedOffAsync(string donorUserId, int deliveryId, DropOffDeliveryDto model);

    Task<DeliveryDto?> ScanBoxQrAsync(string receiverUserId, ScanDeliveryBoxDto model);

    Task<DeliveryDto?> CompleteDeliveryAsync(string receiverUserId, int deliveryId);

    Task<List<DeliveryDto>> GetMyDonatedDeliveriesAsync(string donorUserId);

    Task<List<DeliveryDto>> GetMyReceivedDeliveriesAsync(string receiverUserId);
}
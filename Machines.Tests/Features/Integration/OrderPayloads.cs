#nullable enable
using System;

namespace FastFsm.Tests.Features.Integration;

public class OrderPayload
{
    public int OrderId { get; set; }
    public decimal Amount { get; set; }
    public string? TrackingNumber { get; set; }
}

public class PaymentPayload : OrderPayload
{
    public string PaymentMethod { get; set; } = string.Empty;
    public DateTime PaymentDate { get; set; }
}

public class ShippingPayload : OrderPayload
{
    public string Carrier { get; set; } = string.Empty;
    public DateTime EstimatedDelivery { get; set; }
}

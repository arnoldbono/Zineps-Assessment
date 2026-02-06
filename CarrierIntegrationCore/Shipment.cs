namespace CarrierIntegrationCore;

public class Shipment
{
    public string? Id { get; set; }
    public required string Carrier { get; set; } // e.g., "DHL", "FedEx", "UPS"
    public required string TrackingNumber { get; set; }
    public required double Amount { get; set; } // weight in kg
    public required string Zone { get; set; } // e.g., "NL", "EU", "INT"
}

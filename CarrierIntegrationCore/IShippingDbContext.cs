namespace CarrierIntegrationCore;

public interface IShippingDbContext
{
    TokenInfo Authenticate(string username, string password);

    string? GetUsernameFromToken(string token);

    Account? FindAccount(string username);

    bool Logout(string username);

    Shipment AddShipment(Shipment shipment);

    Shipment? GetShipment(string shipmentId);

    Shipment? GetShipmentByTrackingNumber(string trackingNumber);

    Shipment[] GetShipments();

    ShipmentLabel AddShipmentLabel(ShipmentLabel shipmentLabel);

    ShipmentLabel[] GetShipmentLabels(Shipment shipment);
}

public record TokenInfo
{
    public required string Token { get; init; }

    public DateTime Expiry { get; init; }

    public bool IsValid => Expiry > DateTime.UtcNow;
    
    public static TokenInfo Invalid => new() { Token = string.Empty, Expiry = DateTime.MinValue };
}

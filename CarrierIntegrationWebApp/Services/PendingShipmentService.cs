namespace CarrierIntegrationWebApp.Services;

public class PendingShipmentService
{
    public PendingShipment? PendingData { get; set; }
    
    public void StorePendingShipment(string carrier, string trackingNumber, double amount, string zone)
    {
        PendingData = new PendingShipment
        {
            Carrier = carrier,
            TrackingNumber = trackingNumber,
            Amount = amount,
            Zone = zone
        };
    }
    
    public PendingShipment? RetrieveAndClear()
    {
        var data = PendingData;
        PendingData = null;
        return data;
    }
}

public class PendingShipment
{
    public string Carrier { get; set; } = "";
    public string TrackingNumber { get; set; } = "";
    public double Amount { get; set; }
    public string Zone { get; set; } = "";
}

namespace DiscrepancyReportProducer;

public class ChargeItem : LineItem
{
    public void ImportFromJson(dynamic json)
    {
        this.TrackingNumber = json.trackingNumber;
        this.Amount = json.amount;
        this.Weight = json.weight;
        this.Zone = json.zone;
    }
}

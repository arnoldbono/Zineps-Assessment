namespace DiscrepancyReportProducer;

public class InvoiceItem : LineItem
{
    public void ImportFromJson(dynamic json)
    {
        this.TrackingNumber = json.trackingNumber;
        this.Amount = json.billedAmount;
        this.Weight = json.billedWeight;
        this.Zone = json.zone;
    }
}

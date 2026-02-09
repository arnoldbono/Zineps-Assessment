namespace DiscrepancyReportProducer;

public class DiscrepancyFinderAmount : IDiscrepancyFinder
{
    public string FindDiscrepancy(LineItem invoice, LineItem charge)
    {
        if (invoice.Amount != charge.Amount)
        {
            return $"Amount mismatch for Tracking Number {invoice.TrackingNumber}: Invoice({invoice.Amount}) vs Charge({charge.Amount})";
        }

        return string.Empty;
    }
}

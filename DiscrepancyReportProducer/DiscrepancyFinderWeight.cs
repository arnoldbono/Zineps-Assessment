namespace DiscrepancyReportProducer;

public class DiscrepancyFinderWeight : IDiscrepancyFinder
{
    public string FindDiscrepancy(LineItem invoice, LineItem charge)
    {
        if (invoice.Weight != charge.Weight)
        {
            return $"Weight mismatch for Tracking Number {invoice.TrackingNumber}: Invoice({invoice.Weight}) vs Charge({charge.Weight})";
        }

        return string.Empty;
    }
}

namespace DiscrepancyReportProducer;

public class LineItem
{
    public required string TrackingNumber { get; set; }
    public required decimal Amount { get; set; }
    public required double Weight { get; set; }
    public required string Zone { get; set; }
}

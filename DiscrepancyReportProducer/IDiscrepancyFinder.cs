namespace DiscrepancyReportProducer;

public interface IDiscrepancyFinder
{
    public string FindDiscrepancy(LineItem invoice, LineItem charge);
}

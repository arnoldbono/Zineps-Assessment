namespace CarrierIntegrationCore;

public class ShipmentLabel
{
    public string? Id { get; set; }
    public required string ShipmentId { get; set; }
    public required byte[] LabelData { get; set; } // PDF or base64 image data
    public required string Format { get; set; } // e.g., "PDF", "PNG"
}
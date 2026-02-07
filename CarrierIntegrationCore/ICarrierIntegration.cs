namespace CarrierIntegrationCore;

using Microsoft.AspNetCore.Http;

public interface ICarrierIntegration
{
    IResult Authenticate(string username, string password);

    IResult FindAccount(string token);

    IResult Logout(string token);

    IResult AddShipment(string token, Shipment shipment);

    IResult GetShipment(string token, string shipmentId);

    IResult GetShipments(string token);

    IResult AddShipmentLabel(string token, string shipmentId, IFormFile? labelFile = null);

    IResult GetShipmentLabels(string token, string trackingNumber);
}

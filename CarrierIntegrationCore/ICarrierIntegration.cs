namespace CarrierIntegrationCore;

using Microsoft.AspNetCore.Http;

public interface ICarrierIntegration
{
    IResult Authenticate(string username, string password);

    IResult FindAccount(string token);

    IResult Logout(string token);

    IResult AddShipment(string token, Shipment shipment);

    IResult GetShipmentLabels(string token, string trackingNumber);
}

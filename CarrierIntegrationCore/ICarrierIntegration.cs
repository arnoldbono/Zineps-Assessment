namespace CarrierIntegrationCore;

using Microsoft.AspNetCore.Http;

public interface ICarrierIntegration
{
    IResult Authenticate(string username, string password);

    IResult FindAccount(string token);

    IResult Logout(string token);
}

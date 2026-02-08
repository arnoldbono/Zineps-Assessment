namespace CarrierIntegrationModel;

using Microsoft.AspNetCore.Http;

using CarrierIntegrationCore;

public class CarrierIntegration(IShippingDbContext dbContext) : ICarrierIntegration
{
    public readonly IShippingDbContext DbContext = dbContext;

    public IResult Authenticate(string username, string password)
    {
        // Validate required fields
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            return Results.BadRequest(new { error = "Username and password are required" });
        }

        var tokenInfo = DbContext.Authenticate(username, password);
        if (tokenInfo != TokenInfo.Invalid)
        {
            var token = tokenInfo.Token;
            return Results.Ok(new TokenResponse(token, "Bearer", tokenInfo.Expiry.Subtract(DateTime.UtcNow).Seconds));
        }

        return Results.Unauthorized();
    }

    public IResult FindAccount(string token)
    {
        var username = DbContext.GetUsernameFromToken(token);
        if (username == null)
        {
            return Results.Unauthorized();
        }

        var account = DbContext.FindAccount(username);
        if (account != null)
        {
            return Results.Ok(new AccountResponse(account));
        }

        return Results.Unauthorized();
    }

    public IResult Logout(string token)
    {
        var username = DbContext.GetUsernameFromToken(token);
        if (username == null)
        {
            return Results.Unauthorized();
        }

        if (DbContext.Logout(username))
        {
            return Results.Ok(new { message = "Logout successful" });
        }

        return Results.Unauthorized();
    }

    public IResult AddShipment(string token, Shipment shipment)
    {
        var username = DbContext.GetUsernameFromToken(token);
        if (username == null)
        {
            return Results.Unauthorized();
        }

        shipment = DbContext.AddShipment(shipment);
        return Results.Ok(shipment);
    }

    public IResult GetShipment(string token, string shipmentId)
    {
        var username = DbContext.GetUsernameFromToken(token);
        if (username == null)
        {
            return Results.Unauthorized();
        }

        var shipment = DbContext.GetShipment(shipmentId);
        if (shipment != null)
        {
            return Results.Ok(shipment);
        }

        return Results.NotFound(new { error = "Shipment not found" });
    }

    public IResult GetShipments(string token)
    {
        var username = DbContext.GetUsernameFromToken(token);
        if (username == null)
        {
            return Results.Unauthorized();
        }

        var shipments = DbContext.GetShipments();
        return Results.Ok(shipments);
    }

    // Assessment does not specify whether the LabelData should be generated or uploaded.
    public IResult AddShipmentLabel(string token, string shipmentId, IFormFile? labelFile = null)
    {
        var username = DbContext.GetUsernameFromToken(token);
        if (username == null)
        {
            return Results.Unauthorized();
        }

        var shipment = DbContext.GetShipment(shipmentId);
        if (shipment != null)
        {
            byte[] labelData = [];
            string format = "PDF";
            if (labelFile != null)
            {
                using var memoryStream = new MemoryStream();
                labelFile.CopyTo(memoryStream);
                labelData = memoryStream.ToArray();
                format = labelFile?.ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase) == true ? "PDF" : "PNG";
            }
            
            var label = new ShipmentLabel
            {
                ShipmentId = shipment.Id!,
                LabelData = labelData,
                Format = format
            };

            label = DbContext.AddShipmentLabel(label);
            return Results.Ok(label);
        }

        return Results.NotFound(new { error = "Shipment not found" });
    }

    public IResult GetShipmentLabels(string token, string trackingNumber)
    {
        var username = DbContext.GetUsernameFromToken(token);
        if (username == null)
        {
            return Results.Unauthorized();
        }

        var shipment = DbContext.GetShipmentByTrackingNumber(trackingNumber);
        if (shipment != null)
        {
            var labels = DbContext.GetShipmentLabels(shipment);
            return Results.Ok(labels);
        }

        return Results.NotFound(new { error = "Tracknumber not found" });
    }

    public IResult GetShipmentLabelsByShipmentId(string token, string shipmentId)
    {
        var username = DbContext.GetUsernameFromToken(token);
        if (username == null)
        {
            return Results.Unauthorized();
        }

        var shipment = DbContext.GetShipment(shipmentId);
        if (shipment != null)
        {
            var labels = DbContext.GetShipmentLabels(shipment);
            return Results.Ok(labels);
        }

        return Results.NotFound(new { error = "Shipment not found" });
    }
}

public record TokenResponse(string AccessToken, string TokenType, int ExpiresIn);

public record AccountResponse(Account Account);

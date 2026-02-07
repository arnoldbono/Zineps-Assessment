namespace CarrierIntegrationModel;

using Microsoft.AspNetCore.Http;

using CarrierIntegrationCore;

public class CarrierIntegration : ICarrierIntegration
{
    // In-memory storage for user credentials (For demonstration purposes only. In production, use a secure database and hashing for passwords)
    private static Dictionary<string, string> _userStore = new()
    {
        { "admin", "password" },
        { "user1", "pass123" },
        { "demo", "demo123" }
    };

    // In-memory storage for Account
    private static Dictionary<string, Account> _accounts = new()
    {
        { "admin", new Account { Id = Guid.NewGuid().ToString(), UserName = "admin", Name = "Khosrou (Khoos)", Surname = "Golzad" } },
        { "user1", new Account { Id = Guid.NewGuid().ToString(), UserName = "user1", Name = "Dirk Jan", Surname = "van Lonkhuyzen" } },
        { "demo", new Account { Id = Guid.NewGuid().ToString(), UserName = "demo", Name = "Mani", Surname = "Singh" } }
    };

    private static Dictionary<Guid, string> _tokenUserMap = []; // Maps token GUIDs to usernames
    private static Dictionary<Guid, DateTime> _tokenExpiryMap = []; // Maps token GUIDs to expiration times
    private static Dictionary<string, Guid> _userTokenMap = []; // Maps usernames to token GUIDs
    private static Dictionary<Guid, ShipmentLabel> _shipmentLabels = []; // Maps shipment label IDs to labels
    private static Dictionary<Guid, Shipment> _shipments = []; // Maps shipment IDs to shipments

    public IResult Authenticate(string username, string password)
    {
        // Validate required fields
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            return Results.BadRequest(new { error = "Username and password are required" });
        }

        // Validate against in-memory user store
        if (_userStore.TryGetValue(username, out var storedPassword) && 
            storedPassword == password)
        {
            if (_userTokenMap.TryGetValue(username, out var existingToken))
            {
                _tokenUserMap.Remove(existingToken);
                _tokenExpiryMap.Remove(existingToken);
                _userTokenMap.Remove(username);
            }

            var tokenGuid = Guid.NewGuid();
            _tokenUserMap[tokenGuid] = username;
            _tokenExpiryMap[tokenGuid] = DateTime.UtcNow.AddHours(1); // Token valid for 1 hour
            _userTokenMap[username] = tokenGuid;

            var token = Convert.ToBase64String(tokenGuid.ToByteArray());
            return Results.Ok(new TokenResponse(token, "Bearer", 3600));
        }

        return Results.Unauthorized();
    }

    private static string? GetUsernameFromToken(string token)
    {
        var tokenBytes = new byte[16];
        if (string.IsNullOrEmpty(token) || Convert.TryFromBase64String(token, tokenBytes, out int bytesWritten) == false || bytesWritten != 16)
        {
            return null;
        }

        var tokenGuid = new Guid(tokenBytes);
        if (_tokenExpiryMap.TryGetValue(tokenGuid, out var expiry) && expiry < DateTime.UtcNow)
        {
            // Token has expired, remove it from the maps
            var expiredUsername = _tokenUserMap[tokenGuid];
            _userTokenMap.Remove(expiredUsername);
            _tokenUserMap.Remove(tokenGuid);
            _tokenExpiryMap.Remove(tokenGuid);
            return null;
        }

        return _tokenUserMap.TryGetValue(tokenGuid, out var username) ? username : null;
    }

    public IResult FindAccount(string token)
    {
        var username = GetUsernameFromToken(token);
        if (username == null)
        {
            return Results.Unauthorized();
        }

        if (_accounts.TryGetValue(username, out var account))
        {
            return Results.Ok(account);
        }

        return Results.Unauthorized();
    }

    public IResult Logout(string token)
    {
        var username = GetUsernameFromToken(token);
        if (username == null)
        {
            return Results.Unauthorized();
        }

        if (_userTokenMap.TryGetValue(username, out var tokenGuid))
        {
            _tokenUserMap.Remove(tokenGuid);
            _tokenExpiryMap.Remove(tokenGuid);
            _userTokenMap.Remove(username);
            return Results.Ok(new { message = "Logout successful" });
        }

        return Results.Unauthorized();
    }

    public IResult AddShipment(string token, Shipment shipment)
    {
        var username = GetUsernameFromToken(token);
        if (username == null)
        {
            return Results.Unauthorized();
        }

        var id = Guid.NewGuid();
        shipment.Id = id.ToString();
        if (string.IsNullOrEmpty(shipment.TrackingNumber))
        {
            shipment.TrackingNumber = $"TRACK-{id.ToString()[..8].ToUpper()}";
        }
        _shipments[id] = shipment;
        return Results.Ok(shipment);
    }

    public IResult GetShipment(string token, string shipmentId)
    {
        var username = GetUsernameFromToken(token);
        if (username == null)
        {
            return Results.Unauthorized();
        }

        if (Guid.TryParse(shipmentId, out var shipmentGuid) && _shipments.TryGetValue(shipmentGuid, out var shipment))
        {
            return Results.Ok(shipment);
        }

        return Results.NotFound(new { error = "Shipment not found" });
    }

    public IResult GetShipments(string token)
    {
        var username = GetUsernameFromToken(token);
        if (username == null)
        {
            return Results.Unauthorized();
        }

        var shipments = _shipments.Values.ToList();
        return Results.Ok(shipments);
    }

    // Assessment does not specify whether the LabelData should be generated or uploaded.
    public IResult AddShipmentLabel(string token, string shipmentId, IFormFile? labelFile = null)
    {
        var username = GetUsernameFromToken(token);
        if (username == null)
        {
            return Results.Unauthorized();
        }
    
        if (Guid.TryParse(shipmentId, out var shipmentGuid) && _shipments.TryGetValue(shipmentGuid, out var shipment))
        {
            var labelId = Guid.NewGuid();
            
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
                Id = labelId.ToString(),
                ShipmentId = shipment.Id!,
                LabelData = labelData,
                Format = format
            };
            _shipmentLabels[labelId] = label;
            return Results.Ok(label);
        }

        return Results.NotFound(new { error = "Shipment not found" });
    }

    public IResult GetShipmentLabels(string token, string trackingNumber)
    {
        var username = GetUsernameFromToken(token);
        if (username == null)
        {
            return Results.Unauthorized();
        }

        var shipments = _shipments.Values.FirstOrDefault(s => s.TrackingNumber == trackingNumber);
        if (shipments != null)
        {
            var labels = _shipmentLabels.Values.Where(l => l.ShipmentId == shipments.Id).ToList();
            return Results.Ok(labels);
        }

        return Results.NotFound(new { error = "Tracknumber not found" });
    }

    public IResult GetShipmentLabelsByShipmentId(string token, string shipmentId)
    {
        var username = GetUsernameFromToken(token);
        if (username == null)
        {
            return Results.Unauthorized();
        }

        if (Guid.TryParse(shipmentId, out var shipmentGuid) && _shipments.ContainsKey(shipmentGuid))
        {
            var labels = _shipmentLabels.Values.Where(l => l.ShipmentId == shipmentId).ToList();
            return Results.Ok(labels);
        }

        return Results.NotFound(new { error = "Shipment not found" });
    }
}

public record TokenResponse(string AccessToken, string TokenType, int ExpiresIn);

public record AccountResponse(Account Account);

namespace CarrierIntegrationModel;

using CarrierIntegrationCore;

public class ShippingDbContext : IShippingDbContext
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

    private object _lock = new();

    public TokenInfo Authenticate(string username, string password)
    {
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            return TokenInfo.Invalid;
        }

        // Validate against in-memory user store
        if (_userStore.TryGetValue(username, out var storedPassword) && 
            storedPassword == password)
        {
            lock (_lock)
            {
                if (_userTokenMap.TryGetValue(username, out var existingToken))
                {
                    _tokenUserMap.Remove(existingToken);
                    _tokenExpiryMap.Remove(existingToken);
                    _userTokenMap.Remove(username);
                }
            }

            var tokenGuid = Guid.NewGuid();
            var expiry = DateTime.UtcNow.AddHours(1); // Token valid for 1 hour

            lock (_lock)
            {   
                _tokenUserMap[tokenGuid] = username;
                _tokenExpiryMap[tokenGuid] = expiry;
                _userTokenMap[username] = tokenGuid;
            }

            return new TokenInfo
            {
                Token = Convert.ToBase64String(tokenGuid.ToByteArray()),
                Expiry = expiry
            };
        }

        return TokenInfo.Invalid;
    }

    private static Guid GetTokenGuidFromToken(string token)
    {
        var tokenBytes = new byte[16];
        if (string.IsNullOrEmpty(token) || Convert.TryFromBase64String(token, tokenBytes, out int bytesWritten) == false || bytesWritten != 16)
        {
            return Guid.Empty;
        }

        return new Guid(tokenBytes);
    }

    public string? GetUsernameFromToken(string token)
    {
        var tokenGuid = GetTokenGuidFromToken(token);

        string? username = null;

        lock (_lock)
        {
            if (_tokenExpiryMap.TryGetValue(tokenGuid, out var expiry) &&
                new TokenInfo { Token = token, Expiry = expiry }.IsValid == false)
            {
                // Token has expired, remove it from the maps
                var expiredUsername = _tokenUserMap[tokenGuid];
                _userTokenMap.Remove(expiredUsername);
                _tokenUserMap.Remove(tokenGuid);
                _tokenExpiryMap.Remove(tokenGuid);
                return null;
            }

            _tokenUserMap.TryGetValue(tokenGuid, out username);
        }

        return username;
    }

    public Account? FindAccount(string username)
    {
        Account? account = null;

        lock (_lock)
        {
            _accounts.TryGetValue(username, out account);
        }   

        return account;
    }

    public bool Logout(string username)
    {
        lock (_lock)
        {
            if (_userTokenMap.TryGetValue(username, out var tokenGuid))
            {
                _tokenUserMap.Remove(tokenGuid);
                _tokenExpiryMap.Remove(tokenGuid);
                _userTokenMap.Remove(username);
                return true;
            }
        }

        return false;
    }

    public Shipment AddShipment(Shipment shipment)
    {
        Guid id;
        if (shipment.Id == null || !Guid.TryParse(shipment.Id, out id))
        {
            id = Guid.NewGuid();
            shipment.Id = id.ToString();
        }

        if (string.IsNullOrEmpty(shipment.TrackingNumber))
        {
            shipment.TrackingNumber = $"TRACK-{id.ToString()[..8].ToUpper()}";
        }

        lock (_lock)
        {
            // Skipping the test that id is not already in use for simplicity.
            _shipments[id] = shipment;
        }

        return shipment;
    }

    public Shipment? GetShipment(string shipmentId)
    {
         lock (_lock)
         {
             if (Guid.TryParse(shipmentId, out var shipmentGuid) && _shipments.TryGetValue(shipmentGuid, out var shipment))
             {
                 return shipment;
             }
         }

         return null;
    }

    public Shipment? GetShipmentByTrackingNumber(string trackingNumber)
    {
        lock (_lock)
        {
            return _shipments.Values.FirstOrDefault(s => s.TrackingNumber == trackingNumber);
        }
    }

    public Shipment[] GetShipments()
    {
        lock (_lock)
        {
            return [.. _shipments.Values];
        }
    }

    public ShipmentLabel AddShipmentLabel(ShipmentLabel shipmentLabel)
    {
        var labelId = Guid.NewGuid();
        shipmentLabel.Id = labelId.ToString();

        lock (_lock)
        {
            _shipmentLabels[labelId] = shipmentLabel;
        }

        return shipmentLabel;
    }

    public ShipmentLabel[] GetShipmentLabels(Shipment shipment)
    {
        lock (_lock)
        {
            var shipmentId = shipment.Id!;
            return [.. _shipmentLabels.Values.Where(l => l.ShipmentId == shipmentId)];
        }
    }
}

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
}

public record TokenResponse(string AccessToken, string TokenType, int ExpiresIn);

public record AccountResponse(Account Account);

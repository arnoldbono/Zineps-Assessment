namespace CarrierIntegrationModel;

using Microsoft.AspNetCore.Http;

using CarrierIntegrationCore;

public class CarrierIntegration : ICarrierIntegration
{
    // In-memory storage for user credentials
    private static Dictionary<string, string> _userStore = new()
    {
        { "admin", "password" },
        { "user1", "pass123" },
        { "demo", "demo123" }
    };

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
            var token = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
            return Results.Ok(new TokenResponse(token, "Bearer", 3600));
        }

        return Results.Unauthorized();
    }
}

public record TokenResponse(string AccessToken, string TokenType, int ExpiresIn);

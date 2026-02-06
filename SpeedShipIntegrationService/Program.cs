var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Add CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazorApp", policy =>
    {
        policy.WithOrigins("https://localhost:7020", "http://localhost:7016")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

var app = builder.Build();

// In-memory storage for user credentials
var userStore = new Dictionary<string, string>
{
    { "admin", "password" },
    { "user1", "pass123" },
    { "demo", "demo123" }
};

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseCors("AllowBlazorApp");

app.MapPost("/auth/token", (TokenRequest request) =>
{
    // Validate required fields
    if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
    {
        return Results.BadRequest(new { error = "Username and password are required" });
    }

    // Validate against in-memory user store
    if (userStore.TryGetValue(request.Username, out var storedPassword) && 
        storedPassword == request.Password)
    {
        var token = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        return Results.Ok(new TokenResponse(token, "Bearer", 3600));
    }

    return Results.Unauthorized();
})
.WithName("AuthToken");

app.Run();

record TokenRequest(string Username, string Password);

record TokenResponse(string AccessToken, string TokenType, int ExpiresIn);


using CarrierIntegrationModel;
using CarrierIntegrationCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// This dependency injection is normally outside this service, but for the sake of this exercise, we will register it here.
// In a real-world application, you would typically have a separate project, called CarrierIntegrationAddin, say,
// for the implementation and register it in the composition root of your application.
builder.Services.AddScoped<ICarrierIntegration, CarrierIntegration>();

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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseCors("AllowBlazorApp");

app.MapPost("/auth/token", (TokenRequest request, ICarrierIntegration carrierIntegration) =>
{
    return carrierIntegration.Authenticate(request.Username, request.Password);
})
.WithName("AuthToken");

app.MapPost("/logout", (LogoutRequest request, ICarrierIntegration carrierIntegration) =>
{
    return carrierIntegration.Logout(request.Token);
})
.WithName("Logout");

app.Run();

record TokenRequest(string Username, string Password);

record LogoutRequest(string Token);

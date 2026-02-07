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

app.MapPost("/shipment/add", (AddShipmentRequest request, HttpContext context, ICarrierIntegration carrierIntegration) =>
{
    var token = context.Request.Headers.Authorization.FirstOrDefault()?.Replace("Bearer ", "");
    if (string.IsNullOrEmpty(token))
    {
        return Results.Unauthorized();
    }
    return carrierIntegration.AddShipment(token, request.Shipment);
})
.WithName("AddShipment");

app.MapGet("/shipments", (HttpContext context, ICarrierIntegration carrierIntegration) =>
{
    var token = context.Request.Headers.Authorization.FirstOrDefault()?.Replace("Bearer ", "");
    if (string.IsNullOrEmpty(token))
    {
        return Results.Unauthorized();
    }
    return carrierIntegration.GetShipments(token);
})
.WithName("GetShipments");

app.MapPost("/shipment/labels", (GetShipmentLabelsRequest request, HttpContext context, ICarrierIntegration carrierIntegration) =>
{
    var token = context.Request.Headers.Authorization.FirstOrDefault()?.Replace("Bearer ", "");
    if (string.IsNullOrEmpty(token))
    {
        return Results.Unauthorized();
    }
    return carrierIntegration.GetShipmentLabels(token, request.TrackingNumber);
})
.WithName("GetShipmentLabels");

app.MapGet("/shipment/{shipmentId}/labels", (string shipmentId, HttpContext context, ICarrierIntegration carrierIntegration) =>
{
    var token = context.Request.Headers.Authorization.FirstOrDefault()?.Replace("Bearer ", "");
    if (string.IsNullOrEmpty(token))
    {
        return Results.Unauthorized();
    }
    return carrierIntegration.GetShipmentLabelsByShipmentId(token, shipmentId);
})
.WithName("GetShipmentLabelsByShipmentId");

app.MapPost("/shipment/label/create", async (HttpContext context, ICarrierIntegration carrierIntegration) =>
{
    var token = context.Request.Headers.Authorization.FirstOrDefault()?.Replace("Bearer ", "");
    if (string.IsNullOrEmpty(token))
    {
        return Results.Unauthorized();
    }

    var form = await context.Request.ReadFormAsync();
    var shipmentId = form["shipmentId"].ToString();
    var labelFile = form.Files["labelFile"];

    if (string.IsNullOrEmpty(shipmentId))
    {
        return Results.BadRequest(new { error = "Shipment ID is required" });
    }

    return carrierIntegration.AddShipmentLabel(token, shipmentId, labelFile);
})
.WithName("CreateShipmentLabel")
.DisableAntiforgery();

app.Run();

record TokenRequest(string Username, string Password);

record LogoutRequest(string Token);

record AddShipmentRequest(Shipment Shipment);

record GetShipmentLabelsRequest(string Token, string TrackingNumber);

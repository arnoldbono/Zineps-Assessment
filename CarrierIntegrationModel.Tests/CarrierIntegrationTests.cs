namespace CarrierIntegrationModel.Tests;

using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;

using CarrierIntegrationCore;

public class CarrierIntegrationTests
{
    private readonly Mock<IShippingDbContext> _mockDbContext;
    private readonly CarrierIntegration _carrierIntegration;

    public CarrierIntegrationTests()
    {
        _mockDbContext = new Mock<IShippingDbContext>();
        _carrierIntegration = new CarrierIntegration(_mockDbContext.Object);
    }

    #region Authenticate Tests

    [Fact]
    public void Authenticate_ValidCredentials_ReturnsOkWithToken()
    {
        // Arrange
        var username = "testuser";
        var password = "testpassword";
        var tokenInfo = new TokenInfo
        {
            Token = "test-token",
            Expiry = DateTime.UtcNow.AddHours(1)
        };

        _mockDbContext.Setup(db => db.Authenticate(username, password))
            .Returns(tokenInfo);

        // Act
        var result = _carrierIntegration.Authenticate(username, password);

        // Assert
        Assert.IsType<Ok<TokenResponse>>(result);
        var okResult = result as Ok<TokenResponse>;
        Assert.Equal("test-token", okResult!.Value!.AccessToken);
        Assert.Equal("Bearer", okResult.Value.TokenType);
    }

    [Fact]
    public void Authenticate_InvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        var username = "testuser";
        var password = "wrongpassword";

        _mockDbContext.Setup(db => db.Authenticate(username, password))
            .Returns(TokenInfo.Invalid);

        // Act
        var result = _carrierIntegration.Authenticate(username, password);

        // Assert
        Assert.IsType<UnauthorizedHttpResult>(result);
    }

    [Fact]
    public void Authenticate_EmptyUsername_ReturnsBadRequest()
    {
        // Arrange
        var username = "";
        var password = "testpassword";

        // Act
        var result = _carrierIntegration.Authenticate(username, password);

        // Assert
        Assert.IsAssignableFrom<IResult>(result);
        Assert.Contains("BadRequest", result.GetType().Name);
    }

    [Fact]
    public void Authenticate_EmptyPassword_ReturnsBadRequest()
    {
        // Arrange
        var username = "testuser";
        var password = "";

        // Act
        var result = _carrierIntegration.Authenticate(username, password);

        // Assert
        Assert.IsAssignableFrom<IResult>(result);
        Assert.Contains("BadRequest", result.GetType().Name);
    }

    [Fact]
    public void Authenticate_NullCredentials_ReturnsBadRequest()
    {
        // Act
        var result1 = _carrierIntegration.Authenticate(null!, "password");
        var result2 = _carrierIntegration.Authenticate("username", null!);

        // Assert
        Assert.Contains("BadRequest", result1.GetType().Name);
        Assert.Contains("BadRequest", result2.GetType().Name);
    }

    #endregion

    #region FindAccount Tests

    [Fact]
    public void FindAccount_ValidToken_ReturnsOkWithAccount()
    {
        // Arrange
        var token = "valid-token";
        var username = "testuser";
        var account = new Account
        {
            Id = Guid.NewGuid().ToString(),
            UserName = username,
            Name = "Test",
            Surname = "User"
        };

        _mockDbContext.Setup(db => db.GetUsernameFromToken(token))
            .Returns(username);
        _mockDbContext.Setup(db => db.FindAccount(username))
            .Returns(account);

        // Act
        var result = _carrierIntegration.FindAccount(token);

        // Assert
        Assert.IsType<Ok<AccountResponse>>(result);
        var okResult = result as Ok<AccountResponse>;
        Assert.Equal(username, okResult!.Value!.Account.UserName);
    }

    [Fact]
    public void FindAccount_InvalidToken_ReturnsUnauthorized()
    {
        // Arrange
        var token = "invalid-token";

        _mockDbContext.Setup(db => db.GetUsernameFromToken(token))
            .Returns((string?)null);

        // Act
        var result = _carrierIntegration.FindAccount(token);

        // Assert
        Assert.IsType<UnauthorizedHttpResult>(result);
    }

    [Fact]
    public void FindAccount_ValidTokenButNoAccount_ReturnsUnauthorized()
    {
        // Arrange
        var token = "valid-token";
        var username = "testuser";

        _mockDbContext.Setup(db => db.GetUsernameFromToken(token))
            .Returns(username);
        _mockDbContext.Setup(db => db.FindAccount(username))
            .Returns((Account?)null);

        // Act
        var result = _carrierIntegration.FindAccount(token);

        // Assert
        Assert.IsType<UnauthorizedHttpResult>(result);
    }

    #endregion

    #region Logout Tests

    [Fact]
    public void Logout_ValidToken_ReturnsOk()
    {
        // Arrange
        var token = "valid-token";
        var username = "testuser";

        _mockDbContext.Setup(db => db.GetUsernameFromToken(token))
            .Returns(username);
        _mockDbContext.Setup(db => db.Logout(username))
            .Returns(true);

        // Act
        var result = _carrierIntegration.Logout(token);

        // Assert
        Assert.IsAssignableFrom<IResult>(result);
        Assert.Contains("Ok", result.GetType().Name);
    }

    [Fact]
    public void Logout_InvalidToken_ReturnsUnauthorized()
    {
        // Arrange
        var token = "invalid-token";

        _mockDbContext.Setup(db => db.GetUsernameFromToken(token))
            .Returns((string?)null);

        // Act
        var result = _carrierIntegration.Logout(token);

        // Assert
        Assert.IsType<UnauthorizedHttpResult>(result);
    }

    [Fact]
    public void Logout_FailedLogout_ReturnsUnauthorized()
    {
        // Arrange
        var token = "valid-token";
        var username = "testuser";

        _mockDbContext.Setup(db => db.GetUsernameFromToken(token))
            .Returns(username);
        _mockDbContext.Setup(db => db.Logout(username))
            .Returns(false);

        // Act
        var result = _carrierIntegration.Logout(token);

        // Assert
        Assert.IsType<UnauthorizedHttpResult>(result);
    }

    #endregion

    #region AddShipment Tests

    [Fact]
    public void AddShipment_ValidTokenAndShipment_ReturnsOkWithShipment()
    {
        // Arrange
        var token = "valid-token";
        var username = "testuser";
        var shipment = new Shipment
        {
            Carrier = "DHL",
            TrackingNumber = "TRACK123",
            Amount = 1.5,
            Zone = "NL"
        };
        var addedShipment = new Shipment
        {
            Id = Guid.NewGuid().ToString(),
            Carrier = "DHL",
            TrackingNumber = "TRACK123",
            Amount = 1.5,
            Zone = "NL"
        };

        _mockDbContext.Setup(db => db.GetUsernameFromToken(token))
            .Returns(username);
        _mockDbContext.Setup(db => db.AddShipment(shipment))
            .Returns(addedShipment);

        // Act
        var result = _carrierIntegration.AddShipment(token, shipment);

        // Assert
        Assert.IsType<Ok<Shipment>>(result);
        var okResult = result as Ok<Shipment>;
        Assert.NotNull(okResult!.Value!.Id);
    }

    [Fact]
    public void AddShipment_InvalidToken_ReturnsUnauthorized()
    {
        // Arrange
        var token = "invalid-token";
        var shipment = new Shipment
        {
            Carrier = "DHL",
            TrackingNumber = "TRACK123",
            Amount = 1.5,
            Zone = "NL"
        };

        _mockDbContext.Setup(db => db.GetUsernameFromToken(token))
            .Returns((string?)null);

        // Act
        var result = _carrierIntegration.AddShipment(token, shipment);

        // Assert
        Assert.IsType<UnauthorizedHttpResult>(result);
    }

    #endregion

    #region GetShipment Tests

    [Fact]
    public void GetShipment_ValidTokenAndShipmentId_ReturnsOkWithShipment()
    {
        // Arrange
        var token = "valid-token";
        var username = "testuser";
        var shipmentId = Guid.NewGuid().ToString();
        var shipment = new Shipment
        {
            Id = shipmentId,
            Carrier = "DHL",
            TrackingNumber = "TRACK123",
            Amount = 1.5,
            Zone = "NL"
        };

        _mockDbContext.Setup(db => db.GetUsernameFromToken(token))
            .Returns(username);
        _mockDbContext.Setup(db => db.GetShipment(shipmentId))
            .Returns(shipment);

        // Act
        var result = _carrierIntegration.GetShipment(token, shipmentId);

        // Assert
        Assert.IsType<Ok<Shipment>>(result);
        var okResult = result as Ok<Shipment>;
        Assert.Equal(shipmentId, okResult!.Value!.Id);
    }

    [Fact]
    public void GetShipment_InvalidToken_ReturnsUnauthorized()
    {
        // Arrange
        var token = "invalid-token";
        var shipmentId = Guid.NewGuid().ToString();

        _mockDbContext.Setup(db => db.GetUsernameFromToken(token))
            .Returns((string?)null);

        // Act
        var result = _carrierIntegration.GetShipment(token, shipmentId);

        // Assert
        Assert.IsType<UnauthorizedHttpResult>(result);
    }

    [Fact]
    public void GetShipment_ShipmentNotFound_ReturnsNotFound()
    {
        // Arrange
        var token = "valid-token";
        var username = "testuser";
        var shipmentId = Guid.NewGuid().ToString();

        _mockDbContext.Setup(db => db.GetUsernameFromToken(token))
            .Returns(username);
        _mockDbContext.Setup(db => db.GetShipment(shipmentId))
            .Returns((Shipment?)null);

        // Act
        var result = _carrierIntegration.GetShipment(token, shipmentId);

        // Assert
        Assert.IsAssignableFrom<IResult>(result);
        Assert.Contains("NotFound", result.GetType().Name);
    }

    #endregion

    #region GetShipments Tests

    [Fact]
    public void GetShipments_ValidToken_ReturnsOkWithShipments()
    {
        // Arrange
        var token = "valid-token";
        var username = "testuser";
        var shipments = new[]
        {
            new Shipment { Id = Guid.NewGuid().ToString(), Carrier = "DHL", TrackingNumber = "T1", Amount = 1.0, Zone = "NL" },
            new Shipment { Id = Guid.NewGuid().ToString(), Carrier = "FedEx", TrackingNumber = "T2", Amount = 2.0, Zone = "EU" }
        };

        _mockDbContext.Setup(db => db.GetUsernameFromToken(token))
            .Returns(username);
        _mockDbContext.Setup(db => db.GetShipments())
            .Returns(shipments);

        // Act
        var result = _carrierIntegration.GetShipments(token);

        // Assert
        Assert.IsType<Ok<Shipment[]>>(result);
        var okResult = result as Ok<Shipment[]>;
        Assert.Equal(2, okResult!.Value!.Length);
    }

    [Fact]
    public void GetShipments_InvalidToken_ReturnsUnauthorized()
    {
        // Arrange
        var token = "invalid-token";

        _mockDbContext.Setup(db => db.GetUsernameFromToken(token))
            .Returns((string?)null);

        // Act
        var result = _carrierIntegration.GetShipments(token);

        // Assert
        Assert.IsType<UnauthorizedHttpResult>(result);
    }

    #endregion

    #region AddShipmentLabel Tests

    [Fact]
    public void AddShipmentLabel_ValidTokenAndShipmentId_ReturnsOkWithLabel()
    {
        // Arrange
        var token = "valid-token";
        var username = "testuser";
        var shipmentId = Guid.NewGuid().ToString();
        var shipment = new Shipment
        {
            Id = shipmentId,
            Carrier = "DHL",
            TrackingNumber = "TRACK123",
            Amount = 1.5,
            Zone = "NL"
        };
        var label = new ShipmentLabel
        {
            Id = Guid.NewGuid().ToString(),
            ShipmentId = shipmentId,
            LabelData = new byte[] { 1, 2, 3 },
            Format = "PDF"
        };

        _mockDbContext.Setup(db => db.GetUsernameFromToken(token))
            .Returns(username);
        _mockDbContext.Setup(db => db.GetShipment(shipmentId))
            .Returns(shipment);
        _mockDbContext.Setup(db => db.AddShipmentLabel(It.IsAny<ShipmentLabel>()))
            .Returns(label);

        // Act
        var result = _carrierIntegration.AddShipmentLabel(token, shipmentId, null);

        // Assert
        Assert.IsType<Ok<ShipmentLabel>>(result);
        var okResult = result as Ok<ShipmentLabel>;
        Assert.NotNull(okResult!.Value!.Id);
    }

    [Fact]
    public void AddShipmentLabel_InvalidToken_ReturnsUnauthorized()
    {
        // Arrange
        var token = "invalid-token";
        var shipmentId = Guid.NewGuid().ToString();

        _mockDbContext.Setup(db => db.GetUsernameFromToken(token))
            .Returns((string?)null);

        // Act
        var result = _carrierIntegration.AddShipmentLabel(token, shipmentId, null);

        // Assert
        Assert.IsType<UnauthorizedHttpResult>(result);
    }

    [Fact]
    public void AddShipmentLabel_ShipmentNotFound_ReturnsNotFound()
    {
        // Arrange
        var token = "valid-token";
        var username = "testuser";
        var shipmentId = Guid.NewGuid().ToString();

        _mockDbContext.Setup(db => db.GetUsernameFromToken(token))
            .Returns(username);
        _mockDbContext.Setup(db => db.GetShipment(shipmentId))
            .Returns((Shipment?)null);

        // Act
        var result = _carrierIntegration.AddShipmentLabel(token, shipmentId, null);

        // Assert
        Assert.IsAssignableFrom<IResult>(result);
        Assert.Contains("NotFound", result.GetType().Name);
    }

    #endregion

    #region GetShipmentLabels Tests

    [Fact]
    public void GetShipmentLabels_ValidTokenAndTrackingNumber_ReturnsOkWithLabels()
    {
        // Arrange
        var token = "valid-token";
        var username = "testuser";
        var trackingNumber = "TRACK123";
        var shipmentId = Guid.NewGuid().ToString();
        var shipment = new Shipment
        {
            Id = shipmentId,
            Carrier = "DHL",
            TrackingNumber = trackingNumber,
            Amount = 1.5,
            Zone = "NL"
        };
        var labels = new[]
        {
            new ShipmentLabel { Id = Guid.NewGuid().ToString(), ShipmentId = shipmentId, LabelData = new byte[] { 1 }, Format = "PDF" },
            new ShipmentLabel { Id = Guid.NewGuid().ToString(), ShipmentId = shipmentId, LabelData = new byte[] { 2 }, Format = "PNG" }
        };

        _mockDbContext.Setup(db => db.GetUsernameFromToken(token))
            .Returns(username);
        _mockDbContext.Setup(db => db.GetShipmentByTrackingNumber(trackingNumber))
            .Returns(shipment);
        _mockDbContext.Setup(db => db.GetShipmentLabels(shipment))
            .Returns(labels);

        // Act
        var result = _carrierIntegration.GetShipmentLabels(token, trackingNumber);

        // Assert
        Assert.IsType<Ok<ShipmentLabel[]>>(result);
        var okResult = result as Ok<ShipmentLabel[]>;
        Assert.Equal(2, okResult!.Value!.Length);
    }

    [Fact]
    public void GetShipmentLabels_InvalidToken_ReturnsUnauthorized()
    {
        // Arrange
        var token = "invalid-token";
        var trackingNumber = "TRACK123";

        _mockDbContext.Setup(db => db.GetUsernameFromToken(token))
            .Returns((string?)null);

        // Act
        var result = _carrierIntegration.GetShipmentLabels(token, trackingNumber);

        // Assert
        Assert.IsType<UnauthorizedHttpResult>(result);
    }

    [Fact]
    public void GetShipmentLabels_TrackingNumberNotFound_ReturnsNotFound()
    {
        // Arrange
        var token = "valid-token";
        var username = "testuser";
        var trackingNumber = "TRACK123";

        _mockDbContext.Setup(db => db.GetUsernameFromToken(token))
            .Returns(username);
        _mockDbContext.Setup(db => db.GetShipmentByTrackingNumber(trackingNumber))
            .Returns((Shipment?)null);

        // Act
        var result = _carrierIntegration.GetShipmentLabels(token, trackingNumber);

        // Assert
        Assert.IsAssignableFrom<IResult>(result);
        Assert.Contains("NotFound", result.GetType().Name);
    }

    #endregion

    #region GetShipmentLabelsByShipmentId Tests

    [Fact]
    public void GetShipmentLabelsByShipmentId_ValidTokenAndShipmentId_ReturnsOkWithLabels()
    {
        // Arrange
        var token = "valid-token";
        var username = "testuser";
        var shipmentId = Guid.NewGuid().ToString();
        var shipment = new Shipment
        {
            Id = shipmentId,
            Carrier = "DHL",
            TrackingNumber = "TRACK123",
            Amount = 1.5,
            Zone = "NL"
        };
        var labels = new[]
        {
            new ShipmentLabel { Id = Guid.NewGuid().ToString(), ShipmentId = shipmentId, LabelData = new byte[] { 1 }, Format = "PDF" }
        };

        _mockDbContext.Setup(db => db.GetUsernameFromToken(token))
            .Returns(username);
        _mockDbContext.Setup(db => db.GetShipment(shipmentId))
            .Returns(shipment);
        _mockDbContext.Setup(db => db.GetShipmentLabels(shipment))
            .Returns(labels);

        // Act
        var result = _carrierIntegration.GetShipmentLabelsByShipmentId(token, shipmentId);

        // Assert
        Assert.IsType<Ok<ShipmentLabel[]>>(result);
        var okResult = result as Ok<ShipmentLabel[]>;
        Assert.Single(okResult!.Value!);
    }

    [Fact]
    public void GetShipmentLabelsByShipmentId_InvalidToken_ReturnsUnauthorized()
    {
        // Arrange
        var token = "invalid-token";
        var shipmentId = Guid.NewGuid().ToString();

        _mockDbContext.Setup(db => db.GetUsernameFromToken(token))
            .Returns((string?)null);

        // Act
        var result = _carrierIntegration.GetShipmentLabelsByShipmentId(token, shipmentId);

        // Assert
        Assert.IsType<UnauthorizedHttpResult>(result);
    }

    [Fact]
    public void GetShipmentLabelsByShipmentId_ShipmentIdNotFound_ReturnsNotFound()
    {
        // Arrange
        var token = "valid-token";
        var username = "testuser";
        var shipmentId = Guid.NewGuid().ToString();

        _mockDbContext.Setup(db => db.GetUsernameFromToken(token))
            .Returns(username);
        _mockDbContext.Setup(db => db.GetShipment(shipmentId))
            .Returns((Shipment?)null);

        // Act
        var result = _carrierIntegration.GetShipmentLabelsByShipmentId(token, shipmentId);

        // Assert
        Assert.IsAssignableFrom<IResult>(result);
        Assert.Contains("NotFound", result.GetType().Name);
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_SetsDbContextProperty()
    {
        // Arrange
        var mockContext = new Mock<IShippingDbContext>();

        // Act
        var integration = new CarrierIntegration(mockContext.Object);

        // Assert
        Assert.Same(mockContext.Object, integration.DbContext);
    }

    #endregion
}

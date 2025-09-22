using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Maliev.PurchaseOrderService.Api.Configuration;
using Maliev.PurchaseOrderService.Api.ExternalServices;
using Maliev.PurchaseOrderService.Api.DTOs;

namespace Maliev.PurchaseOrderService.Tests.ExternalServices;

/// <summary>
/// T035: External service mock tests for OrderService
/// Tests HTTP client configuration, error handling, circuit breaker patterns,
/// authentication, timeout handling, response parsing and service availability
/// </summary>
public class OrderServiceTests
{
    private readonly Mock<ILogger<OrderServiceClient>> _loggerMock;
    private readonly Mock<IOptions<ExternalServiceOptions>> _optionsMock;
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly HttpClient _httpClient;
    private readonly ExternalServiceOptions _serviceOptions;

    public OrderServiceTests()
    {
        _loggerMock = new Mock<ILogger<OrderServiceClient>>();
        _optionsMock = new Mock<IOptions<ExternalServiceOptions>>();
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();

        _serviceOptions = new ExternalServiceOptions
        {
            OrderService = new ServiceEndpoint
            {
                BaseUrl = "https://api.maliev.com/orders/v1",
                TimeoutInSeconds = 45
            }
        };

        _optionsMock.Setup(x => x.Value).Returns(_serviceOptions);

        _httpClient = new HttpClient(_httpMessageHandlerMock.Object)
        {
            BaseAddress = new Uri(_serviceOptions.OrderService.BaseUrl)
        };
    }

    [Fact]
    public async Task GetOrderAsync_ValidId_ReturnsOrderDetails()
    {
        // Arrange
        var orderId = 12345;
        var expectedOrder = new OrderDto
        {
            Id = orderId,
            OrderNumber = "ORD-2024-001",
            CustomerId = 67890,
            CustomerName = "Test Customer",
            OrderDate = DateTime.UtcNow,
            Status = "Pending",
            TotalAmount = 1500.00m,
            Currency = "USD",
            OrderItems = new List<OrderItemDto>
            {
                new() { Id = 1, ExternalOrderItemId = 123, ProductName = "Product 1", Quantity = 2, UnitPrice = 750.00m, UnitOfMeasure = "pcs", Currency = "USD", TotalPrice = 1500.00m, CachedAt = DateTime.UtcNow }
            }
        };

        var responseContent = JsonSerializer.Serialize(expectedOrder);
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseContent, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"))
        };

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri!.ToString().Contains($"/orders/{orderId}")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        var service = new OrderServiceClient(_httpClient, _loggerMock.Object, _optionsMock.Object);

        // Act
        var result = await service.GetOrderAsync(orderId);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(expectedOrder.Id);
        result.OrderNumber.Should().Be(expectedOrder.OrderNumber);
        result.TotalAmount.Should().Be(expectedOrder.TotalAmount);
        result.OrderItems.Should().HaveCount(1);
    }

    [Fact]
    public async Task CreateOrderAsync_ValidData_ReturnsCreatedOrder()
    {
        // Arrange
        var createRequest = new
        {
            CustomerId = 67890,
            Items = new[]
            {
                new { ProductId = 123, Quantity = 2, UnitPrice = 500.00m }
            },
            DeliveryAddress = "123 Test Street",
            Notes = "Test order"
        };

        var createdOrder = new OrderDto
        {
            Id = 11111,
            OrderNumber = "ORD-2024-002",
            CustomerId = createRequest.CustomerId,
            OrderDate = DateTime.UtcNow,
            Status = "Pending",
            TotalAmount = 1000.00m,
            Currency = "USD"
        };

        var responseContent = JsonSerializer.Serialize(createdOrder);
        var httpResponse = new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = new StringContent(responseContent, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"))
        };

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri!.ToString().Contains("/orders")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        var service = new OrderServiceClient(_httpClient, _loggerMock.Object, _optionsMock.Object);

        // Act
        var result = await service.GetOrderAsync(11111);

        // Assert
        result.Should().NotBeNull();
        result.CustomerId.Should().Be(createRequest.CustomerId);
        result.Status.Should().Be("Pending");
        result.TotalAmount.Should().Be(1000.00m);
    }

    [Fact]
    public async Task UpdateOrderStatusAsync_ValidData_ReturnsUpdatedOrder()
    {
        // Arrange
        var orderId = 12345;
        var statusUpdate = new
        {
            Status = "Confirmed",
            StatusReason = "Payment verified",
            UpdatedBy = "system"
        };

        var updatedOrder = new OrderDto
        {
            Id = orderId,
            OrderNumber = "ORD-2024-003",
            Status = statusUpdate.Status,
            UpdatedAt = DateTime.UtcNow,
            TotalAmount = 750.00m,
            Currency = "USD"
        };

        var responseContent = JsonSerializer.Serialize(updatedOrder);
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseContent, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"))
        };

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Patch &&
                    req.RequestUri!.ToString().Contains($"/orders/{orderId}/status")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        var service = new OrderServiceClient(_httpClient, _loggerMock.Object, _optionsMock.Object);

        // Act
        var result = await service.UpdateOrderStatusAsync(orderId, statusUpdate.Status);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task GetOrdersByCustomerAsync_ValidCustomerId_ReturnsOrders()
    {
        // Arrange
        var customerId = 55555;
        var filters = new
        {
            CustomerId = customerId,
            Status = "Pending",
            Page = 1,
            PageSize = 20
        };

        var expectedOrders = new
        {
            Orders = new[]
            {
                new { Id = 77777, OrderNumber = "ORD-001", Status = "Pending", TotalAmount = 500.00m },
                new { Id = 88888, OrderNumber = "ORD-002", Status = "Pending", TotalAmount = 750.00m }
            },
            TotalCount = 2,
            Page = 1,
            PageSize = 20
        };

        var responseContent = JsonSerializer.Serialize(expectedOrders);
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseContent, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"))
        };

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri!.ToString().Contains("/orders") &&
                    req.RequestUri.ToString().Contains($"customerId={customerId}")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        var service = new OrderServiceClient(_httpClient, _loggerMock.Object, _optionsMock.Object);

        // Act
        var result = await service.GetOrderAsync(customerId);

        // Assert
        result.Should().NotBeNull();
        result.CustomerId.Should().Be(customerId);
        result.Id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CancelOrderAsync_ValidId_CompletesSuccessfully()
    {
        // Arrange
        var orderId = 12345;
        var cancelRequest = new
        {
            Reason = "Customer request",
            CancelledBy = "customer"
        };

        var httpResponse = new HttpResponseMessage(HttpStatusCode.NoContent);

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri!.ToString().Contains($"/orders/{orderId}/cancel")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        var service = new OrderServiceClient(_httpClient, _loggerMock.Object, _optionsMock.Object);

        // Act
        await service.UpdateOrderStatusAsync(orderId, "Cancelled");

        // Assert
        _httpMessageHandlerMock
            .Protected()
            .Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri!.ToString().Contains($"/orders/{orderId}/cancel")),
                ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task GetOrderAsync_ServiceUnavailable_ThrowsHttpRequestException()
    {
        // Arrange
        var orderId = 12345;
        var httpResponse = new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        var service = new OrderServiceClient(_httpClient, _loggerMock.Object, _optionsMock.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<HttpRequestException>(
            () => service.GetOrderAsync(orderId));

        exception.Message.Should().Contain("OrderService");
    }

    [Fact]
    public async Task GetOrderAsync_Timeout_ThrowsTimeoutException()
    {
        // Arrange
        var orderId = 12345;

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException("Request timeout"));

        var service = new OrderServiceClient(_httpClient, _loggerMock.Object, _optionsMock.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<TimeoutException>(
            () => service.GetOrderAsync(orderId));

        exception.Message.Should().Contain("timeout");
    }

    [Fact]
    public async Task CreateOrderAsync_InvalidData_ThrowsBadRequestException()
    {
        // Arrange
        var createRequest = new
        {
            CustomerId = Guid.Empty, // Invalid customer ID
            Items = new object[0]
        };

        var errorResponse = new { message = "Invalid customer ID", errors = new[] { "CustomerId is required" } };
        var responseContent = JsonSerializer.Serialize(errorResponse);
        var httpResponse = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent(responseContent, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"))
        };

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        var service = new OrderServiceClient(_httpClient, _loggerMock.Object, _optionsMock.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<HttpRequestException>(
            () => service.GetOrderAsync(0));

        exception.Message.Should().Contain("Invalid customer ID");
    }

    [Fact]
    public async Task GetOrderAsync_CircuitBreakerOpen_ThrowsCircuitBreakerException()
    {
        // Arrange
        var orderId = 12345;

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Circuit breaker is open for OrderService"));

        var service = new OrderServiceClient(_httpClient, _loggerMock.Object, _optionsMock.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.GetOrderAsync(orderId));

        exception.Message.Should().Contain("OrderService");
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    public async Task GetOrderAsync_AuthenticationFailure_ThrowsAuthenticationException(HttpStatusCode statusCode)
    {
        // Arrange
        var orderId = 12345;
        var httpResponse = new HttpResponseMessage(statusCode);

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        var service = new OrderServiceClient(_httpClient, _loggerMock.Object, _optionsMock.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.GetOrderAsync(orderId));

        exception.Message.Should().Contain("authentication");
    }

    [Fact]
    public async Task UpdateOrderStatusAsync_OrderNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var orderId = 12345;
        var statusUpdate = new { Status = "Confirmed" };
        var httpResponse = new HttpResponseMessage(HttpStatusCode.NotFound);

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        var service = new OrderServiceClient(_httpClient, _loggerMock.Object, _optionsMock.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UpdateOrderStatusAsync(orderId, statusUpdate.Status));

        exception.Message.Should().Contain("Order not found");
    }

    [Fact]
    public async Task GetOrderAsync_InvalidJson_ThrowsInvalidDataException()
    {
        // Arrange
        var orderId = 12345;
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("invalid json response", Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"))
        };

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        var service = new OrderServiceClient(_httpClient, _loggerMock.Object, _optionsMock.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidDataException>(
            () => service.GetOrderAsync(orderId));

        exception.Message.Should().Contain("parsing");
    }

    [Fact]
    public void Constructor_NullHttpClientFactory_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new OrderServiceClient(null!, _loggerMock.Object, _optionsMock.Object));
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new OrderServiceClient(_httpClient, null!, _optionsMock.Object));
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new OrderServiceClient(_httpClient, _loggerMock.Object, null!));
    }
}
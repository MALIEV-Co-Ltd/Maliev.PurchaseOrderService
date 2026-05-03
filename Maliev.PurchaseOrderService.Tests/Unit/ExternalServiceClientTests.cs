using System.Net;
using System.Net.Http.Json;
using Maliev.PurchaseOrderService.Api.ExternalServices;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;

namespace Maliev.PurchaseOrderService.Tests.Unit;

public class SupplierServiceClientTests
{
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<ILogger<SupplierServiceClient>> _loggerMock;
    private readonly HttpClient _httpClient;
    private readonly SupplierServiceClient _client;

    public SupplierServiceClientTests()
    {
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _loggerMock = new Mock<ILogger<SupplierServiceClient>>();

        var handlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("http://test/")
        };

        _httpClientFactoryMock
            .Setup(x => x.CreateClient("SupplierService"))
            .Returns(_httpClient);

        _client = new SupplierServiceClient(_httpClientFactoryMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetSupplierAsync_WithValidId_ReturnsSupplier()
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("http://test/")
        };

        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = JsonContent.Create(new Application.Interfaces.SupplierDto
                {
                    Id = 1,
                    Name = "Test Supplier",
                    ContactInfo = "Contact",
                    Email = "email@test.com",
                    Phone = "123456"
                })
            });

        _httpClientFactoryMock
            .Setup(x => x.CreateClient("SupplierService"))
            .Returns(httpClient);

        var client = new SupplierServiceClient(_httpClientFactoryMock.Object, _loggerMock.Object);

        var result = await client.GetSupplierAsync(1);

        Assert.NotNull(result);
        Assert.Equal("Test Supplier", result.Name);
        Assert.Equal("email@test.com", result.Email);
    }

    [Fact]
    public async Task GetSupplierAsync_WithServiceGuid_RequestsSupplierServiceRouteAndMapsSupplier()
    {
        var supplierId = Guid.Parse("8f851a28-5947-4cd4-9ef9-0d2698fd36f8");
        Uri? requestedUri = null;
        var handlerMock = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("http://test/supplier/v1/suppliers/")
        };

        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((request, _) => requestedUri = request.RequestUri)
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = JsonContent.Create(new
                {
                    id = supplierId,
                    companyName = "Acme Materials",
                    address = "1 Industrial Road",
                    city = "Bangkok",
                    country = "TH"
                })
            });

        _httpClientFactoryMock
            .Setup(x => x.CreateClient("SupplierService"))
            .Returns(httpClient);

        var client = new SupplierServiceClient(_httpClientFactoryMock.Object, _loggerMock.Object);

        var result = await client.GetSupplierAsync(supplierId);

        Assert.NotNull(result);
        Assert.Equal(supplierId, result.ExternalId);
        Assert.Equal("Acme Materials", result.Name);
        Assert.Equal(new Uri($"http://test/supplier/v1/suppliers/{supplierId:D}"), requestedUri);
    }

    [Fact]
    public async Task GetSupplierAsync_WithNotFound_ReturnsNull()
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("http://test/")
        };

        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NotFound
            });

        _httpClientFactoryMock
            .Setup(x => x.CreateClient("SupplierService"))
            .Returns(httpClient);

        var client = new SupplierServiceClient(_httpClientFactoryMock.Object, _loggerMock.Object);

        var result = await client.GetSupplierAsync(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task ValidateSupplierExistsAsync_WithExistingId_ReturnsTrue()
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("http://test/")
        };

        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK
            });

        _httpClientFactoryMock
            .Setup(x => x.CreateClient("SupplierService"))
            .Returns(httpClient);

        var client = new SupplierServiceClient(_httpClientFactoryMock.Object, _loggerMock.Object);

        var result = await client.ValidateSupplierExistsAsync(1);

        Assert.True(result);
    }

    [Fact]
    public async Task ValidateSupplierExistsAsync_WithNonExistingId_ReturnsFalse()
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("http://test/")
        };

        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NotFound
            });

        _httpClientFactoryMock
            .Setup(x => x.CreateClient("SupplierService"))
            .Returns(httpClient);

        var client = new SupplierServiceClient(_httpClientFactoryMock.Object, _loggerMock.Object);

        var result = await client.ValidateSupplierExistsAsync(999);

        Assert.False(result);
    }

    [Fact]
    public async Task GetSupplierAsync_OnException_ThrowsExternalServiceException()
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("http://test/")
        };

        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection failed"));

        _httpClientFactoryMock
            .Setup(x => x.CreateClient("SupplierService"))
            .Returns(httpClient);

        var client = new SupplierServiceClient(_httpClientFactoryMock.Object, _loggerMock.Object);

        await Assert.ThrowsAsync<ExternalServiceException>(() => client.GetSupplierAsync(1));
    }
}

public class OrderServiceClientTests
{
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<ILogger<OrderServiceClient>> _loggerMock;

    public OrderServiceClientTests()
    {
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _loggerMock = new Mock<ILogger<OrderServiceClient>>();
    }

    [Fact]
    public async Task GetOrderAsync_WithValidId_ReturnsOrder()
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("http://test/orders/v1/")
        };

        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = JsonContent.Create(new Application.Interfaces.OrderDto
                {
                    Id = 1,
                    OrderNumber = "ORD-001",
                    OrderDate = DateTime.UtcNow,
                    Status = "Active",
                    Items = new List<Application.Interfaces.OrderItemDto>()
                })
            });

        _httpClientFactoryMock
            .Setup(x => x.CreateClient("OrderService"))
            .Returns(httpClient);

        var client = new OrderServiceClient(_httpClientFactoryMock.Object, _loggerMock.Object);

        var result = await client.GetOrderAsync(1);

        Assert.NotNull(result);
        Assert.Equal("ORD-001", result.OrderNumber);
    }

    [Fact]
    public async Task GetOrderAsync_WithSourceOrderId_RequestsOrderServiceRouteAndDerivesLineItem()
    {
        const string orderId = "ORD-2026-0001";
        Uri? requestedUri = null;
        var handlerMock = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("http://test/order/v1/orders/")
        };

        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((request, _) => requestedUri = request.RequestUri)
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = JsonContent.Create(new
                {
                    orderId,
                    serviceCategoryName = "CNC machining",
                    processTypeName = "Milling",
                    currentStatus = "Approved",
                    orderedQuantity = 4,
                    quotedAmount = 1200m,
                    quoteCurrency = "THB",
                    createdAt = DateTime.UtcNow
                })
            });

        _httpClientFactoryMock
            .Setup(x => x.CreateClient("OrderService"))
            .Returns(httpClient);

        var client = new OrderServiceClient(_httpClientFactoryMock.Object, _loggerMock.Object);

        var result = await client.GetOrderAsync(orderId);

        Assert.NotNull(result);
        Assert.Equal(orderId, result.SourceOrderId);
        Assert.Equal("ORD-2026-0001", result.OrderNumber);
        Assert.Single(result.Items);
        Assert.Equal("primary", result.Items[0].SourceItemId);
        Assert.Equal(300m, result.Items[0].UnitPrice);
        Assert.Equal(new Uri($"http://test/order/v1/orders/{orderId}"), requestedUri);
    }

    [Fact]
    public async Task GetOrderAsync_WithNotFound_ReturnsNull()
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("http://test/orders/v1/")
        };

        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NotFound
            });

        _httpClientFactoryMock
            .Setup(x => x.CreateClient("OrderService"))
            .Returns(httpClient);

        var client = new OrderServiceClient(_httpClientFactoryMock.Object, _loggerMock.Object);

        var result = await client.GetOrderAsync(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetOrderItemsAsync_WithValidOrderId_ReturnsItems()
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("http://test/orders/v1/")
        };

        var items = new List<Application.Interfaces.OrderItemDto>
        {
            new() { Id = 1, ProductCode = "P001", ProductName = "Product 1", Quantity = 10, UnitOfMeasure = "EA", UnitPrice = 100, TotalPrice = 1000, Currency = "THB" },
            new() { Id = 2, ProductCode = "P002", ProductName = "Product 2", Quantity = 5, UnitOfMeasure = "EA", UnitPrice = 50, TotalPrice = 250, Currency = "THB" }
        };

        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = JsonContent.Create(items)
            });

        _httpClientFactoryMock
            .Setup(x => x.CreateClient("OrderService"))
            .Returns(httpClient);

        var client = new OrderServiceClient(_httpClientFactoryMock.Object, _loggerMock.Object);

        var result = await client.GetOrderItemsAsync(1);

        Assert.Equal(2, result.Count);
        Assert.Equal("P001", result[0].ProductCode);
    }

    [Fact]
    public async Task GetOrderItemsAsync_WithFailedResponse_ReturnsEmptyList()
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("http://test/orders/v1/")
        };

        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError
            });

        _httpClientFactoryMock
            .Setup(x => x.CreateClient("OrderService"))
            .Returns(httpClient);

        var client = new OrderServiceClient(_httpClientFactoryMock.Object, _loggerMock.Object);

        var result = await client.GetOrderItemsAsync(1);

        Assert.Empty(result);
    }

    [Fact]
    public async Task ValidateOrderExistsAsync_WithExistingId_ReturnsTrue()
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("http://test/orders/v1/")
        };

        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK
            });

        _httpClientFactoryMock
            .Setup(x => x.CreateClient("OrderService"))
            .Returns(httpClient);

        var client = new OrderServiceClient(_httpClientFactoryMock.Object, _loggerMock.Object);

        var result = await client.ValidateOrderExistsAsync(1);

        Assert.True(result);
    }

    [Fact]
    public async Task GetOrderAsync_OnException_ThrowsExternalServiceException()
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("http://test/orders/v1/")
        };

        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection failed"));

        _httpClientFactoryMock
            .Setup(x => x.CreateClient("OrderService"))
            .Returns(httpClient);

        var client = new OrderServiceClient(_httpClientFactoryMock.Object, _loggerMock.Object);

        await Assert.ThrowsAsync<ExternalServiceException>(() => client.GetOrderAsync(1));
    }
}

public class CurrencyServiceClientTests
{
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<ILogger<CurrencyServiceClient>> _loggerMock;
    private readonly Microsoft.Extensions.Caching.Memory.IMemoryCache _cache;

    public CurrencyServiceClientTests()
    {
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _loggerMock = new Mock<ILogger<CurrencyServiceClient>>();
        _cache = new Microsoft.Extensions.Caching.Memory.MemoryCache(new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions());
    }

    [Fact]
    public async Task GetCurrencyAsync_WithValidId_ReturnsCurrency()
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("http://test/")
        };

        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = JsonContent.Create(new Application.Interfaces.CurrencyDto
                {
                    Id = 1,
                    Code = "THB",
                    Symbol = "฿",
                    Name = "Thai Baht",
                    ExchangeRate = 1.0m
                })
            });

        _httpClientFactoryMock
            .Setup(x => x.CreateClient("CurrencyService"))
            .Returns(httpClient);

        var client = new CurrencyServiceClient(_httpClientFactoryMock.Object, _cache, _loggerMock.Object);

        var result = await client.GetCurrencyAsync(1);

        Assert.NotNull(result);
        Assert.Equal("THB", result.Code);
        Assert.Equal("฿", result.Symbol);
    }

    [Fact]
    public async Task GetCurrencyByCodeAsync_RequestsCurrencyCodeRouteAndMapsCurrency()
    {
        var currencyId = Guid.Parse("447b1f1f-7287-44b7-a746-2a0d69a5d706");
        Uri? requestedUri = null;
        var handlerMock = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("http://test/currency/v1/currencies/")
        };

        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((request, _) => requestedUri = request.RequestUri)
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = JsonContent.Create(new
                {
                    id = currencyId,
                    code = "THB",
                    symbol = "฿",
                    name = "Thai Baht",
                    exchangeRate = 1m
                })
            });

        _httpClientFactoryMock
            .Setup(x => x.CreateClient("CurrencyService"))
            .Returns(httpClient);

        var client = new CurrencyServiceClient(_httpClientFactoryMock.Object, _cache, _loggerMock.Object);

        var result = await client.GetCurrencyByCodeAsync("thb");

        Assert.NotNull(result);
        Assert.Equal(currencyId, result.ExternalId);
        Assert.Equal("THB", result.Code);
        Assert.Equal(new Uri("http://test/currency/v1/currencies/THB"), requestedUri);
    }

    [Fact]
    public async Task GetCurrencyAsync_WithNotFound_ReturnsNull()
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("http://test/")
        };

        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NotFound
            });

        _httpClientFactoryMock
            .Setup(x => x.CreateClient("CurrencyService"))
            .Returns(httpClient);

        var client = new CurrencyServiceClient(_httpClientFactoryMock.Object, _cache, _loggerMock.Object);

        var result = await client.GetCurrencyAsync(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task ValidateCurrencyExistsAsync_WithExistingId_ReturnsTrue()
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("http://test/")
        };

        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = JsonContent.Create(new Application.Interfaces.CurrencyDto
                {
                    Id = 1,
                    Code = "THB",
                    Symbol = "฿",
                    Name = "Thai Baht",
                    ExchangeRate = 1.0m
                })
            });

        _httpClientFactoryMock
            .Setup(x => x.CreateClient("CurrencyService"))
            .Returns(httpClient);

        var client = new CurrencyServiceClient(_httpClientFactoryMock.Object, _cache, _loggerMock.Object);

        var result = await client.ValidateCurrencyExistsAsync(1);

        Assert.True(result);
    }

    [Fact]
    public async Task ValidateCurrencyExistsAsync_WithNonExistingId_ReturnsFalse()
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("http://test/")
        };

        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NotFound
            });

        _httpClientFactoryMock
            .Setup(x => x.CreateClient("CurrencyService"))
            .Returns(httpClient);

        var client = new CurrencyServiceClient(_httpClientFactoryMock.Object, _cache, _loggerMock.Object);

        var result = await client.ValidateCurrencyExistsAsync(999);

        Assert.False(result);
    }

    [Fact]
    public async Task GetCurrencyAsync_CachesResult_OnSecondCall()
    {
        var callCount = 0;

        var handlerMock = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("http://test/")
        };

        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                callCount++;
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = JsonContent.Create(new Application.Interfaces.CurrencyDto
                    {
                        Id = 1,
                        Code = "THB",
                        Symbol = "฿",
                        Name = "Thai Baht",
                        ExchangeRate = 1.0m
                    })
                };
            });

        _httpClientFactoryMock
            .Setup(x => x.CreateClient("CurrencyService"))
            .Returns(httpClient);

        var client = new CurrencyServiceClient(_httpClientFactoryMock.Object, _cache, _loggerMock.Object);

        var result1 = await client.GetCurrencyAsync(1);
        var result2 = await client.GetCurrencyAsync(1);

        Assert.Equal(1, callCount);
        Assert.NotNull(result1);
        Assert.NotNull(result2);
    }

    [Fact]
    public async Task GetCurrencyAsync_OnException_ThrowsExternalServiceException()
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("http://test/")
        };

        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection failed"));

        _httpClientFactoryMock
            .Setup(x => x.CreateClient("CurrencyService"))
            .Returns(httpClient);

        var client = new CurrencyServiceClient(_httpClientFactoryMock.Object, _cache, _loggerMock.Object);

        await Assert.ThrowsAsync<ExternalServiceException>(() => client.GetCurrencyAsync(1));
    }
}

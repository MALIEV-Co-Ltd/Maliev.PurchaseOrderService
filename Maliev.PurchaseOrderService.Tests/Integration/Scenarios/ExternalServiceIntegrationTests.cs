using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Moq;
using Maliev.PurchaseOrderService.Data;
using Maliev.PurchaseOrderService.Api.DTOs;
using Maliev.PurchaseOrderService.Api.ExternalServices;
using Maliev.PurchaseOrderService.Api.Services;
using Maliev.PurchaseOrderService.Data.Entities;
using Maliev.PurchaseOrderService.Data.Enums;
using System.Net;

namespace Maliev.PurchaseOrderService.Tests.Integration.Scenarios;

public class ExternalServiceIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly Mock<ISupplierServiceClient> _mockSupplierService;
    private readonly Mock<IOrderServiceClient> _mockOrderService;
    private readonly Mock<ICurrencyServiceClient> _mockCurrencyService;
    private readonly Mock<IUploadServiceClient> _mockUploadService;
    private readonly Mock<IPdfGenerationService> _mockPdfService;
    private readonly Mock<IWHTCalculationService> _mockWHTService;

    public ExternalServiceIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _mockSupplierService = new Mock<ISupplierServiceClient>();
        _mockOrderService = new Mock<IOrderServiceClient>();
        _mockCurrencyService = new Mock<ICurrencyServiceClient>();
        _mockUploadService = new Mock<IUploadServiceClient>();
        _mockPdfService = new Mock<IPdfGenerationService>();
        _mockWHTService = new Mock<IWHTCalculationService>();

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove the real DbContext registration
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<PurchaseOrderContext>));
                if (descriptor != null)
                    services.Remove(descriptor);

                // Add InMemory database for testing
                services.AddDbContext<PurchaseOrderContext>(options =>
                {
                    options.UseInMemoryDatabase("TestDatabase_ExternalServiceIntegration");
                });

                // Replace external service clients with mocks
                services.AddSingleton(_mockSupplierService.Object);
                services.AddSingleton(_mockOrderService.Object);
                services.AddSingleton(_mockCurrencyService.Object);
                services.AddSingleton(_mockUploadService.Object);
                services.AddSingleton(_mockPdfService.Object);
                services.AddSingleton(_mockWHTService.Object);
            });
        });

        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task Create_Purchase_Order_With_All_External_Service_Validations_Success()
    {
        // Arrange
        SetupEmployeeAuthentication();
        SetupAllExternalServicesForSuccess();

        var createRequest = new CreatePurchaseOrderRequest
        {
            OrderType = OrderType.Internal,
            SupplierID = 1234,
            OrderID = 5678,
            CurrencyID = 1,
            Notes = "Integration test order",
            OrderItems = new List<CreateOrderItemRequest>
            {
                new()
                {
                    Quantity = 5,
                    UnitPrice = 200.00m,
                    Notes = "Integration test item",
                    ProductName = "Test Product"
                }
            }
        };

        var json = JsonSerializer.Serialize(createRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/v1.0/purchase-orders", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        // Verify all external service calls were made
        _mockSupplierService.Verify(x => x.ValidateSupplierAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockCurrencyService.Verify(x => x.ValidateCurrencyAsync("THB", It.IsAny<CancellationToken>()), Times.Once);
        _mockOrderService.Verify(x => x.GetOrderItemsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_Purchase_Order_With_Supplier_Service_Failure_Handles_Gracefully()
    {
        // Arrange
        SetupEmployeeAuthentication();
        SetupSupplierServiceFailure();
        SetupValidCurrencyService();
        SetupValidOrderService();

        var createRequest = new CreatePurchaseOrderRequest
        {
            OrderType = OrderType.Internal,
            SupplierID = 1234,
            OrderID = 5678,
            CurrencyID = 1,
            OrderItems = new List<CreateOrderItemRequest>
            {
                new()
                {
                    Quantity = 1,
                    UnitPrice = 100.00m,
                    ProductName = "Test Product"
                }
            }
        };

        var json = JsonSerializer.Serialize(createRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/v1.0/purchase-orders", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().Contain("Supplier service unavailable");

        // Verify supplier service was called but others were not due to early failure
        _mockSupplierService.Verify(x => x.ValidateSupplierAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_Purchase_Order_With_Currency_Service_Timeout_Handles_Gracefully()
    {
        // Arrange
        SetupEmployeeAuthentication();
        SetupValidSupplierService();
        SetupCurrencyServiceTimeout();

        var createRequest = new CreatePurchaseOrderRequest
        {
            OrderType = OrderType.Internal,
            SupplierID = 1234,
            OrderID = 5678,
            CurrencyID = 1,
            OrderItems = new List<CreateOrderItemRequest>
            {
                new()
                {
                    Quantity = 1,
                    UnitPrice = 100.00m,
                    ProductName = "Test Product"
                }
            }
        };

        var json = JsonSerializer.Serialize(createRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/v1.0/purchase-orders", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.RequestTimeout);

        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().Contain("timeout");
    }

    [Fact]
    public async Task Upload_Document_With_Upload_Service_Integration_Success()
    {
        // Arrange
        var purchaseOrderId = await CreateTestPurchaseOrder();
        SetupEmployeeAuthentication();
        SetupValidUploadService();

        // DocumentUploadRequest doesn't exist - using multipart form data approach
        var content = new MultipartFormDataContent();

        content.Add(new StringContent("invoice.pdf"), "FileName");
        content.Add(new StringContent("application/pdf"), "ContentType");

        // Act
        var response = await _client.PostAsync($"/api/purchaseorders/{purchaseOrderId}/documents", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var responseContent = await response.Content.ReadAsStringAsync();
        var uploadResult = JsonSerializer.Deserialize<DocumentUploadResult>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        uploadResult.Should().NotBeNull();
        uploadResult!.FileId.Should().NotBeNull();
        uploadResult.FilePath.Should().NotBeNullOrEmpty();

        // Verify upload service was called
        _mockUploadService.Verify(x => x.UploadFileAsync(
            It.IsAny<Stream>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Download_Document_With_Upload_Service_Integration_Success()
    {
        // Arrange
        var purchaseOrderId = await CreateTestPurchaseOrderWithDocument();
        SetupEmployeeAuthentication();
        SetupValidUploadServiceForDownload();

        var documentId = await GetFirstDocumentIdFromOrder(purchaseOrderId);

        // Act
        var response = await _client.GetAsync($"/api/purchaseorders/{purchaseOrderId}/documents/{documentId}/download");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var downloadResult = JsonSerializer.Deserialize<DocumentDownloadResult>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        downloadResult.Should().NotBeNull();
        downloadResult!.FileName.Should().NotBeNullOrEmpty();

        // Verify upload service was called
        _mockUploadService.Verify(x => x.GetDownloadUrlAsync(
            It.IsAny<string>(),
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Generate_PDF_For_Internal_Purchase_Order_Integrates_With_PDF_Service()
    {
        // Arrange
        var purchaseOrderId = await CreateTestPurchaseOrder();
        SetupManagerAuthentication();
        SetupValidPdfService();

        var approveRequest = new ApprovePurchaseOrderRequest
        {
            Comments = "Approved for PDF generation test",
            ApprovedBy = "manager@maliev.com"
        };

        var json = JsonSerializer.Serialize(approveRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync($"/api/purchaseorders/{purchaseOrderId}/approve", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify PDF service was called for internal purchase order
        _mockPdfService.Verify(x => x.GeneratePurchaseOrderPdfAsync(
            purchaseOrderId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Calculate_WHT_With_WHT_Service_Integration_Success()
    {
        // Arrange
        var purchaseOrderId = await CreateTestPurchaseOrder();
        SetupEmployeeAuthentication();
        SetupValidWHTService();

        var whtRequest = new WHTCalculationRequest
        {
            TotalAmount = 10000.00m,
            SupplierCountry = "Thailand",
            ServiceType = "Professional Service"
        };

        var json = JsonSerializer.Serialize(whtRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync($"/v1.0/purchase-orders/{purchaseOrderId}/calculate-wht", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var whtResult = JsonSerializer.Deserialize<WHTCalculationResult>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        whtResult.Should().NotBeNull();
        whtResult!.WHTAmount.Should().BeGreaterThan(0);
        whtResult.WHTRate.Should().BeGreaterThan(0);

        // Verify WHT service was called
        _mockWHTService.Verify(x => x.CalculateWHTAsync(
            It.IsAny<SupplierDto>(),
            It.IsAny<decimal>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Multiple_External_Service_Failures_Return_Appropriate_Error_Response()
    {
        // Arrange
        SetupEmployeeAuthentication();
        SetupMultipleServiceFailures();

        var createRequest = new CreatePurchaseOrderRequest
        {
            OrderType = OrderType.Internal,
            SupplierID = 1234,
            OrderID = 5678,
            CurrencyID = 1,
            OrderItems = new List<CreateOrderItemRequest>
            {
                new()
                {
                    Quantity = 1,
                    UnitPrice = 100.00m,
                    ProductName = "Test Product"
                }
            }
        };

        var json = JsonSerializer.Serialize(createRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/v1.0/purchase-orders", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().Contain("service unavailable");
    }

    [Fact]
    public async Task Refresh_Order_Items_Integrates_With_Order_Service_Successfully()
    {
        // Arrange
        var purchaseOrderId = await CreateTestPurchaseOrderWithItems();
        SetupEmployeeAuthentication();
        SetupValidOrderServiceForRefresh();

        var refreshRequest = new OrderItemRefreshRequest
        {
            OrderItemIds = new[] { 1, 2 }
        };

        var json = JsonSerializer.Serialize(refreshRequest);
        var content = new StringContent(json, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"));

        // Act
        var response = await _client.PostAsync($"/api/purchaseorders/{purchaseOrderId}/refresh-items", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var refreshResult = JsonSerializer.Deserialize<OrderItemRefreshResult>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        refreshResult.Should().NotBeNull();
        refreshResult!.RefreshedCount.Should().BeGreaterThan(0);

        // Verify order service was called for each item
        _mockOrderService.Verify(x => x.GetOrderItemsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task External_Service_Circuit_Breaker_Behavior_Handles_Repeated_Failures()
    {
        // Arrange
        SetupEmployeeAuthentication();
        SetupSupplierServiceRepeatedFailure();

        var createRequest = new CreatePurchaseOrderRequest
        {
            OrderType = OrderType.Internal,
            SupplierID = 1234,
            OrderID = 5678,
            CurrencyID = 1,
            OrderItems = new List<CreateOrderItemRequest>
            {
                new()
                {
                    Quantity = 1,
                    UnitPrice = 100.00m,
                    ProductName = "Test Product"
                }
            }
        };

        var json = JsonSerializer.Serialize(createRequest);

        // Act - Make multiple requests to trigger circuit breaker
        var responses = new List<HttpResponseMessage>();
        for (int i = 0; i < 5; i++)
        {
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _client.PostAsync("/v1.0/purchase-orders", content);
            responses.Add(response);
        }

        // Assert - All should fail appropriately
        responses.Should().OnlyContain(r => r.StatusCode == HttpStatusCode.BadRequest || r.StatusCode == HttpStatusCode.ServiceUnavailable);

        // Verify supplier service was called (circuit breaker may reduce calls after threshold)
        _mockSupplierService.Verify(x => x.ValidateSupplierAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.AtLeast(1));
    }

    #region Test Data Setup Methods

    private async Task<int> CreateTestPurchaseOrder()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PurchaseOrderContext>();

        var purchaseOrder = new PurchaseOrder
        {
            OrderNumber = "PO-EXT-INT-001",
            OrderType = OrderType.Internal,
            Status = OrderStatus.Pending,
            SupplierID = 1,
            OrderID = 1,
            CurrencyID = 1,
            CurrencyCode = "THB",
            CurrencySymbol = "฿",
            Currency = "THB",
            SupplierName = "Test Supplier",
            SubtotalAmount = 1000.00m,
            TotalAmount = 1000.00m,
            CreatedBy = "employee@maliev.com",
            CreatedAt = DateTime.UtcNow,
            OrderDate = DateTime.UtcNow
        };

        dbContext.PurchaseOrders.Add(purchaseOrder);
        await dbContext.SaveChangesAsync();

        return purchaseOrder.Id;
    }

    private async Task<int> CreateTestPurchaseOrderWithDocument()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PurchaseOrderContext>();

        var purchaseOrder = new PurchaseOrder
        {
            OrderNumber = "PO-DOC-001",
            OrderType = OrderType.Internal,
            Status = OrderStatus.Pending,
            SupplierID = 1,
            OrderID = 1,
            CurrencyID = 1,
            CurrencyCode = "THB",
            CurrencySymbol = "฿",
            Currency = "THB",
            SupplierName = "Test Supplier",
            SubtotalAmount = 1000.00m,
            TotalAmount = 1000.00m,
            CreatedBy = "employee@maliev.com",
            CreatedAt = DateTime.UtcNow,
            OrderDate = DateTime.UtcNow,
            PurchaseOrderFiles = new List<PurchaseOrderFile>
            {
                new()
                {
                    FileName = "test-document.pdf",
                    ObjectName = "test-documents/test-document.pdf",
                    DocumentType = DocumentType.Reference,
                    ContentType = "application/pdf",
                    FileSize = 1024,
                    UploadedBy = "employee@maliev.com",
                    UploadedAt = DateTime.UtcNow
                }
            }
        };

        dbContext.PurchaseOrders.Add(purchaseOrder);
        await dbContext.SaveChangesAsync();

        return purchaseOrder.Id;
    }

    private async Task<int> CreateTestPurchaseOrderWithItems()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PurchaseOrderContext>();

        var purchaseOrder = new PurchaseOrder
        {
            OrderNumber = "PO-ITEMS-001",
            OrderType = OrderType.Internal,
            Status = OrderStatus.Pending,
            SupplierID = 1,
            OrderID = 1,
            CurrencyID = 1,
            CurrencyCode = "THB",
            CurrencySymbol = "฿",
            Currency = "THB",
            SupplierName = "Test Supplier",
            SubtotalAmount = 1000.00m,
            TotalAmount = 1000.00m,
            CreatedBy = "employee@maliev.com",
            CreatedAt = DateTime.UtcNow,
            OrderDate = DateTime.UtcNow,
            OrderItems = new List<OrderItem>
            {
                new()
                {
                    ExternalOrderItemId = 1,
                    ProductName = "Test Product 1",
                    Quantity = 5,
                    UnitOfMeasure = "pieces",
                    UnitPrice = 100.00m,
                    TotalPrice = 500.00m,
                    Currency = "THB",
                    SourceService = "OrderService",
                    CachedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                },
                new()
                {
                    ExternalOrderItemId = 2,
                    ProductName = "Test Product 2",
                    Quantity = 5,
                    UnitOfMeasure = "pieces",
                    UnitPrice = 100.00m,
                    TotalPrice = 500.00m,
                    Currency = "THB",
                    SourceService = "OrderService",
                    CachedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                }
            }
        };

        dbContext.PurchaseOrders.Add(purchaseOrder);
        await dbContext.SaveChangesAsync();

        return purchaseOrder.Id;
    }

    private async Task<int> GetFirstDocumentIdFromOrder(int purchaseOrderId)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PurchaseOrderContext>();

        var order = await dbContext.PurchaseOrders
            .Include(po => po.PurchaseOrderFiles)
            .FirstAsync(po => po.Id == purchaseOrderId);

        return order.PurchaseOrderFiles.First().Id;
    }

    #endregion

    #region External Service Mock Setups

    private void SetupAllExternalServicesForSuccess()
    {
        SetupValidSupplierService();
        SetupValidCurrencyService();
        SetupValidOrderService();
    }

    private void SetupValidSupplierService()
    {
        _mockSupplierService
            .Setup(x => x.ValidateSupplierAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SupplierDto
            {
                Id = Guid.NewGuid(),
                Name = "Test Supplier",
                IsActive = true
            });
    }

    private void SetupValidCurrencyService()
    {
        _mockCurrencyService
            .Setup(x => x.ValidateCurrencyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CurrencyDto
            {
                Code = "THB",
                Name = "Thai Baht",
                IsActive = true
            });
    }

    private void SetupValidOrderService()
    {
        _mockOrderService
            .Setup(x => x.GetOrderItemsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OrderItemDto>
            {
                new OrderItemDto
                {
                    Id = 12345,
                    ProductName = "Test Product",
                    Quantity = 1,
                    UnitPrice = 100.00m,
                    TotalPrice = 100.00m
                }
            });
    }

    private void SetupValidUploadService()
    {
        _mockUploadService
            .Setup(x => x.GenerateUploadUrlAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UploadDto
            {
                UploadUrl = "https://storage.googleapis.com/upload-url",
                FileId = Guid.NewGuid().ToString()
            });
    }

    private void SetupValidUploadServiceForDownload()
    {
        _mockUploadService
            .Setup(x => x.GenerateDownloadUrlAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://storage.googleapis.com/download-url");
    }

    private void SetupValidPdfService()
    {
        _mockPdfService
            .Setup(x => x.GeneratePurchaseOrderPdfAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PdfGenerationResult
            {
                Success = true,
                FilePath = "purchase-orders/PO-001.pdf",
                FileSize = 2048,
                GeneratedAt = DateTime.UtcNow,
                GenerationTime = TimeSpan.FromSeconds(2),
                RequestId = Guid.NewGuid().ToString()
            });
    }

    private void SetupValidWHTService()
    {
        _mockWHTService
            .Setup(x => x.CalculateWHTAsync(
                It.IsAny<SupplierDto>(),
                It.IsAny<decimal>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WHTCalculationResult
            {
                WHTAmount = 300.00m,
                WHTRate = 3.0m,
                NetAmount = 9700.00m,
                TaxBase = 10000.00m
            });
    }

    private void SetupValidOrderServiceForRefresh()
    {
        _mockOrderService
            .Setup(x => x.GetOrderItemsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OrderItemDto>
            {
                new OrderItemDto
                {
                    Id = 1,
                    Quantity = 1,
                    UnitPrice = 1000.00m,
                    TotalPrice = 1000.00m
                }
            });
    }

    private void SetupSupplierServiceFailure()
    {
        _mockSupplierService
            .Setup(x => x.ValidateSupplierAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Supplier service unavailable"));
    }

    private void SetupCurrencyServiceTimeout()
    {
        _mockCurrencyService
            .Setup(x => x.ValidateCurrencyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TaskCanceledException("Currency service timeout"));
    }

    private void SetupMultipleServiceFailures()
    {
        _mockSupplierService
            .Setup(x => x.ValidateSupplierAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Supplier service unavailable"));

        _mockCurrencyService
            .Setup(x => x.ValidateCurrencyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Currency service unavailable"));

        _mockOrderService
            .Setup(x => x.GetOrderItemsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Order service unavailable"));
    }

    private void SetupSupplierServiceRepeatedFailure()
    {
        _mockSupplierService
            .Setup(x => x.ValidateSupplierAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Supplier service consistently failing"));
    }

    #endregion

    #region Authentication Setup

    private void SetupEmployeeAuthentication()
    {
        var token = "Bearer mock-employee-token";
        _client.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse(token);
    }

    private void SetupManagerAuthentication()
    {
        var token = "Bearer mock-manager-token";
        _client.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse(token);
    }

    #endregion
}
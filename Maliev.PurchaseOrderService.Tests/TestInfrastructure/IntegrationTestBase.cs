using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Moq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Maliev.PurchaseOrderService.Data;
using Maliev.PurchaseOrderService.Data.Entities;
using Maliev.PurchaseOrderService.Api.ExternalServices;
using Maliev.PurchaseOrderService.Api.Services;
using Maliev.PurchaseOrderService.Api.DTOs;
using Maliev.PurchaseOrderService.Data.Enums;

namespace Maliev.PurchaseOrderService.Tests.TestInfrastructure;

/// <summary>
/// Base class for integration tests with common setup and utilities
/// </summary>
public abstract class IntegrationTestBase : IClassFixture<TestWebApplicationFactory<Program>>
{
    protected readonly TestWebApplicationFactory<Program> Factory;
    protected readonly HttpClient Client;
    protected readonly Mock<ISupplierServiceClient> MockSupplierService;
    protected readonly Mock<IOrderServiceClient> MockOrderService;
    protected readonly Mock<ICurrencyServiceClient> MockCurrencyService;
    // MockDomainEventService removed - using real service for database persistence
    protected readonly Mock<IUploadServiceClient> MockUploadService;
    protected readonly Mock<IPdfGenerationService> MockPdfService;
    protected readonly Mock<IWHTCalculationService> MockWHTService;
    protected readonly Mock<IDocumentManagementService> MockDocumentService;
    protected readonly JsonSerializerOptions JsonOptions;

    protected IntegrationTestBase(TestWebApplicationFactory<Program> factory)
    {
        MockSupplierService = new Mock<ISupplierServiceClient>();
        MockOrderService = new Mock<IOrderServiceClient>();
        MockCurrencyService = new Mock<ICurrencyServiceClient>();
        // MockDomainEventService initialization removed - using real service
        MockUploadService = new Mock<IUploadServiceClient>();
        MockPdfService = new Mock<IPdfGenerationService>();
        MockWHTService = new Mock<IWHTCalculationService>();
        MockDocumentService = new Mock<IDocumentManagementService>();

        // Create a unique database for each test class instance to ensure proper isolation
        var testId = $"{GetType().Name}_{Guid.NewGuid():N}";
        Factory = new TestWebApplicationFactory<Program>($"TestDatabase_{testId}");

        // Configure the test services
        Factory.ConfigureTestServices = services =>
        {
            // Replace external service clients with mocks (keep domain event service real for database persistence)
            TestWebApplicationFactory<Program>.ReplaceService(services, MockSupplierService.Object);
            TestWebApplicationFactory<Program>.ReplaceService(services, MockOrderService.Object);
            TestWebApplicationFactory<Program>.ReplaceService(services, MockCurrencyService.Object);
            TestWebApplicationFactory<Program>.ReplaceService(services, MockUploadService.Object);
            TestWebApplicationFactory<Program>.ReplaceService(services, MockPdfService.Object);
            TestWebApplicationFactory<Program>.ReplaceService(services, MockWHTService.Object);
            TestWebApplicationFactory<Program>.ReplaceService(services, MockDocumentService.Object);

            // Configure additional test services
            ConfigureAdditionalTestServices(services);
        };

        // Create client with proper API base address
        Client = Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost/")
        });
        JsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        SetupCommonMocks();
        InitializeDatabase();
    }

    /// <summary>
    /// Initialize InMemory database for testing with proper isolation
    /// </summary>
    private void InitializeDatabase()
    {
        try
        {
            using var scope = Factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<PurchaseOrderContext>();

            // For InMemory database, we don't need to delete/recreate, just ensure it's created
            context.Database.EnsureCreated();

            // Clear any existing data for proper isolation
            ClearAllTestData(context);

            // Reset the TestDataFactory sequence for consistent IDs
            TestDataFactory.ResetIdSequence(1);
        }
        catch (Exception ex)
        {
            // If database initialization fails, log and continue
            // This prevents test failures due to database setup issues
            System.Diagnostics.Debug.WriteLine($"Database initialization warning: {ex.Message}");
        }
    }

    /// <summary>
    /// Clear all test data from the database for test isolation
    /// </summary>
    private void ClearAllTestData(PurchaseOrderContext context)
    {
        try
        {
            // For InMemory database, clear data using EF Core methods
            // Remove all test data in correct order to avoid foreign key constraints
            context.OrderItems.RemoveRange(context.OrderItems);
            context.PurchaseOrderFiles.RemoveRange(context.PurchaseOrderFiles);
            context.PurchaseOrders.RemoveRange(context.PurchaseOrders);
            context.Addresses.RemoveRange(context.Addresses);
            context.AuditLogs.RemoveRange(context.AuditLogs);
            context.DomainEvents.RemoveRange(context.DomainEvents);

            context.SaveChanges();
        }
        catch
        {
            // If clearing data fails, it's likely because the database schema doesn't exist yet
            // This can be safely ignored on initial database creation
        }
    }

    /// <summary>
    /// Override this method to configure additional test services
    /// </summary>
    protected virtual void ConfigureAdditionalTestServices(IServiceCollection services)
    {
        // Override in derived classes if needed
    }

    /// <summary>
    /// Resets test data sequence for consistent test runs
    /// </summary>
    protected virtual void ResetTestDataSequence()
    {
        TestDataFactory.ResetIdSequence();
    }

    /// <summary>
    /// Setup common mock behaviors that most tests need
    /// </summary>
    protected virtual void SetupCommonMocks()
    {
        // Default supplier validation with consistent test data
        var defaultSupplier = TestDataFactory.CreateSupplierDto();
        MockSupplierService
            .Setup(x => x.ValidateSupplierAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(defaultSupplier);

        // Default supplier lookup with same consistent data
        MockSupplierService
            .Setup(x => x.GetSupplierAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(defaultSupplier);

        // Default currency validation with consistent test data
        var defaultCurrency = TestDataFactory.CreateCurrencyDto("THB", "Thai Baht");
        MockCurrencyService
            .Setup(x => x.ValidateCurrencyAsync("THB", It.IsAny<CancellationToken>()))
            .ReturnsAsync(defaultCurrency);
        MockCurrencyService
            .Setup(x => x.ValidateCurrencyAsync("USD", It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestDataFactory.CreateCurrencyDto("USD", "US Dollar"));

        // Default order items with consistent test data matching scenarios
        MockOrderService
            .Setup(x => x.GetOrderItemsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int orderId, CancellationToken _) => new List<OrderItemDto>
            {
                TestDataFactory.CreateOrderItemDto(
                    id: 1,
                    purchaseOrderId: 1,
                    externalOrderItemId: 1,
                    quantity: 2,
                    unitPrice: 150.00m),
                TestDataFactory.CreateOrderItemDto(
                    id: 2,
                    purchaseOrderId: 1,
                    externalOrderItemId: 2,
                    quantity: 1,
                    unitPrice: 300.00m)
            });

        // Order validation (missing from IntegrationTestBase)
        MockOrderService
            .Setup(x => x.ValidateOrderForPurchaseOrderAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Use real domain event service for database persistence (no mock setup needed)

        // Default WHT calculation matching expected test scenario totals (600 subtotal)
        MockWHTService
            .Setup(x => x.CalculateWHTAsync(It.IsAny<SupplierDto>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SupplierDto supplier, decimal amount, string currency, CancellationToken _) => new WHTCalculationResult
            {
                WHTAmount = Math.Round(amount * 0.03m, 2), // 3% of subtotal
                NetAmount = Math.Round(amount * 0.97m, 2), // Amount minus WHT
                WHTRate = 3.0m, // 3% stored as percentage
                IsApplicable = true,
                SubtotalAmount = amount,
                TaxBase = amount,
                CurrencyCode = currency,
                WHTAmountTHB = Math.Round(amount * 0.03m, 2),
                Reason = "Standard 3% WHT for Thailand suppliers"
            });

        // Default document management service mocks
        SetupDocumentManagementMocks();

        // Default PDF generation service mocks
        SetupPdfGenerationMocks();
    }

    /// <summary>
    /// Sets up common document management service mocks
    /// </summary>
    private void SetupDocumentManagementMocks()
    {
        // Default file validation - valid
        MockDocumentService
            .Setup(x => x.ValidateFile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>()))
            .Returns(new DocumentValidationResult { IsValid = true, Errors = new List<string>() });

        // Default document metadata
        MockDocumentService
            .Setup(x => x.GetDocumentMetadataAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int fileId, CancellationToken _) => new PurchaseOrderFileDto
            {
                Id = fileId,
                PurchaseOrderId = 1,
                FileName = $"test-document-{fileId}.pdf",
                ContentType = "application/pdf",
                FileSize = 1024,
                UploadedBy = "test-user",
                UploadedAt = DateTime.UtcNow
            });

        // Default upload result
        MockDocumentService
            .Setup(x => x.UploadDocumentAsync(It.IsAny<int>(), It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int poId, Stream stream, string fileName, string contentType, string uploadedBy, CancellationToken _) => new DocumentUploadResult
            {
                Success = true,
                FileId = 1,
                FileSize = stream.Length,
                UploadedBy = uploadedBy
            });

        // Default download result
        MockDocumentService
            .Setup(x => x.DownloadDocumentAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DocumentDownloadResult
            {
                Success = true,
                FileName = "test-document.pdf",
                ContentType = "application/pdf",
                FileStream = new MemoryStream(Encoding.UTF8.GetBytes("Test PDF content"))
            });

        // Default delete operation
        MockDocumentService
            .Setup(x => x.DeleteDocumentAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Default update operation
        MockDocumentService
            .Setup(x => x.UpdateDocumentAsync(It.IsAny<int>(), It.IsAny<UpdateDocumentRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int fileId, UpdateDocumentRequest request, string updatedBy, CancellationToken _) => new PurchaseOrderFileDto
            {
                Id = fileId,
                PurchaseOrderId = 1,
                FileName = request.FileName ?? $"updated-document-{fileId}.pdf",
                ContentType = "application/pdf",
                FileSize = 1024,
                UploadedBy = "test-user",
                UploadedAt = DateTime.UtcNow
            });

        // Default preview URL generation
        MockDocumentService
            .Setup(x => x.GeneratePreviewUrlAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://preview.example.com/document/1");

        // Default documents list
        MockDocumentService
            .Setup(x => x.GetDocumentsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PurchaseOrderFileDto>
            {
                new PurchaseOrderFileDto
                {
                    Id = 1,
                    PurchaseOrderId = 1,
                    FileName = "test-document.pdf",
                    ContentType = "application/pdf",
                    FileSize = 1024,
                    UploadedBy = "test-user",
                    UploadedAt = DateTime.UtcNow
                }
            });
    }

    /// <summary>
    /// Sets up common PDF generation service mocks
    /// </summary>
    private void SetupPdfGenerationMocks()
    {
        // Default PDF generation - success
        MockPdfService
            .Setup(x => x.GeneratePurchaseOrderPdfAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PdfGenerationResult
            {
                Success = true,
                FileSize = 2048,
                GeneratedAt = DateTime.UtcNow,
                GenerationTime = TimeSpan.FromSeconds(2),
                RequestId = Guid.NewGuid().ToString(),
                IsAsync = false,
                FilePath = "/storage/pdfs/purchase-order.pdf"
            });

        // Default PDF status
        MockPdfService
            .Setup(x => x.GetPdfGenerationStatusAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PdfGenerationStatus
            {
                Status = PdfStatus.Completed,
                LastAttempt = DateTime.UtcNow,
                AttemptCount = 1,
                IsApplicable = true
            });

        // Default PDF applicability check - applicable for internal orders
        MockPdfService
            .Setup(x => x.IsPdfGenerationApplicable(It.IsAny<PurchaseOrderDto>()))
            .Returns(true);

        // Default PDF download URL
        MockPdfService
            .Setup(x => x.GetPdfDownloadUrlAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://storage.example.com/pdf/po-123.pdf");
    }

    #region Authentication Helpers

    protected void SetupEmployeeAuthentication(string userId = "emp123", string department = "department1")
    {
        var token = TestJwtHelper.GenerateEmployeeToken(userId, department);
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    protected void SetupManagerAuthentication(string userId = "mgr123", string department = "department1")
    {
        var token = TestJwtHelper.GenerateManagerToken(userId, department);
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    protected void SetupProcurementAuthentication(string userId = "proc123", string department = "procurement")
    {
        var token = TestJwtHelper.GenerateProcurementToken(userId, department);
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    protected void SetupAdminAuthentication(string userId = "admin123", string department = "admin")
    {
        var token = TestJwtHelper.GenerateAdminToken(userId, department);
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    protected void SetupExpiredAuthentication(string userId = "expired123", string role = "Employee")
    {
        var token = TestJwtHelper.GenerateExpiredToken(userId, role);
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    protected void ClearAuthentication()
    {
        Client.DefaultRequestHeaders.Authorization = null;
    }

    #endregion

    #region External Service Mocking Helpers

    protected virtual void SetupExternalServiceMocks()
    {
        // This method can be overridden by derived classes for specific mock setups
        // The default mocks are already set up in the constructor
    }

    #endregion

    #region HTTP Helpers

    protected async Task<HttpResponseMessage> PostAsJsonAsync<T>(string requestUri, T value)
    {
        var json = JsonSerializer.Serialize(value, JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"));
        return await Client.PostAsync(requestUri, content);
    }

    protected async Task<HttpResponseMessage> PutAsJsonAsync<T>(string requestUri, T value)
    {
        var json = JsonSerializer.Serialize(value, JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"));
        return await Client.PutAsync(requestUri, content);
    }

    protected async Task<T?> DeserializeResponseAsync<T>(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(content, JsonOptions);
    }

    #endregion

    #region Database Helpers

    protected Task<PurchaseOrderContext> GetDbContextAsync()
    {
        var scope = Factory.Services.CreateScope();
        return Task.FromResult(scope.ServiceProvider.GetRequiredService<PurchaseOrderContext>());
    }

    protected async Task ExecuteInDbContextAsync(Func<PurchaseOrderContext, Task> action)
    {
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PurchaseOrderContext>();
        await action(dbContext);
    }

    protected async Task<T> ExecuteInDbContextAsync<T>(Func<PurchaseOrderContext, Task<T>> func)
    {
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PurchaseOrderContext>();
        return await func(dbContext);
    }

    #endregion

    #region Enhanced Test Data Creation Helpers

    /// <summary>
    /// Creates a purchase order request using enhanced TestDataFactory scenarios
    /// </summary>
    protected CreatePurchaseOrderRequest CreateBasicPurchaseOrderRequest(
        OrderType orderType = OrderType.Internal,
        string? userContext = null)
    {
        if (orderType == OrderType.Internal)
        {
            var (request, _, _, _) = TestDataFactory.CreateEmployeeInternalPOScenario(userContext ?? "test-employee");
            return request;
        }
        else
        {
            var (request, _, _, _) = TestDataFactory.CreateExternalPOScenario();
            return request;
        }
    }

    /// <summary>
    /// Creates a complete purchase order request with all dependencies set up
    /// </summary>
    protected (CreatePurchaseOrderRequest request, SupplierDto supplier, CurrencyDto currency, List<OrderItemDto> orderItems)
        CreateCompletePurchaseOrderScenario(OrderType orderType = OrderType.Internal, string? userContext = null)
    {
        return orderType == OrderType.Internal
            ? TestDataFactory.CreateEmployeeInternalPOScenario(userContext ?? "test-employee")
            : TestDataFactory.CreateExternalPOScenario();
    }

    /// <summary>
    /// Creates an address request using the enhanced factory
    /// </summary>
    protected CreateAddressRequest CreateBasicAddressRequest(Data.Enums.AddressType addressType = Data.Enums.AddressType.Shipping)
    {
        return TestDataFactory.CreateAddressRequest(addressType);
    }

    /// <summary>
    /// Sets up external service mocks for a complete purchase order scenario
    /// </summary>
    protected void SetupMocksForScenario(
        SupplierDto supplier,
        CurrencyDto currency,
        List<OrderItemDto> orderItems,
        bool includeWHTCalculation = true)
    {
        // Setup supplier service
        MockSupplierService
            .Setup(x => x.ValidateSupplierAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(supplier);
        MockSupplierService
            .Setup(x => x.GetSupplierAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(supplier);

        // Setup currency service
        MockCurrencyService
            .Setup(x => x.ValidateCurrencyAsync(currency.Code, It.IsAny<CancellationToken>()))
            .ReturnsAsync(currency);

        // Setup order service
        MockOrderService
            .Setup(x => x.GetOrderItemsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(orderItems);
        MockOrderService
            .Setup(x => x.ValidateOrderForPurchaseOrderAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Setup WHT calculation if needed
        if (includeWHTCalculation)
        {
            var whtResult = TestDataFactory.CreateWHTCalculationResult(
                orderItems.Sum(i => i.TotalPrice),
                3.0m // 3%
            );
            MockWHTService
                .Setup(x => x.CalculateWHTAsync(It.IsAny<SupplierDto>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(whtResult);
        }
    }

    #endregion

    #region Test Isolation Helpers

    /// <summary>
    /// Ensures a clean database state for the current test
    /// Call this at the beginning of tests that need isolated state
    /// </summary>
    protected async Task EnsureCleanDatabaseAsync()
    {
        await ClearTestDataAsync();
        TestDataFactory.ResetIdSequence(1);
        ResetMocks(); // Also reset mocks for complete isolation
    }

    /// <summary>
    /// Ensures database is ready and optionally seeds test data
    /// This is a comprehensive setup method for integration tests
    /// </summary>
    protected async Task PrepareTestAsync(bool seedData = false, bool resetMocks = true)
    {
        await EnsureCleanDatabaseAsync();

        if (resetMocks)
        {
            ResetMocks();
        }

        if (seedData)
        {
            await SeedTestDataAsync();
        }
    }

    /// <summary>
    /// Resets all mocks to their default state
    /// Call this if you need to reset mock configurations during a test
    /// </summary>
    protected void ResetMocks()
    {
        MockSupplierService.Reset();
        MockOrderService.Reset();
        MockCurrencyService.Reset();
        MockUploadService.Reset();
        MockPdfService.Reset();
        MockWHTService.Reset();
        MockDocumentService.Reset();
        SetupCommonMocks();
    }

    #endregion

    #region Database Seeding Helpers

    /// <summary>
    /// Seeds the database with comprehensive test data with predictable IDs for scenarios that need existing purchase orders
    /// </summary>
    protected async Task SeedTestDataAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PurchaseOrderContext>();

        // Ensure database is created and clean
        await dbContext.Database.EnsureCreatedAsync();

        // Don't seed if data already exists
        if (await dbContext.PurchaseOrders.AnyAsync())
        {
            return;
        }

        try
        {
            // Reset sequence to ensure predictable IDs
            TestDataFactory.ResetIdSequence(1);

            // Create predictable test scenarios with consistent data that tests expect
            var scenarios = new[]
            {
                // Scenario 1: Internal, Pending (most tests expect this) - Predictable amounts: 1*150 + 2*300 = 750, WHT: 22.50, Total: 727.50
                TestDataFactory.CreateCompletePOScenarioForSeeding(OrderType.Internal, OrderStatus.Pending, "emp123", 2),
                // Scenario 2: External, Approved - Predictable amounts: 1*150 = 150, No WHT, Total: 150
                TestDataFactory.CreateCompletePOScenarioForSeeding(OrderType.External, OrderStatus.Approved, "mgr456", 1),
                // Scenario 3: Internal, Cancelled - Predictable amounts: 1*150 + 2*300 + 3*450 = 2100, WHT: 63, Total: 2037
                TestDataFactory.CreateCompletePOScenarioForSeeding(OrderType.Internal, OrderStatus.Cancelled, "emp789", 3)
            };

            // Seed addresses first ensuring proper data consistency
            var allAddresses = new List<Address>();
            foreach (var (_, _, shippingAddr, billingAddr, _, _) in scenarios)
            {
                if (shippingAddr != null)
                {
                    shippingAddr.Id = 0; // Let database assign sequential IDs
                    allAddresses.Add(shippingAddr);
                }
                if (billingAddr != null)
                {
                    billingAddr.Id = 0; // Let database assign sequential IDs
                    allAddresses.Add(billingAddr);
                }
            }

            if (allAddresses.Count > 0)
            {
                await dbContext.Addresses.AddRangeAsync(allAddresses);
                await dbContext.SaveChangesAsync();
                // Database assigns sequential IDs starting from 1, 2, 3...
            }

            // Seed purchase orders with proper address relationships
            var allPurchaseOrders = new List<PurchaseOrder>();
            int addressIndex = 0;
            foreach (var (purchaseOrder, _, shippingAddr, billingAddr, _, _) in scenarios)
            {
                purchaseOrder.Id = 0; // Let database assign
                if (shippingAddr != null)
                {
                    purchaseOrder.ShippingAddressId = allAddresses[addressIndex].Id;
                    addressIndex++;
                }
                if (billingAddr != null)
                {
                    purchaseOrder.BillingAddressId = allAddresses[addressIndex].Id;
                    addressIndex++;
                }
                allPurchaseOrders.Add(purchaseOrder);
            }

            await dbContext.PurchaseOrders.AddRangeAsync(allPurchaseOrders);
            await dbContext.SaveChangesAsync();
            // Database assigns sequential IDs, addresses are already linked by foreign keys

            // Seed order items with predictable IDs and proper foreign key relationships
            var allOrderItems = new List<OrderItem>();
            foreach (var (purchaseOrder, orderItems, _, _, _, _) in scenarios)
            {
                foreach (var item in orderItems)
                {
                    item.Id = 0; // Let database assign
                    item.PurchaseOrderId = purchaseOrder.Id; // Use the updated ID
                    allOrderItems.Add(item);
                }
            }

            if (allOrderItems.Count > 0)
            {
                await dbContext.OrderItems.AddRangeAsync(allOrderItems);
                await dbContext.SaveChangesAsync();
                // Order items now linked to purchase orders with correct totals calculated
            }

            // Add test files for document management scenarios
            var testFiles = new List<PurchaseOrderFile>();
            foreach (var (purchaseOrder, _, _, _, _, _) in scenarios.Take(2)) // Add files to first 2 POs
            {
                var file = TestDataFactory.CreatePurchaseOrderFileEntity(
                    id: 0, // Let database assign sequential IDs
                    purchaseOrderId: purchaseOrder.Id,
                    fileName: $"PO-{purchaseOrder.OrderNumber}-Document.pdf",
                    uploadedBy: purchaseOrder.CreatedBy
                );
                testFiles.Add(file);
            }

            if (testFiles.Count > 0)
            {
                await dbContext.PurchaseOrderFiles.AddRangeAsync(testFiles);
                await dbContext.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            // Log the specific error for debugging
            System.Diagnostics.Debug.WriteLine($"SeedTestDataAsync failed: {ex.Message}");

            // Clean up partial data on error
            try
            {
                await ClearTestDataAsync();
            }
            catch
            {
                // Ignore cleanup errors
            }
            throw;
        }
    }

    /// <summary>
    /// Seeds minimal test data with specific scenarios for focused tests
    /// </summary>
    protected async Task SeedMinimalTestDataAsync(params (OrderType orderType, OrderStatus status, string createdBy)[] scenarios)
    {
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PurchaseOrderContext>();

        await dbContext.Database.EnsureCreatedAsync();

        try
        {
            foreach (var (orderType, status, createdBy) in scenarios)
            {
                var (purchaseOrder, orderItems, shippingAddr, billingAddr, _, _) =
                    TestDataFactory.CreateCompletePOScenarioForSeeding(orderType, status, createdBy, 2);

                // Add addresses
                var addresses = new List<Address>();
                if (shippingAddr != null) addresses.Add(shippingAddr);
                if (billingAddr != null) addresses.Add(billingAddr);

                if (addresses.Count > 0)
                {
                    await dbContext.Addresses.AddRangeAsync(addresses);
                    await dbContext.SaveChangesAsync();
                }

                // Set foreign keys and add purchase order
                purchaseOrder.ShippingAddressId = shippingAddr?.Id;
                purchaseOrder.BillingAddressId = billingAddr?.Id;
                await dbContext.PurchaseOrders.AddAsync(purchaseOrder);
                await dbContext.SaveChangesAsync();

                // Add order items
                foreach (var item in orderItems)
                    item.PurchaseOrderId = purchaseOrder.Id;

                await dbContext.OrderItems.AddRangeAsync(orderItems);
                await dbContext.SaveChangesAsync();
            }
        }
        catch
        {
            try
            {
                ClearAllTestData(dbContext);
            }
            catch { }
            throw;
        }
    }

    /// <summary>
    /// Seeds a specific purchase order for tests that need a known entity with enhanced scenario support and predictable ID
    /// </summary>
    protected async Task<PurchaseOrder> SeedPurchaseOrderAsync(
        OrderType orderType = OrderType.Internal,
        OrderStatus status = OrderStatus.Pending,
        string createdBy = "emp123",
        int itemCount = 2,
        int? expectedId = null)
    {
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PurchaseOrderContext>();

        await dbContext.Database.EnsureCreatedAsync();

        try
        {
            // Use the enhanced factory method
            var (purchaseOrder, orderItems, shippingAddress, billingAddress, supplier, currency) =
                TestDataFactory.CreateCompletePOScenarioForSeeding(orderType, status, createdBy, itemCount);

            // Assign predictable IDs
            var nextAddressId = (await dbContext.Addresses.CountAsync()) + 1;
            var nextPOId = expectedId ?? ((await dbContext.PurchaseOrders.CountAsync()) + 1);
            var nextItemId = (await dbContext.OrderItems.CountAsync()) + 1;

            shippingAddress.Id = nextAddressId;
            billingAddress.Id = nextAddressId + 1;
            purchaseOrder.Id = nextPOId;

            // Add addresses first
            var addresses = new List<Address> { shippingAddress, billingAddress };
            await dbContext.Addresses.AddRangeAsync(addresses);
            await dbContext.SaveChangesAsync();

            // Set address foreign keys
            purchaseOrder.ShippingAddressId = shippingAddress.Id;
            purchaseOrder.BillingAddressId = billingAddress.Id;

            // Add purchase order
            await dbContext.PurchaseOrders.AddAsync(purchaseOrder);
            await dbContext.SaveChangesAsync();

            // Set order item IDs and foreign keys
            foreach (var item in orderItems)
            {
                item.Id = nextItemId++;
                item.PurchaseOrderId = purchaseOrder.Id;
            }

            await dbContext.OrderItems.AddRangeAsync(orderItems);
            await dbContext.SaveChangesAsync();

            return purchaseOrder;
        }
        catch
        {
            try
            {
                ClearAllTestData(dbContext);
            }
            catch { }
            throw;
        }
    }

    /// <summary>
    /// Seeds multiple purchase orders with different statuses for comprehensive testing
    /// </summary>
    protected async Task<(PurchaseOrder pending, PurchaseOrder approved, PurchaseOrder cancelled)> SeedMultiStatusPurchaseOrdersAsync(
        string createdBy = "emp123")
    {
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PurchaseOrderContext>();

        await dbContext.Database.EnsureCreatedAsync();

        try
        {
            // Create three purchase orders with different statuses
            var scenarios = new[]
            {
                TestDataFactory.CreateCompletePOScenarioForSeeding(OrderType.Internal, OrderStatus.Pending, createdBy, 2),
                TestDataFactory.CreateCompletePOScenarioForSeeding(OrderType.External, OrderStatus.Approved, $"mgr-{createdBy}", 1),
                TestDataFactory.CreateCompletePOScenarioForSeeding(OrderType.Internal, OrderStatus.Cancelled, createdBy, 3)
            };

            // Seed all addresses first
            var allAddresses = new List<Address>();
            foreach (var (_, _, shipping, billing, _, _) in scenarios)
            {
                allAddresses.AddRange(new[] { shipping, billing });
            }
            await dbContext.Addresses.AddRangeAsync(allAddresses);
            await dbContext.SaveChangesAsync();

            // Seed all purchase orders
            var purchaseOrders = new List<PurchaseOrder>();
            foreach (var (po, _, shipping, billing, _, _) in scenarios)
            {
                po.ShippingAddressId = shipping.Id;
                po.BillingAddressId = billing.Id;
                purchaseOrders.Add(po);
            }
            await dbContext.PurchaseOrders.AddRangeAsync(purchaseOrders);
            await dbContext.SaveChangesAsync();

            // Seed all order items
            var allOrderItems = new List<OrderItem>();
            foreach (var (po, items, _, _, _, _) in scenarios)
            {
                foreach (var item in items)
                {
                    item.PurchaseOrderId = po.Id;
                    allOrderItems.Add(item);
                }
            }
            await dbContext.OrderItems.AddRangeAsync(allOrderItems);
            await dbContext.SaveChangesAsync();

            return (purchaseOrders[0], purchaseOrders[1], purchaseOrders[2]);
        }
        catch
        {
            try
            {
                ClearAllTestData(dbContext);
            }
            catch { }
            throw;
        }
    }

    /// <summary>
    /// Clears all test data from the database
    /// </summary>
    protected async Task ClearTestDataAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PurchaseOrderContext>();

        // For InMemory database, we don't need transactions (they're ignored anyway)
        try
        {
            // Use IgnoreQueryFilters to get soft-deleted entities too
            var orderItems = await dbContext.OrderItems.IgnoreQueryFilters().ToListAsync();
            var purchaseOrderFiles = await dbContext.PurchaseOrderFiles.IgnoreQueryFilters().ToListAsync();
            var purchaseOrders = await dbContext.PurchaseOrders.IgnoreQueryFilters().ToListAsync();
            var addresses = await dbContext.Addresses.IgnoreQueryFilters().ToListAsync();
            var auditLogs = await dbContext.AuditLogs.ToListAsync();
            var domainEvents = await dbContext.DomainEvents.ToListAsync();

            // Remove in correct order to avoid foreign key constraints
            if (orderItems.Count > 0)
                dbContext.OrderItems.RemoveRange(orderItems);
            if (purchaseOrderFiles.Count > 0)
                dbContext.PurchaseOrderFiles.RemoveRange(purchaseOrderFiles);
            if (purchaseOrders.Count > 0)
                dbContext.PurchaseOrders.RemoveRange(purchaseOrders);
            if (addresses.Count > 0)
                dbContext.Addresses.RemoveRange(addresses);
            if (auditLogs.Count > 0)
                dbContext.AuditLogs.RemoveRange(auditLogs);
            if (domainEvents.Count > 0)
                dbContext.DomainEvents.RemoveRange(domainEvents);

            await dbContext.SaveChangesAsync();
        }
        catch
        {
            // For InMemory database, we can't rollback
            // Since this is already a cleanup method, we'll just let the exception propagate
            throw;
        }
    }

    /// <summary>
    /// Gets a seeded purchase order for testing with comprehensive includes
    /// </summary>
    protected async Task<PurchaseOrder?> GetSeededPurchaseOrderAsync(OrderStatus? status = null, OrderType? orderType = null)
    {
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PurchaseOrderContext>();

        var query = dbContext.PurchaseOrders
            .Include(po => po.OrderItems)
            .Include(po => po.ShippingAddress)
            .Include(po => po.BillingAddress)
            .Include(po => po.PurchaseOrderFiles)
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(po => po.Status == status.Value);
        if (orderType.HasValue)
            query = query.Where(po => po.OrderType == orderType.Value);

        return await query.FirstOrDefaultAsync();
    }

    /// <summary>
    /// Gets all seeded purchase orders with full data for comprehensive testing
    /// </summary>
    protected async Task<List<PurchaseOrder>> GetAllSeededPurchaseOrdersAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PurchaseOrderContext>();

        return await dbContext.PurchaseOrders
            .Include(po => po.OrderItems)
            .Include(po => po.ShippingAddress)
            .Include(po => po.BillingAddress)
            .Include(po => po.PurchaseOrderFiles)
            .OrderBy(po => po.Id)
            .ToListAsync();
    }

    /// <summary>
    /// Gets a purchase order by ID with full includes for testing
    /// </summary>
    protected async Task<PurchaseOrder?> GetPurchaseOrderByIdAsync(int id)
    {
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PurchaseOrderContext>();

        return await dbContext.PurchaseOrders
            .Include(po => po.OrderItems)
            .Include(po => po.ShippingAddress)
            .Include(po => po.BillingAddress)
            .Include(po => po.PurchaseOrderFiles)
            .FirstOrDefaultAsync(po => po.Id == id);
    }

    /// <summary>
    /// Gets purchase order count by status for validation
    /// </summary>
    protected async Task<int> GetPurchaseOrderCountByStatusAsync(OrderStatus status)
    {
        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PurchaseOrderContext>();

        return await dbContext.PurchaseOrders.CountAsync(po => po.Status == status);
    }

    #endregion

}
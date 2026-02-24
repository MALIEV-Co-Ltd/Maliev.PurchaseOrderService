using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Maliev.PurchaseOrderService.Api.DTOs;
using Maliev.PurchaseOrderService.Api.Services;
using Maliev.PurchaseOrderService.Common.Enumerations;
using Microsoft.EntityFrameworkCore;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using Xunit;

namespace Maliev.PurchaseOrderService.Tests.Integration;

/// <summary>
/// Integration tests for PurchaseOrdersController using Testcontainers and WireMock
/// Tests cover T033, T040, and T047
/// </summary>
[Trait("Category", "Integration")]
public class PurchaseOrdersControllerTests : IntegrationTestBase
{
    #region T033: Integration tests for POST /purchase-orders/v1/purchase-orders

    [Fact]
    public async Task CreatePurchaseOrder_WithValidRequest_ReturnsCreatedOrder()
    {
        // Arrange
        // Use a new client for this test to avoid sharing the base Client
        var client = Factory.CreateAuthenticatedClient("user123", permissions: new[] { PurchaseOrderPermissions.Orders.Create, PurchaseOrderPermissions.Orders.Read });

        // Mock external service responses
        SupplierServiceMock!
            .Given(Request.Create().WithPath("/v1/suppliers/1").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBodyAsJson(new
                {
                    id = 1,
                    name = "Test Supplier",
                    contactInfo = "test@supplier.com",
                    isActive = true
                }));

        OrderServiceMock!
            .Given(Request.Create().WithPath("/v1/orders/100").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBodyAsJson(new
                {
                    id = 100,
                    orderNumber = "ORD-100"
                }));

        OrderServiceMock!
            .Given(Request.Create().WithPath("/v1/orders/100/items").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBodyAsJson(new[]
                {
                    new { id = 1001, productName = "Product 1", quantity = 10, unitPrice = 100.00m, totalPrice = 1000.00m, currency = "THB", unitOfMeasure = "pcs" }
                }));

        CurrencyServiceMock!
            .Given(Request.Create().WithPath("/v1/currencies/1").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBodyAsJson(new
                {
                    id = 1,
                    code = "THB",
                    symbol = "฿",
                    rate = 1.0m,
                    isActive = true
                }));

        var request = new CreatePurchaseOrderRequest
        {
            SupplierID = 1,
            OrderID = 100,
            CurrencyID = 1,
            OrderType = OrderType.External,
            WHTRate = 3.0m,
            ShippingAddress = new CreateAddressRequest
            {
                AddressType = AddressType.Shipping,
                ContactName = "Test Contact",
                AddressLine1 = "123 Test St",
                City = "Bangkok",
                PostalCode = "10110",
                Country = "Thailand"
            }
        };

        // Act
        var response = await client.PostAsJsonAsync("/purchase-order/v1/purchase-orders", request);

        // Assert
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
        }
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PurchaseOrderResponse>();
        Assert.NotNull(result);
        Assert.Equal(1, result!.SupplierID);
        Assert.Equal(100, result.OrderID);
        Assert.Equal(OrderStatus.Pending, result.Status);
        Assert.NotNull(result.OrderNumber);
        Assert.NotEmpty(result.OrderNumber);
        Assert.True(result.TotalAmount > 0);
    }

    [Fact]
    public async Task CreatePurchaseOrder_WithInvalidSupplier_ReturnsBadRequest()
    {
        // Arrange
        var client = Factory.CreateAuthenticatedClient("user123", permissions: new[] { PurchaseOrderPermissions.Orders.Create });

        // Mock supplier not found
        SupplierServiceMock!
            .Given(Request.Create().WithPath("/v1/suppliers/9999").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(404));

        var request = new CreatePurchaseOrderRequest
        {
            SupplierID = 9999,
            OrderID = 100,
            CurrencyID = 1,
            OrderType = OrderType.External,
            WHTRate = 3.0m
        };

        // Act
        var response = await client.PostAsJsonAsync("/purchase-order/v1/purchase-orders", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreatePurchaseOrder_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        var request = new CreatePurchaseOrderRequest
        {
            SupplierID = 1,
            OrderID = 100,
            CurrencyID = 1,
            OrderType = OrderType.External,
            WHTRate = 3.0m
        };

        // Act
        var response = await Client!.PostAsJsonAsync("/purchase-order/v1/purchase-orders", request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreatePurchaseOrder_WithPartialOrdering_CreatesOrderWithSelectedItems()
    {
        // Arrange
        var client = Factory.CreateAuthenticatedClient("user123", permissions: new[] { PurchaseOrderPermissions.Orders.Create, PurchaseOrderPermissions.Orders.Read });

        // Mock responses
        SupplierServiceMock!
            .Given(Request.Create().WithPath("/v1/suppliers/2").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBodyAsJson(new
                {
                    id = 2,
                    name = "Supplier 2",
                    contactInfo = "supplier2@test.com",
                    isActive = true
                }));

        OrderServiceMock!
            .Given(Request.Create().WithPath("/v1/orders/200").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBodyAsJson(new
                {
                    id = 200,
                    orderNumber = "ORD-200"
                }));

        OrderServiceMock!
            .Given(Request.Create().WithPath("/v1/orders/200/items").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBodyAsJson(new[]
                {
                    new { id = 2001, productName = "Item 1", quantity = 10, unitPrice = 100.00m, totalPrice = 1000.00m, currency = "USD", unitOfMeasure = "pcs" },
                    new { id = 2002, productName = "Item 2", quantity = 5, unitPrice = 200.00m, totalPrice = 1000.00m, currency = "USD", unitOfMeasure = "pcs" },
                    new { id = 2003, productName = "Item 3", quantity = 2, unitPrice = 500.00m, totalPrice = 1000.00m, currency = "USD", unitOfMeasure = "pcs" }
                }));

        CurrencyServiceMock!
            .Given(Request.Create().WithPath("/v1/currencies/2").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBodyAsJson(new
                {
                    id = 2,
                    code = "USD",
                    symbol = "$",
                    rate = 35.0m,
                    isActive = true
                }));

        var request = new CreatePurchaseOrderRequest
        {
            SupplierID = 2,
            OrderID = 200,
            CurrencyID = 2,
            OrderType = OrderType.External,
            WHTRate = 3.0m,
            Items = new List<PartialOrderItem>
            {
                new() { ExternalOrderItemId = 2001, Quantity = 8 },
                new() { ExternalOrderItemId = 2003, Quantity = 1 }
            }
        };

        // Act
        var response = await client.PostAsJsonAsync("/purchase-order/v1/purchase-orders", request);

        // Assert
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
        }
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PurchaseOrderResponse>();
        Assert.NotNull(result);

        // Verify only selected items are in the PO
        var dbContext = GetDbContext();
        var po = await dbContext.PurchaseOrders
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == result!.Id);

        Assert.NotNull(po);
        Assert.Equal(2, po!.Items.Count);
    }

    #endregion

    #region T040: Integration tests for GET endpoints

    [Fact]
    public async Task GetPurchaseOrderById_WithExistingOrder_ReturnsOrder()
    {
        // Arrange
        var client = Factory.CreateAuthenticatedClient("user123", permissions: new[] { PurchaseOrderPermissions.Orders.Read });

        // Create test data
        var dbContext = GetDbContext();
        var po = new Data.Entities.PurchaseOrder
        {
            OrderNumber = "PO-2025-001",
            SupplierID = 1,
            SupplierName = "Test Supplier",
            OrderID = 100,
            CurrencyID = 1,
            CurrencyCode = "THB",
            CurrencySymbol = "฿",
            OrderType = OrderType.External,
            DepartmentId = 1,
            Status = OrderStatus.Pending,
            OrderDate = DateTime.UtcNow,
            WHTRate = 3.0m,
            SubtotalAmount = 1000.00m,
            WHTAmount = 30.00m,
            TotalAmount = 970.00m,
            CreatedBy = "user123",
            CreatedAt = DateTime.UtcNow
            // RowVersion will be auto-generated by the database
        };
        dbContext.PurchaseOrders.Add(po);
        await dbContext.SaveChangesAsync();

        // Act
        var response = await client.GetAsync($"/purchase-order/v1/purchase-orders/{po.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PurchaseOrderDetailResponse>();
        Assert.NotNull(result);
        Assert.Equal(po.Id, result!.Id);
        Assert.Equal("PO-2025-001", result.OrderNumber);
        Assert.Equal(1, result.SupplierID);
    }

    [Fact]
    public async Task GetPurchaseOrderById_WithNonExistingOrder_ReturnsNotFound()
    {
        // Arrange
        var client = Factory.CreateAuthenticatedClient("user123", permissions: new[] { PurchaseOrderPermissions.Orders.Read });

        // Act
        var response = await client.GetAsync("/purchase-order/v1/purchase-orders/99999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetPurchaseOrderById_AsEmployee_CanOnlySeeOwnOrders()
    {
        // Arrange
        var dbContext = GetDbContext();

        // Create PO owned by another user
        var otherUserPO = new Data.Entities.PurchaseOrder
        {
            OrderNumber = "PO-2025-002",
            SupplierID = 1,
            SupplierName = "Test Supplier",
            OrderID = 100,
            CurrencyID = 1,
            CurrencyCode = "THB",
            CurrencySymbol = "฿",
            OrderType = OrderType.External,
            DepartmentId = 1,
            Status = OrderStatus.Pending,
            OrderDate = DateTime.UtcNow,
            WHTRate = 3.0m,
            SubtotalAmount = 1000.00m,
            WHTAmount = 30.00m,
            TotalAmount = 970.00m,
            CreatedBy = "otheruser",
            CreatedAt = DateTime.UtcNow
        };
        dbContext.PurchaseOrders.Add(otherUserPO);
        await dbContext.SaveChangesAsync();

        // Use 'employee' role via permissions (simulated as not having enough permission or being restricted by ownership)
        var client = Factory.CreateAuthenticatedClient("user123", roles: new[] { "employee" }, permissions: new[] { PurchaseOrderPermissions.Orders.Read });

        // Act
        var response = await client.GetAsync($"/purchase-order/v1/purchase-orders/{otherUserPO.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task SearchPurchaseOrders_WithFilters_ReturnsFilteredResults()
    {
        // Arrange
        var client = Factory.CreateAuthenticatedClient("user123", permissions: new[] { PurchaseOrderPermissions.Orders.Read });

        // Create test data
        var dbContext = GetDbContext();
        var po1 = new Data.Entities.PurchaseOrder
        {
            OrderNumber = "PO-2025-003",
            SupplierID = 1,
            SupplierName = "Supplier 1",
            OrderID = 100,
            CurrencyID = 1,
            CurrencyCode = "THB",
            CurrencySymbol = "฿",
            OrderType = OrderType.External,
            DepartmentId = 1,
            Status = OrderStatus.Pending,
            OrderDate = DateTime.UtcNow,
            WHTRate = 3.0m,
            SubtotalAmount = 1000.00m,
            WHTAmount = 30.00m,
            TotalAmount = 970.00m,
            CreatedBy = "user123",
            CreatedAt = DateTime.UtcNow
        };

        var po2 = new Data.Entities.PurchaseOrder
        {
            OrderNumber = "PO-2025-004",
            SupplierID = 2,
            SupplierName = "Supplier 2",
            OrderID = 200,
            CurrencyID = 2,
            CurrencyCode = "USD",
            CurrencySymbol = "$",
            OrderType = OrderType.Internal,
            Status = OrderStatus.Approved,
            OrderDate = DateTime.UtcNow,
            WHTRate = 3.0m,
            SubtotalAmount = 2000.00m,
            WHTAmount = 60.00m,
            TotalAmount = 1940.00m,
            CreatedBy = "user456",
            CreatedAt = DateTime.UtcNow
        };

        dbContext.PurchaseOrders.AddRange(po1, po2);
        await dbContext.SaveChangesAsync();

        // Act - Search for Pending status
        var response = await client.GetAsync("/purchase-order/v1/purchase-orders?Status=Pending&Page=1&PageSize=10");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<PurchaseOrderResponse>>();
        Assert.NotNull(result);
        Assert.Single(result!.Items);
        Assert.Equal(OrderStatus.Pending, result.Items.First().Status);
    }

    [Fact]
    public async Task SearchPurchaseOrders_WithPagination_ReturnsPagedResults()
    {
        // Arrange
        var client = Factory.CreateAuthenticatedClient("user123", permissions: new[] { PurchaseOrderPermissions.Orders.Read });

        // Create test data - 15 purchase orders
        var dbContext = GetDbContext();
        var purchaseOrders = Enumerable.Range(1, 15).Select(i => new Data.Entities.PurchaseOrder
        {
            OrderNumber = $"PO-2025-{i:D3}",
            SupplierID = 1,
            SupplierName = "Test Supplier",
            OrderID = 100 + i,
            CurrencyID = 1,
            CurrencyCode = "THB",
            CurrencySymbol = "฿",
            OrderType = OrderType.External,
            DepartmentId = 1,
            Status = OrderStatus.Pending,
            OrderDate = DateTime.UtcNow,
            WHTRate = 3.0m,
            SubtotalAmount = 1000.00m * i,
            WHTAmount = 30.00m * i,
            TotalAmount = 970.00m * i,
            CreatedBy = "user123",
            CreatedAt = DateTime.UtcNow.AddDays(-i)
        }).ToList();

        dbContext.PurchaseOrders.AddRange(purchaseOrders);
        await dbContext.SaveChangesAsync();

        // Act - Get page 1 with 10 items
        var response = await client.GetAsync("/purchase-order/v1/purchase-orders?Page=1&PageSize=10");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<PurchaseOrderResponse>>();
        Assert.NotNull(result);
        Assert.Equal(10, result!.Items.Count);
        Assert.Equal(15, result.TotalCount);
        Assert.Equal(1, result.Page);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(2, result.TotalPages);
    }

    #endregion

    #region T047: Integration tests for PUT and cancel endpoints

    [Fact]
    public async Task UpdatePurchaseOrder_WithValidRequest_UpdatesOrder()
    {
        // Arrange
        var client = Factory.CreateAuthenticatedClient("user123", permissions: new[] { PurchaseOrderPermissions.Orders.Update, PurchaseOrderPermissions.Orders.Read });

        // Create test data
        var dbContext = GetDbContext();
        var po = new Data.Entities.PurchaseOrder
        {
            OrderNumber = "PO-2025-010",
            SupplierID = 1,
            SupplierName = "Test Supplier",
            OrderID = 100,
            CurrencyID = 1,
            CurrencyCode = "THB",
            CurrencySymbol = "฿",
            OrderType = OrderType.External,
            DepartmentId = 1,
            Status = OrderStatus.Pending,
            OrderDate = DateTime.UtcNow,
            WHTRate = 3.0m,
            SubtotalAmount = 1000.00m,
            WHTAmount = 30.00m,
            TotalAmount = 970.00m,
            CreatedBy = "user123",
            CreatedAt = DateTime.UtcNow
        };
        dbContext.PurchaseOrders.Add(po);
        await dbContext.SaveChangesAsync();

        // Verify persistence
        var checkContext = GetDbContext();
        var savedPo = await checkContext.PurchaseOrders.FirstOrDefaultAsync(p => p.Id == po.Id);
        Assert.NotNull(savedPo);
        Assert.False(savedPo!.IsDeleted);
        Assert.Equal("user123", savedPo.CreatedBy);
        Assert.True(savedPo.Id > 0);

        // Mock currency service for updated currency
        CurrencyServiceMock!
            .Given(Request.Create().WithPath("/v1/currencies/2").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBodyAsJson(new
                {
                    id = 2,
                    code = "USD",
                    symbol = "$",
                    rate = 35.0m,
                    isActive = true
                }));

        var updateRequest = new UpdatePurchaseOrderRequest
        {
            CurrencyID = 2,
            WHTRate = 5.0m,
            RowVersion = po.RowVersion.ToString()
        };

        // Act
        var response = await client.PutAsJsonAsync($"/purchase-order/v1/purchase-orders/{po.Id}", updateRequest);

        // Assert
        // Assert
        if (response.StatusCode != HttpStatusCode.OK)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
        }
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PurchaseOrderResponse>();
        Assert.NotNull(result);
        Assert.Equal(2, result!.CurrencyID);
        Assert.Equal(5.0m, result.WHTRate);
    }

    [Fact]
    public async Task UpdatePurchaseOrder_WithConcurrencyConflict_ReturnsConflict()
    {
        // Arrange
        var client = Factory.CreateAuthenticatedClient("user123", permissions: new[] { PurchaseOrderPermissions.Orders.Update });

        // Create test data
        var dbContext = GetDbContext();
        var po = new Data.Entities.PurchaseOrder
        {
            OrderNumber = "PO-2025-011",
            SupplierID = 1,
            SupplierName = "Test Supplier",
            OrderID = 100,
            CurrencyID = 1,
            CurrencyCode = "THB",
            CurrencySymbol = "฿",
            OrderType = OrderType.External,
            DepartmentId = 1,
            Status = OrderStatus.Pending,
            OrderDate = DateTime.UtcNow,
            WHTRate = 3.0m,
            SubtotalAmount = 1000.00m,
            WHTAmount = 30.00m,
            TotalAmount = 970.00m,
            CreatedBy = "user123",
            CreatedAt = DateTime.UtcNow
        };
        dbContext.PurchaseOrders.Add(po);
        await dbContext.SaveChangesAsync();

        var updateRequest = new UpdatePurchaseOrderRequest
        {
            WHTRate = 5.0m,
            RowVersion = (po.RowVersion + 1).ToString()
        };

        // Act
        var response = await client.PutAsJsonAsync($"/purchase-order/v1/purchase-orders/{po.Id}", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task UpdatePurchaseOrder_AsEmployee_ReturnsForbidden()
    {
        // Arrange
        var client = Factory.CreateAuthenticatedClient("user123", roles: new[] { "employee" }, permissions: new[] { "some.other.permission" });

        var updateRequest = new UpdatePurchaseOrderRequest
        {
            WHTRate = 5.0m,
            RowVersion = "1234"
        };

        // Act
        var response = await client.PutAsJsonAsync("/purchase-order/v1/purchase-orders/1", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CancelPurchaseOrder_WithExistingOrder_CancelsOrder()
    {
        // Arrange
        var client = Factory.CreateAuthenticatedClient("user123", permissions: new[] { PurchaseOrderPermissions.Orders.Cancel });

        // Create test data
        var dbContext = GetDbContext();
        var po = new Data.Entities.PurchaseOrder
        {
            OrderNumber = "PO-2025-012",
            SupplierID = 1,
            SupplierName = "Test Supplier",
            OrderID = 100,
            CurrencyID = 1,
            CurrencyCode = "THB",
            CurrencySymbol = "฿",
            OrderType = OrderType.External,
            DepartmentId = 1,
            Status = OrderStatus.Pending,
            OrderDate = DateTime.UtcNow,
            WHTRate = 3.0m,
            SubtotalAmount = 1000.00m,
            WHTAmount = 30.00m,
            TotalAmount = 970.00m,
            CreatedBy = "user123",
            CreatedAt = DateTime.UtcNow
        };
        dbContext.PurchaseOrders.Add(po);
        await dbContext.SaveChangesAsync();

        // Act
        var response = await client.PostAsJsonAsync($"/purchase-order/v1/purchase-orders/{po.Id}/cancel", new CancelPurchaseOrderRequest { Reason = "Test cancellation" });

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify status changed in database
        await dbContext.Entry(po).ReloadAsync();
        Assert.Equal(OrderStatus.Cancelled, po.Status);
    }

    [Fact]
    public async Task CancelPurchaseOrder_WithNonExistingOrder_ReturnsNotFound()
    {
        // Arrange
        var client = Factory.CreateAuthenticatedClient("user123", permissions: new[] { PurchaseOrderPermissions.Orders.Cancel });

        // Act
        var response = await client.PostAsJsonAsync("/purchase-order/v1/purchase-orders/99999/cancel", new CancelPurchaseOrderRequest { Reason = "Test cancellation" });

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CancelPurchaseOrder_AsEmployee_ReturnsForbidden()
    {
        // Arrange
        var client = Factory.CreateAuthenticatedClient("user123", roles: new[] { "employee" }, permissions: new[] { "some.other.permission" });

        // Act
        var response = await client.PostAsJsonAsync("/purchase-order/v1/purchase-orders/1/cancel", new CancelPurchaseOrderRequest { Reason = "Test cancellation" });

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    #endregion
}

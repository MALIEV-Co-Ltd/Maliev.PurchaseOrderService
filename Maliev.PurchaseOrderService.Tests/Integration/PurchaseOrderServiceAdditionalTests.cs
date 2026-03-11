using Maliev.PurchaseOrderService.Domain.Constants;
using Maliev.PurchaseOrderService.Infrastructure.Persistence;
using Maliev.PurchaseOrderService.Domain.Entities;
using System.Net;
using System.Net.Http.Json;
using Maliev.PurchaseOrderService.Application.DTOs;
using Maliev.PurchaseOrderService.Application.Interfaces;
using Maliev.PurchaseOrderService.Domain.Enumerations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using Xunit;

namespace Maliev.PurchaseOrderService.Tests.Integration;

[Trait("Category", "Integration")]
public class PurchaseOrderServiceAdditionalTests : IntegrationTestBase
{
    [Fact]
    public async Task ApprovePurchaseOrder_WithValidStatus_ReturnsOk()
    {
        // Arrange
        var client = Factory.CreateAuthenticatedClient("user123", permissions: new[] { PurchaseOrderPermissions.Orders.Approve, PurchaseOrderPermissions.Orders.Read });
        var dbContext = GetDbContext();

        var po = new Domain.Entities.PurchaseOrder
        {
            OrderNumber = "PO-APPROVE-TEST",
            SupplierID = 1,
            OrderID = 100,
            CurrencyID = 1,
            CurrencyCode = "THB",
            Status = OrderStatus.Pending,
            CreatedBy = "user123",
            CreatedAt = DateTime.UtcNow
        };
        dbContext.PurchaseOrders.Add(po);
        await dbContext.SaveChangesAsync();

        // Act
        var response = await client.PostAsync($"/purchase-order/v1/purchase-orders/{po.Id}/approve", null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PurchaseOrderDetailResponse>();
        Assert.NotNull(result);
        Assert.Equal("Approved", result!.Status);
    }

    [Fact]
    public async Task SendToSupplier_WithApprovedStatus_ReturnsOk()
    {
        // Arrange
        var client = Factory.CreateAuthenticatedClient("user123", permissions: new[] { PurchaseOrderPermissions.Orders.Send, PurchaseOrderPermissions.Orders.Read });
        var dbContext = GetDbContext();

        var po = new Domain.Entities.PurchaseOrder
        {
            OrderNumber = "PO-SEND-TEST",
            SupplierID = 1,
            OrderID = 100,
            CurrencyID = 1,
            CurrencyCode = "THB",
            Status = OrderStatus.Approved,
            CreatedBy = "user123",
            CreatedAt = DateTime.UtcNow
        };
        dbContext.PurchaseOrders.Add(po);
        await dbContext.SaveChangesAsync();

        // Act
        var response = await client.PostAsync($"/purchase-order/v1/purchase-orders/{po.Id}/send-to-supplier", null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PurchaseOrderDetailResponse>();
        Assert.NotNull(result);
        Assert.Equal("Ordered", result!.Status);
    }

    [Fact]
    public async Task ReceiveItems_FullReceipt_ReturnsDelivered()
    {
        // Arrange
        var client = Factory.CreateAuthenticatedClient("user123", permissions: new[] { PurchaseOrderPermissions.Orders.Receive, PurchaseOrderPermissions.Orders.Read });
        var dbContext = GetDbContext();

        var po = new Domain.Entities.PurchaseOrder
        {
            OrderNumber = "PO-RECEIVE-TEST",
            SupplierID = 1,
            OrderID = 100,
            CurrencyID = 1,
            CurrencyCode = "THB",
            Status = OrderStatus.Ordered,
            CreatedBy = "user123",
            CreatedAt = DateTime.UtcNow
        };
        dbContext.PurchaseOrders.Add(po);
        await dbContext.SaveChangesAsync();

        // Act
        var response = await client.PostAsync($"/purchase-order/v1/purchase-orders/{po.Id}/receive?isPartialReceipt=false", null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PurchaseOrderDetailResponse>();
        Assert.NotNull(result);
        Assert.Equal("Delivered", result!.Status);
    }

    [Fact]
    public async Task ExportPurchaseOrder_ReturnsOk()
    {
        // Arrange
        var client = Factory.CreateAuthenticatedClient("user123", permissions: new[] { PurchaseOrderPermissions.Orders.Export });

        // Act
        var response = await client.GetAsync("/purchase-order/v1/purchase-orders/1/export?format=pdf");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AuditLogService_ShouldPersistLog()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var auditService = scope.ServiceProvider.GetRequiredService<IAuditLogService>();
        var dbContext = scope.ServiceProvider.GetRequiredService<PurchaseOrderContext>();

        // Act
        await auditService.LogAuditAsync("TestEntity", "123", AuditAction.Create, "user1", "admin", "old", "new", "reason");

        // Assert
        var log = await dbContext.AuditLogs.FirstOrDefaultAsync(l => l.EntityId == "123");
        Assert.NotNull(log);
        Assert.Equal("TestEntity", log.EntityType);
        Assert.Equal(AuditAction.Create, log.Action);
        Assert.Equal("old", log.OldValues);
        Assert.Equal("new", log.NewValues);
        Assert.Equal("reason", log.ChangeReason);
    }

    [Fact]
    public async Task UpdatePurchaseOrder_WithAddress_UpdatesSuccessfully()
    {
        // Arrange
        var client = Factory.CreateAuthenticatedClient("user123", permissions: new[] { PurchaseOrderPermissions.Orders.Update, PurchaseOrderPermissions.Orders.Read });
        var dbContext = GetDbContext();

        var po = new Domain.Entities.PurchaseOrder
        {
            OrderNumber = "PO-UPDATE-ADDR",
            SupplierID = 1,
            OrderID = 100,
            CurrencyID = 1,
            CurrencyCode = "THB",
            CreatedBy = "user123",
            CreatedAt = DateTime.UtcNow
        };
        dbContext.PurchaseOrders.Add(po);
        await dbContext.SaveChangesAsync();

        var updateRequest = new UpdatePurchaseOrderRequest
        {
            ShippingAddress = new UpdateAddressRequest(
                AddressType.Shipping,
                null,
                "New Contact",
                "New Address",
                null,
                "Bangkok",
                null,
                null,
                "Thailand",
                null,
                null)
        };

        // Act
        var response = await client.PutAsJsonAsync($"/purchase-order/v1/purchase-orders/{po.Id}", updateRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PurchaseOrderDetailResponse>();
        Assert.NotNull(result);
        Assert.NotNull(result!.ShippingAddress);
        Assert.Equal("New Contact", result.ShippingAddress!.ContactName);
    }

    [Fact]
    public async Task ExternalServiceClients_ErrorPaths_ShouldBeHandled()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var supplierClient = scope.ServiceProvider.GetRequiredService<ISupplierServiceClient>();
        var orderClient = scope.ServiceProvider.GetRequiredService<IOrderServiceClient>();
        var permissionService = scope.ServiceProvider.GetRequiredService<IUserPermissionService>();

        // Mock 500 errors
        SupplierServiceMock.Given(Request.Create().WithPath("/v1/suppliers/999").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(500));
        OrderServiceMock.Given(Request.Create().WithPath("/v1/orders/999").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(500));
        IAMServiceMock.Given(Request.Create().WithPath("/v1/users/bad-user/permissions").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(500));

        // Act & Assert
        var supplier = await supplierClient.GetSupplierAsync(999);
        Assert.Null(supplier);

        var order = await orderClient.GetOrderAsync(999);
        Assert.Null(order);

        var permissions = await permissionService.GetUserPermissionsAsync("bad-user");
        Assert.Empty(permissions);

        var supplierExists = await supplierClient.ValidateSupplierExistsAsync(999);
        Assert.False(supplierExists);

        var orderExists = await orderClient.ValidateOrderExistsAsync(999);
        Assert.False(orderExists);
    }
}

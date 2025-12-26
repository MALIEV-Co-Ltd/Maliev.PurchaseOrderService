using Maliev.PurchaseOrderService.Api.Services;
using Maliev.PurchaseOrderService.Api.DTOs;
using Maliev.PurchaseOrderService.Common.Enumerations;
using System.Net;
using System.Net.Http.Json;
using Xunit;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace Maliev.PurchaseOrderService.Tests.Integration;

public class PurchaseOrdersControllerPermissionTests : IntegrationTestBase
{
    private void SetupExternalServiceMocks()
    {
        // NOTE: The base URL in TestWebApplicationFactory already includes "/v1/" or "/iam/v1/"
        
        SupplierServiceMock.Given(Request.Create().WithPath("/v1/suppliers/1").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithBodyAsJson(new { id = 1, name = "Supplier 1" }));

        OrderServiceMock.Given(Request.Create().WithPath("/v1/orders/1").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithBodyAsJson(new { id = 1, orderNumber = "ORD-1" }));

        OrderServiceMock.Given(Request.Create().WithPath("/v1/orders/1/items").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithBodyAsJson(new[] 
            { 
                new { id = 1, productCode = "P1", productName = "Product 1", quantity = 10, unitPrice = 100, totalPrice = 1000, currency = "THB", unitOfMeasure = "pcs" } 
            }));

        CurrencyServiceMock.Given(Request.Create().WithPath("/v1/currencies/1").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(200).WithBodyAsJson(new { id = 1, code = "THB", symbol = "B" }));
            
        IAMServiceMock.Given(Request.Create().WithPath("/iam/v1/permissions/register").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(200));
            
        IAMServiceMock.Given(Request.Create().WithPath("/iam/v1/roles/register").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(200));
    }

    [Fact]
    public async Task CreatePurchaseOrder_WithCreatePermission_ShouldSucceed()
    {
        // Arrange
        SetupExternalServiceMocks();
        var client = CreateAuthenticatedClient(permissions: new[] { PurchaseOrderPermissions.Orders.Create, PurchaseOrderPermissions.Orders.Read });

        var request = new CreatePurchaseOrderRequest
        {
            SupplierID = 1,
            OrderID = 1,
            CurrencyID = 1,
            OrderType = OrderType.Internal,
            Items = new List<PartialOrderItem>
            {
                new() { ExternalOrderItemId = 1, Quantity = 1 }
            }
        };

        // Act
        var response = await client.PostAsJsonAsync("/purchase-order/v1/purchase-orders", request);

        // Assert
        if (response.StatusCode != HttpStatusCode.Created)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Create failed with {response.StatusCode}: {error}. Request Path: {response.RequestMessage?.RequestUri}");
        }
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreatePurchaseOrder_WithoutPermission_ShouldReturnForbidden()
    {
        // Arrange
        var client = CreateAuthenticatedClient(permissions: new[] { "some.other.permission" });

        var request = new CreatePurchaseOrderRequest
        {
            SupplierID = 1,
            OrderID = 1,
            CurrencyID = 1,
            OrderType = OrderType.Internal
        };

        // Act
        var response = await client.PostAsJsonAsync("/purchase-order/v1/purchase-orders", request);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeletePurchaseOrder_WithAdminPermission_ShouldSucceed()
    {
        // Arrange
        SetupExternalServiceMocks();
        var setupClient = CreateAuthenticatedClient(permissions: new[] { PurchaseOrderPermissions.Orders.Create, PurchaseOrderPermissions.Orders.Read });
        
        var createRequest = new CreatePurchaseOrderRequest
        {
            SupplierID = 1,
            OrderID = 1,
            CurrencyID = 1,
            OrderType = OrderType.Internal
        };
        var createResponse = await setupClient.PostAsJsonAsync("/purchase-order/v1/purchase-orders", createRequest);
        var po = await createResponse.Content.ReadFromJsonAsync<PurchaseOrderDetailResponse>();

        var client = CreateAuthenticatedClient(permissions: new[] { PurchaseOrderPermissions.Orders.Delete });

        // Act
        var response = await client.DeleteAsync($"/purchase-order/v1/purchase-orders/{po!.Id}");

        // Assert
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetPurchaseOrders_WithWildcardPermission_ShouldSucceed()
    {
        // Arrange
        var client = CreateAuthenticatedClient(permissions: new[] { "purchase-order.*" });

        // Act
        var response = await client.GetAsync("/purchase-order/v1/purchase-orders");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ApprovePurchaseOrder_WithApprovePermission_ShouldSucceed()
    {
        // Arrange
        SetupExternalServiceMocks();
        var setupClient = CreateAuthenticatedClient(permissions: new[] { PurchaseOrderPermissions.Orders.Create, PurchaseOrderPermissions.Orders.Read });
        
        var createRequest = new CreatePurchaseOrderRequest
        {
            SupplierID = 1,
            OrderID = 1,
            CurrencyID = 1,
            OrderType = OrderType.Internal
        };
        var createResponse = await setupClient.PostAsJsonAsync("/purchase-order/v1/purchase-orders", createRequest);
        var po = await createResponse.Content.ReadFromJsonAsync<PurchaseOrderDetailResponse>();

        var client = CreateAuthenticatedClient(permissions: new[] { PurchaseOrderPermissions.Orders.Approve });

        // Act
        var response = await client.PostAsync($"/purchase-order/v1/purchase-orders/{po!.Id}/approve", null);

        // Assert
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CancelPurchaseOrder_WithCancelPermission_ShouldSucceed()
    {
        // Arrange
        SetupExternalServiceMocks();
        var setupClient = CreateAuthenticatedClient(permissions: new[] { PurchaseOrderPermissions.Orders.Create, PurchaseOrderPermissions.Orders.Read });
        
        var createRequest = new CreatePurchaseOrderRequest
        {
            SupplierID = 1,
            OrderID = 1,
            CurrencyID = 1,
            OrderType = OrderType.Internal
        };
        var createResponse = await setupClient.PostAsJsonAsync("/purchase-order/v1/purchase-orders", createRequest);
        var po = await createResponse.Content.ReadFromJsonAsync<PurchaseOrderDetailResponse>();

        var client = CreateAuthenticatedClient(permissions: new[] { PurchaseOrderPermissions.Orders.Cancel });

        // Act
        var response = await client.PostAsync($"/purchase-order/v1/purchase-orders/{po!.Id}/cancel", null);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task ReceiveItems_WithReceivePermission_ShouldSucceed()
    {
        // Arrange
        SetupExternalServiceMocks();
        var setupClient = CreateAuthenticatedClient(permissions: new[] { PurchaseOrderPermissions.Orders.Create, PurchaseOrderPermissions.Orders.Read });
        
        var createRequest = new CreatePurchaseOrderRequest
        {
            SupplierID = 1,
            OrderID = 1,
            CurrencyID = 1,
            OrderType = OrderType.Internal
        };
        var createResponse = await setupClient.PostAsJsonAsync("/purchase-order/v1/purchase-orders", createRequest);
        var po = await createResponse.Content.ReadFromJsonAsync<PurchaseOrderDetailResponse>();

        var client = CreateAuthenticatedClient(permissions: new[] { PurchaseOrderPermissions.Orders.Receive });

        // Act
        var response = await client.PostAsync($"/purchase-order/v1/purchase-orders/{po!.Id}/receive", null);

        // Assert
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }
}

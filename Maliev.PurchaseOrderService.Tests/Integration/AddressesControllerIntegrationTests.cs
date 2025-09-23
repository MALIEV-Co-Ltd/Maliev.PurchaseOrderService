using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;
using System.Net;
using Xunit;
using FluentAssertions;
using Maliev.PurchaseOrderService.Api;
using Maliev.PurchaseOrderService.Api.DTOs;
using Maliev.PurchaseOrderService.Data;
using Maliev.PurchaseOrderService.Data.Entities;
using Maliev.PurchaseOrderService.Data.Enums;
using Maliev.PurchaseOrderService.Tests.TestInfrastructure;

namespace Maliev.PurchaseOrderService.Tests.Integration;

/// <summary>
/// Comprehensive integration tests for AddressesController
/// Tests address CRUD operations, validation, and authorization
/// </summary>
public class AddressesControllerIntegrationTests : IntegrationTestBase
{
    public AddressesControllerIntegrationTests(TestWebApplicationFactory<Program> factory) : base(factory)
    {
    }

    #region GET /v1/addresses Tests

    [Fact]
    public async Task GetAddresses_WithEmployeeAuth_ShouldReturnAllAddresses()
    {
        // Arrange
        SetupEmployeeAuthentication();
        await SeedTestDataAsync();

        // Act
        var response = await Client.GetAsync("/v1/addresses");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<List<AddressDto>>(responseContent, JsonOptions);

        result.Should().NotBeNull();
        result!.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetAddresses_WithAddressTypeFilter_ShouldReturnFilteredAddresses()
    {
        // Arrange
        SetupEmployeeAuthentication();
        await SeedTestDataAsync();

        // Act
        var response = await Client.GetAsync("/v1/addresses?type=Shipping");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<List<AddressDto>>(responseContent, JsonOptions);

        result.Should().NotBeNull();
        result!.Should().OnlyContain(addr => addr.AddressType == AddressType.Shipping);
    }

    [Fact]
    public async Task GetAddresses_WithBillingTypeFilter_ShouldReturnBillingAddresses()
    {
        // Arrange
        SetupEmployeeAuthentication();
        await SeedTestDataAsync();

        // Act
        var response = await Client.GetAsync("/v1/addresses?type=Billing");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<List<AddressDto>>(responseContent, JsonOptions);

        result.Should().NotBeNull();
        result!.Should().OnlyContain(addr => addr.AddressType == AddressType.Billing);
    }

    [Fact]
    public async Task GetAddresses_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        ClearAuthentication();

        // Act
        var response = await Client.GetAsync("/v1/addresses");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region GET /v1/addresses/{addressId} Tests

    [Fact]
    public async Task GetAddress_WithValidIds_ShouldReturnAddress()
    {
        // Arrange
        SetupEmployeeAuthentication();
        var seededPurchaseOrder = await SeedPurchaseOrderAsync();

        // Get shipping address ID
        var addressId = await ExecuteInDbContextAsync(async dbContext =>
        {
            var po = await dbContext.PurchaseOrders
                .Include(po => po.ShippingAddress)
                .FirstAsync(po => po.Id == seededPurchaseOrder.Id);

            return po.ShippingAddress!.Id;
        });

        // Act
        var response = await Client.GetAsync($"/v1/addresses/{addressId}?purchaseOrderId={seededPurchaseOrder.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<AddressDto>(responseContent, JsonOptions);

        result.Should().NotBeNull();
        result!.Id.Should().Be(addressId);
    }

    [Fact]
    public async Task GetAddress_WithInvalidPurchaseOrderId_ShouldReturnNotFound()
    {
        // Arrange
        SetupEmployeeAuthentication();

        // Act
        var response = await Client.GetAsync("/v1/addresses/1?purchaseOrderId=99999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var responseContent = await response.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseContent, JsonOptions);

        errorResponse.Should().NotBeNull();
        errorResponse!.Error.Code.Should().Be("PURCHASE_ORDER_NOT_FOUND");
    }

    [Fact]
    public async Task GetAddress_WithUnassociatedAddressId_ShouldReturnNotFound()
    {
        // Arrange
        SetupEmployeeAuthentication();
        var seededPurchaseOrder = await SeedPurchaseOrderAsync();

        // Act - Try to access an address that doesn't belong to this purchase order
        var response = await Client.GetAsync($"/v1/addresses/99999?purchaseOrderId={seededPurchaseOrder.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var responseContent = await response.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseContent, JsonOptions);

        errorResponse.Should().NotBeNull();
        errorResponse!.Error.Code.Should().Be("ADDRESS_NOT_FOUND");
    }

    #endregion

    #region POST /v1/addresses Tests

    [Fact]
    public async Task CreateAddress_WithValidRequest_ShouldCreateSuccessfully()
    {
        // Arrange
        SetupEmployeeAuthentication();
        var request = CreateBasicAddressRequest(AddressType.Shipping);

        // Act
        var response = await PostAsJsonAsync("/v1/addresses", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<AddressDto>(responseContent, JsonOptions);

        result.Should().NotBeNull();
        result!.AddressType.Should().Be(AddressType.Shipping);
        result.ContactName.Should().Be(request.ContactName);
        result.AddressLine1.Should().Be(request.AddressLine1);
        result.City.Should().Be(request.City);
        result.Country.Should().Be(request.Country);

        // Verify the address was created in the database
        await ExecuteInDbContextAsync(async dbContext =>
        {
            var createdAddress = await dbContext.Addresses.FindAsync(result.Id);
            createdAddress.Should().NotBeNull();
            createdAddress!.ContactName.Should().Be(request.ContactName);
        });
    }

    [Fact]
    public async Task CreateAddress_WithBillingType_ShouldCreateBillingAddress()
    {
        // Arrange
        SetupEmployeeAuthentication();
        var request = CreateBasicAddressRequest(AddressType.Billing);
        request.CompanyName = "Test Company Ltd.";

        // Act
        var response = await PostAsJsonAsync("/v1/addresses", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<AddressDto>(responseContent, JsonOptions);

        result.Should().NotBeNull();
        result!.AddressType.Should().Be(AddressType.Billing);
        result.CompanyName.Should().Be("Test Company Ltd.");
    }

    [Fact]
    public async Task CreateAddress_WithInvalidRequest_ShouldReturnBadRequest()
    {
        // Arrange
        SetupEmployeeAuthentication();
        var request = new CreateAddressRequest(); // Empty request

        // Act
        var response = await PostAsJsonAsync("/v1/addresses", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var responseContent = await response.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseContent, JsonOptions);

        errorResponse.Should().NotBeNull();
        errorResponse!.Error.Code.Should().Be("INVALID_REQUEST");
        errorResponse.Error.Details.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CreateAddress_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        ClearAuthentication();
        var request = CreateBasicAddressRequest();

        // Act
        var response = await PostAsJsonAsync("/v1/addresses", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region PUT /v1/addresses/{addressId} Tests

    [Fact]
    public async Task UpdateAddress_WithValidRequest_ShouldUpdateSuccessfully()
    {
        // Arrange
        SetupEmployeeAuthentication("emp123");
        var seededPurchaseOrder = await SeedPurchaseOrderAsync(OrderType.Internal, OrderStatus.Pending);

        var addressId = await ExecuteInDbContextAsync(async dbContext =>
        {
            var po = await dbContext.PurchaseOrders
                .Include(po => po.ShippingAddress)
                .FirstAsync(po => po.Id == seededPurchaseOrder.Id);

            return po.ShippingAddress!.Id;
        });

        var updateRequest = new UpdateAddressRequest
        {
            ContactName = "Updated Contact Name",
            AddressLine1 = "456 Updated Street",
            City = "Updated City",
            PostalCode = "12345",
            PhoneNumber = "+66-2-555-9999"
        };

        // Act
        var response = await PutAsJsonAsync($"/v1/addresses/{addressId}?purchaseOrderId={seededPurchaseOrder.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<AddressDto>(responseContent, JsonOptions);

        result.Should().NotBeNull();
        result!.ContactName.Should().Be("Updated Contact Name");
        result.AddressLine1.Should().Be("456 Updated Street");
        result.City.Should().Be("Updated City");
        result.PostalCode.Should().Be("12345");
        result.PhoneNumber.Should().Be("+66-2-555-9999");
        result.UpdatedAt.Should().NotBeNull();

        // Verify audit log was created
        await ExecuteInDbContextAsync(async dbContext =>
        {
            var auditLogs = await dbContext.AuditLogs
                .Where(a => a.EntityId == seededPurchaseOrder.Id.ToString() && a.Action == AuditAction.Update)
                .ToListAsync();

            auditLogs.Should().Contain(log => log.ChangeReason.Contains("Address updated"));
        });
    }

    [Fact]
    public async Task UpdateAddress_WithApprovedPurchaseOrder_ShouldReturnBadRequest()
    {
        // Arrange
        SetupEmployeeAuthentication();
        var seededPurchaseOrder = await SeedPurchaseOrderAsync(OrderType.Internal, OrderStatus.Approved);

        var addressId = await ExecuteInDbContextAsync(async dbContext =>
        {
            var po = await dbContext.PurchaseOrders
                .Include(po => po.ShippingAddress)
                .FirstAsync(po => po.Id == seededPurchaseOrder.Id);

            return po.ShippingAddress!.Id;
        });

        var updateRequest = new UpdateAddressRequest
        {
            ContactName = "Updated Contact Name"
        };

        // Act
        var response = await PutAsJsonAsync($"/v1/addresses/{addressId}?purchaseOrderId={seededPurchaseOrder.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var responseContent = await response.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseContent, JsonOptions);

        errorResponse.Should().NotBeNull();
        errorResponse!.Error.Code.Should().Be("INVALID_PURCHASE_ORDER_STATUS");
        errorResponse.Error.Message.Should().Contain("approved");
    }

    [Fact]
    public async Task UpdateAddress_WithCancelledPurchaseOrder_ShouldReturnBadRequest()
    {
        // Arrange
        SetupEmployeeAuthentication();
        var seededPurchaseOrder = await SeedPurchaseOrderAsync(OrderType.Internal, OrderStatus.Cancelled);

        var addressId = await ExecuteInDbContextAsync(async dbContext =>
        {
            var po = await dbContext.PurchaseOrders
                .Include(po => po.ShippingAddress)
                .FirstAsync(po => po.Id == seededPurchaseOrder.Id);

            return po.ShippingAddress!.Id;
        });

        var updateRequest = new UpdateAddressRequest
        {
            ContactName = "Updated Contact Name"
        };

        // Act
        var response = await PutAsJsonAsync($"/v1/addresses/{addressId}?purchaseOrderId={seededPurchaseOrder.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var responseContent = await response.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseContent, JsonOptions);

        errorResponse.Should().NotBeNull();
        errorResponse!.Error.Code.Should().Be("INVALID_PURCHASE_ORDER_STATUS");
        errorResponse.Error.Message.Should().Contain("cancelled");
    }

    [Fact]
    public async Task UpdateAddress_WithNoChanges_ShouldReturnOriginalAddress()
    {
        // Arrange
        SetupEmployeeAuthentication();
        var seededPurchaseOrder = await SeedPurchaseOrderAsync();

        var addressInfo = await ExecuteInDbContextAsync(async dbContext =>
        {
            var po = await dbContext.PurchaseOrders
                .Include(po => po.ShippingAddress)
                .FirstAsync(po => po.Id == seededPurchaseOrder.Id);

            return new { Id = po.ShippingAddress!.Id, ContactName = po.ShippingAddress.ContactName };
        });

        var updateRequest = new UpdateAddressRequest
        {
            ContactName = addressInfo.ContactName // Same as existing
        };

        // Act
        var response = await PutAsJsonAsync($"/v1/addresses/{addressInfo.Id}?purchaseOrderId={seededPurchaseOrder.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<AddressDto>(responseContent, JsonOptions);

        result.Should().NotBeNull();
        result!.ContactName.Should().Be(addressInfo.ContactName);
    }

    #endregion

    #region DELETE /v1/addresses/{addressId} Tests

    [Fact]
    public async Task DeleteAddress_WithValidRequest_ShouldRemoveAddressReference()
    {
        // Arrange
        SetupEmployeeAuthentication("emp123");
        var seededPurchaseOrder = await SeedPurchaseOrderAsync(OrderType.Internal, OrderStatus.Pending);

        var addressId = await ExecuteInDbContextAsync(async dbContext =>
        {
            var po = await dbContext.PurchaseOrders
                .Include(po => po.ShippingAddress)
                .FirstAsync(po => po.Id == seededPurchaseOrder.Id);

            return po.ShippingAddress!.Id;
        });

        // Act
        var response = await Client.DeleteAsync($"/v1/addresses/{addressId}?purchaseOrderId={seededPurchaseOrder.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify the address reference was removed from purchase order
        await ExecuteInDbContextAsync(async dbContext =>
        {
            var po = await dbContext.PurchaseOrders
                .Include(po => po.ShippingAddress)
                .FirstAsync(po => po.Id == seededPurchaseOrder.Id);

            po.ShippingAddressId.Should().BeNull();
            po.ShippingAddress.Should().BeNull();

            // Verify the address itself still exists (not physically deleted)
            var address = await dbContext.Addresses.FindAsync(addressId);
            address.Should().NotBeNull();
        });

        // Verify audit log was created
        await ExecuteInDbContextAsync(async dbContext =>
        {
            var auditLogs = await dbContext.AuditLogs
                .Where(a => a.EntityId == seededPurchaseOrder.Id.ToString() && a.Action == AuditAction.Delete)
                .ToListAsync();

            auditLogs.Should().Contain(log => log.ChangeReason != null && log.ChangeReason.Contains("Address removed"));
        });
    }

    [Fact]
    public async Task DeleteAddress_WithApprovedPurchaseOrder_ShouldReturnBadRequest()
    {
        // Arrange
        SetupEmployeeAuthentication();
        var seededPurchaseOrder = await SeedPurchaseOrderAsync(OrderType.Internal, OrderStatus.Approved);

        var addressId = await ExecuteInDbContextAsync(async dbContext =>
        {
            var po = await dbContext.PurchaseOrders
                .Include(po => po.ShippingAddress)
                .FirstAsync(po => po.Id == seededPurchaseOrder.Id);

            return po.ShippingAddress!.Id;
        });

        // Act
        var response = await Client.DeleteAsync($"/v1/addresses/{addressId}?purchaseOrderId={seededPurchaseOrder.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var responseContent = await response.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseContent, JsonOptions);

        errorResponse.Should().NotBeNull();
        errorResponse!.Error.Code.Should().Be("INVALID_PURCHASE_ORDER_STATUS");
    }

    [Fact]
    public async Task DeleteAddress_WithInvalidAddressId_ShouldReturnNotFound()
    {
        // Arrange
        SetupEmployeeAuthentication();
        var seededPurchaseOrder = await SeedPurchaseOrderAsync();

        // Act
        var response = await Client.DeleteAsync($"/v1/addresses/99999?purchaseOrderId={seededPurchaseOrder.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var responseContent = await response.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseContent, JsonOptions);

        errorResponse.Should().NotBeNull();
        errorResponse!.Error.Code.Should().Be("ADDRESS_NOT_FOUND");
    }

    #endregion

    #region GET /v1/addresses/by-type/{addressType} Tests

    [Fact]
    public async Task GetAddressesByType_WithShippingType_ShouldReturnShippingAddresses()
    {
        // Arrange
        SetupEmployeeAuthentication();
        var seededPurchaseOrder = await SeedPurchaseOrderAsync();

        // Act
        var response = await Client.GetAsync($"/v1/addresses/by-type/Shipping?purchaseOrderId={seededPurchaseOrder.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<List<AddressDto>>(responseContent, JsonOptions);

        result.Should().NotBeNull();
        result!.Should().OnlyContain(addr => addr.AddressType == AddressType.Shipping);
    }

    [Fact]
    public async Task GetAddressesByType_WithBillingType_ShouldReturnBillingAddresses()
    {
        // Arrange
        SetupEmployeeAuthentication();
        var seededPurchaseOrder = await SeedPurchaseOrderAsync();

        // Act
        var response = await Client.GetAsync($"/v1/addresses/by-type/Billing?purchaseOrderId={seededPurchaseOrder.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<List<AddressDto>>(responseContent, JsonOptions);

        result.Should().NotBeNull();
        result!.Should().OnlyContain(addr => addr.AddressType == AddressType.Billing);
    }

    [Fact]
    public async Task GetAddressesByType_WithInvalidPurchaseOrderId_ShouldReturnNotFound()
    {
        // Arrange
        SetupEmployeeAuthentication();

        // Act
        var response = await Client.GetAsync("/v1/addresses/by-type/Shipping?purchaseOrderId=99999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var responseContent = await response.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseContent, JsonOptions);

        errorResponse.Should().NotBeNull();
        errorResponse!.Error.Code.Should().Be("PURCHASE_ORDER_NOT_FOUND");
    }

    #endregion

    #region Authorization Tests

    [Theory]
    [InlineData("Employee")]
    [InlineData("Manager")]
    [InlineData("Procurement")]
    [InlineData("Admin")]
    public async Task GetAddresses_WithValidRoles_ShouldSucceed(string role)
    {
        // Arrange
        switch (role)
        {
            case "Employee":
                SetupEmployeeAuthentication();
                break;
            case "Manager":
                SetupManagerAuthentication();
                break;
            case "Procurement":
                SetupProcurementAuthentication();
                break;
            case "Admin":
                SetupAdminAuthentication();
                break;
        }

        // Act
        var response = await Client.GetAsync("/v1/addresses");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData("/v1/addresses")]
    [InlineData("/v1/addresses/1")]
    [InlineData("/v1/addresses/by-type/Shipping")]
    public async Task GetEndpoints_WithoutAuthentication_ShouldReturnUnauthorized(string endpoint)
    {
        // Arrange
        ClearAuthentication();

        // Act
        var response = await Client.GetAsync(endpoint);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task CreateAddress_WithMalformedJson_ShouldReturnBadRequest()
    {
        // Arrange
        SetupEmployeeAuthentication();
        var malformedJson = "{ invalid json }";
        var content = new StringContent(malformedJson, Encoding.UTF8, "application/json");

        // Act
        var response = await Client.PostAsync("/v1/addresses", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateAddress_WithNegativeAddressId_ShouldReturnNotFound()
    {
        // Arrange
        SetupEmployeeAuthentication();
        var seededPurchaseOrder = await SeedPurchaseOrderAsync();

        var updateRequest = new UpdateAddressRequest
        {
            ContactName = "Test"
        };

        // Act
        var response = await PutAsJsonAsync($"/v1/addresses/-1?purchaseOrderId={seededPurchaseOrder.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Data Validation Tests

    [Fact]
    public async Task CreateAddress_WithLongContactName_ShouldHandleGracefully()
    {
        // Arrange
        SetupEmployeeAuthentication();
        var request = CreateBasicAddressRequest();
        request.ContactName = new string('A', 500); // Very long name

        // Act
        var response = await PostAsJsonAsync("/v1/addresses", request);

        // Assert
        // Should either succeed (if database allows) or return validation error
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateAddress_WithSpecialCharacters_ShouldHandleCorrectly()
    {
        // Arrange
        SetupEmployeeAuthentication();
        var request = CreateBasicAddressRequest();
        request.ContactName = "José María Azuñar"; // Special characters
        request.AddressLine1 = "123 Thăng Long Đại Học"; // Vietnamese characters

        // Act
        var response = await PostAsJsonAsync("/v1/addresses", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<AddressDto>(responseContent, JsonOptions);

        result.Should().NotBeNull();
        result!.ContactName.Should().Be("José María Azuñar");
        result.AddressLine1.Should().Be("123 Thăng Long Đại Học");
    }

    #endregion
}
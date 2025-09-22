using Maliev.PurchaseOrderService.Api.DTOs;
using Maliev.PurchaseOrderService.Data.Enums;

namespace Maliev.PurchaseOrderService.Tests.TestInfrastructure;

/// <summary>
/// Factory for creating test data objects with sensible defaults
/// </summary>
public static class TestDataFactory
{
    #region Purchase Order Request Factory

    /// <summary>
    /// Creates a basic CreatePurchaseOrderRequest with default values
    /// </summary>
    public static CreatePurchaseOrderRequest CreatePurchaseOrderRequest(
        int supplierID = 1234,
        int orderID = 5678,
        int currencyID = 1,
        OrderType orderType = OrderType.Internal,
        string? customerPO = null,
        decimal? whtRate = null,
        string? notes = null)
    {
        return new CreatePurchaseOrderRequest
        {
            SupplierID = supplierID,
            OrderID = orderID,
            CurrencyID = currencyID,
            OrderType = orderType,
            CustomerPO = customerPO ?? "CUST-PO-001",
            ExpectedDeliveryDate = DateTime.UtcNow.AddDays(14),
            WhtRate = whtRate ?? 3.0m,
            Notes = notes ?? "Test purchase order",
            ShippingAddress = CreateAddressRequest(AddressType.Shipping),
            BillingAddress = CreateAddressRequest(AddressType.Billing)
        };
    }

    /// <summary>
    /// Creates a minimal CreatePurchaseOrderRequest with only required fields
    /// </summary>
    public static CreatePurchaseOrderRequest CreateMinimalPurchaseOrderRequest(
        int supplierID = 1234,
        int orderID = 5678,
        int currencyID = 1,
        OrderType orderType = OrderType.Internal)
    {
        return new CreatePurchaseOrderRequest
        {
            SupplierID = supplierID,
            OrderID = orderID,
            CurrencyID = currencyID,
            OrderType = orderType
        };
    }

    /// <summary>
    /// Creates a CreatePurchaseOrderRequest with complete data including items
    /// </summary>
    public static CreatePurchaseOrderRequest CreateCompletePurchaseOrderRequest(
        int supplierID = 1234,
        int orderID = 5678,
        int currencyID = 1,
        OrderType orderType = OrderType.Internal)
    {
        var request = CreatePurchaseOrderRequest(supplierID, orderID, currencyID, orderType);
        request.OrderItems = new List<CreateOrderItemRequest>
        {
            CreateOrderItemRequest(),
            CreateOrderItemRequest(quantity: 2, unitPrice: 500.00m)
        };
        return request;
    }

    #endregion

    #region Address Request Factory

    /// <summary>
    /// Creates a CreateAddressRequest with default values
    /// </summary>
    public static CreateAddressRequest CreateAddressRequest(
        AddressType addressType = AddressType.Shipping,
        string? contactName = null,
        string? addressLine1 = null,
        string? city = null,
        string? postalCode = null,
        string? country = null)
    {
        return new CreateAddressRequest
        {
            AddressType = addressType,
            ContactName = contactName ?? "Test Contact",
            AddressLine1 = addressLine1 ?? "123 Test Street",
            AddressLine2 = "Suite 100",
            City = city ?? "Bangkok",
            StateProvince = "Bangkok",
            PostalCode = postalCode ?? "10100",
            Country = country ?? "Thailand",
            PhoneNumber = "+66-2-555-0123",
            EmailAddress = "test@maliev.com"
        };
    }

    /// <summary>
    /// Creates a shipping address with Thai defaults
    /// </summary>
    public static CreateAddressRequest CreateThaiShippingAddress()
    {
        return CreateAddressRequest(
            AddressType.Shipping,
            "Bangkok Manufacturing Hub",
            "456 Industrial Road",
            "Bangkok",
            "10330",
            "Thailand"
        );
    }

    /// <summary>
    /// Creates a billing address with company defaults
    /// </summary>
    public static CreateAddressRequest CreateCompanyBillingAddress()
    {
        return CreateAddressRequest(
            AddressType.Billing,
            "Maliev Co., Ltd. Accounting",
            "789 Business District",
            "Bangkok",
            "10500",
            "Thailand"
        );
    }

    #endregion

    #region Order Item Request Factory

    /// <summary>
    /// Creates a CreateOrderItemRequest with default values
    /// </summary>
    public static CreateOrderItemRequest CreateOrderItemRequest(
        int orderID = 9012,
        int quantity = 1,
        decimal unitPrice = 1000.00m,
        string? notes = null)
    {
        return new CreateOrderItemRequest
        {
            ProductName = "Test Product",
            Quantity = quantity,
            UnitPrice = unitPrice
        };
    }

    #endregion

    #region Update Request Factory

    /// <summary>
    /// Creates an UpdatePurchaseOrderRequest with default values
    /// </summary>
    public static UpdatePurchaseOrderRequest CreateUpdatePurchaseOrderRequest(
        string? rowVersion = null,
        string? customerPO = null,
        DateTime? expectedDeliveryDate = null,
        decimal? whtRate = null,
        string? notes = null)
    {
        return new UpdatePurchaseOrderRequest
        {
            RowVersion = rowVersion ?? Convert.ToBase64String(new byte[] { 1, 2, 3, 4 }),
            CustomerPO = customerPO ?? "UPDATED-PO-001",
            ExpectedDeliveryDate = expectedDeliveryDate ?? DateTime.UtcNow.AddDays(21),
            WhtRate = whtRate ?? 5.0m,
            Notes = notes ?? "Updated notes"
        };
    }

    #endregion

    #region Cancellation Request Factory

    /// <summary>
    /// Creates a CancelPurchaseOrderRequest with default values
    /// </summary>
    public static CancelPurchaseOrderRequest CreateCancelPurchaseOrderRequest(
        string? reason = null,
        string? canceledBy = null,
        bool sendNotifications = true,
        bool archiveDocuments = true)
    {
        return new CancelPurchaseOrderRequest
        {
            Reason = reason ?? "Test cancellation reason",
            CanceledBy = canceledBy ?? "test-user",
            Comments = "Test cancellation comments",
            CanceledAt = DateTime.UtcNow,
            SendNotifications = sendNotifications,
            ArchiveDocuments = archiveDocuments
        };
    }

    #endregion

    #region Approval Request Factory

    /// <summary>
    /// Creates an ApprovePurchaseOrderRequest with default values
    /// </summary>
    public static ApprovePurchaseOrderRequest CreateApprovePurchaseOrderRequest(
        string? approvedBy = null,
        string? notes = null)
    {
        return new ApprovePurchaseOrderRequest
        {
            ApprovedBy = approvedBy ?? "test-manager",
            Comments = notes ?? "Approved for processing",
            ApprovedAt = DateTime.UtcNow
        };
    }

    #endregion

    #region Search Request Factory

    /// <summary>
    /// Creates a SearchPurchaseOrdersRequest with default pagination
    /// </summary>
    public static SearchPurchaseOrdersRequest CreateSearchPurchaseOrdersRequest(
        string? searchTerm = null,
        int? supplierId = null,
        int? orderId = null,
        OrderStatus? status = null,
        OrderType? orderType = null,
        int page = 1,
        int pageSize = 20)
    {
        return new SearchPurchaseOrdersRequest
        {
            SearchTerm = searchTerm,
            SupplierId = supplierId,
            OrderId = orderId,
            Status = status,
            OrderType = orderType,
            Page = page,
            PageSize = pageSize,
            SortBy = Common.Enumerations.PurchaseOrderSortType.CreatedAt,
            SortDirection = "desc"
        };
    }

    #endregion

    #region External Service Response Factory

    /// <summary>
    /// Creates a SupplierDto for testing
    /// </summary>
    public static SupplierDto CreateSupplierDto(
        Guid? id = null,
        string? name = null,
        bool isActive = true)
    {
        return new SupplierDto
        {
            Id = id ?? Guid.NewGuid(),
            Name = name ?? "Test Supplier",
            Code = "SUP-001",
            Email = "supplier@test.com",
            Phone = "+66-2-555-0100",
            IsActive = isActive,
            TaxId = "0123456789012",
            Address = new AddressDto
            {
                AddressLine1 = "123 Supplier Street",
                City = "Bangkok",
                PostalCode = "10100",
                Country = "Thailand"
            }
        };
    }

    /// <summary>
    /// Creates a CurrencyDto for testing
    /// </summary>
    public static CurrencyDto CreateCurrencyDto(
        string code = "THB",
        string? name = null,
        bool isActive = true)
    {
        return new CurrencyDto
        {
            Code = code,
            Name = name ?? "Thai Baht",
            Symbol = code == "THB" ? "฿" : "$",
            IsActive = isActive,
            DecimalPlaces = 2,
            Country = code == "THB" ? "Thailand" : "United States",
            CountryCode = code == "THB" ? "TH" : "US"
        };
    }

    /// <summary>
    /// Creates an OrderItemDto for testing
    /// </summary>
    public static OrderItemDto CreateOrderItemDto(
        int? id = null,
        int quantity = 1,
        decimal unitPrice = 1000.00m)
    {
        return new OrderItemDto
        {
            Id = id ?? 1,
            Quantity = quantity,
            UnitPrice = unitPrice,
            TotalPrice = quantity * unitPrice,
            ProductName = "Test Item",
            ProductCode = "ITEM-001",
            UnitOfMeasure = "pcs",
            Currency = "THB"
        };
    }

    /// <summary>
    /// Creates a DomainEventDto for testing
    /// </summary>
    public static DomainEventDto CreateDomainEventDto(
        string eventType = "PurchaseOrderCreated",
        string? aggregateId = null,
        string? eventData = null)
    {
        return new DomainEventDto
        {
            Id = Random.Shared.NextInt64(),
            EventType = eventType,
            AggregateType = "PurchaseOrder",
            AggregateId = aggregateId ?? Guid.NewGuid().ToString(),
            EventData = eventData ?? "{}",
            OccurredAt = DateTime.UtcNow,
            ProcessedAt = null,
            UserId = "test-user",
            CorrelationId = Guid.NewGuid().ToString()
        };
    }

    #endregion

    #region File Upload Factory

    /// <summary>
    /// Creates file upload test data
    /// </summary>
    public static (string fileName, byte[] content, string contentType) CreateTestFile(
        string fileName = "test-document.pdf",
        string contentType = "application/pdf",
        int sizeInBytes = 1024)
    {
        var content = new byte[sizeInBytes];
        Random.Shared.NextBytes(content);
        return (fileName, content, contentType);
    }

    /// <summary>
    /// Creates a DocumentUploadResult for testing
    /// </summary>
    public static DocumentUploadResult CreateDocumentUploadResult(
        string? fileName = null,
        string? filePath = null,
        long fileSize = 1024)
    {
        return new DocumentUploadResult
        {
            Success = true,
            FilePath = filePath ?? "/uploads/test-document.pdf",
            FileSize = fileSize,
            UploadedAt = DateTime.UtcNow,
            FileId = 1,
            UploadedBy = "test-user",
            FileHash = "abcdef123456"
        };
    }

    #endregion

    #region WHT Calculation Factory

    /// <summary>
    /// Creates a WHTCalculationRequest for testing
    /// </summary>
    public static WHTCalculationRequest CreateWHTCalculationRequest(
        decimal amount = 1000.00m,
        decimal whtRate = 0.03m,
        string currencyCode = "THB")
    {
        return new WHTCalculationRequest
        {
            SubtotalAmount = amount,
            WHTRate = whtRate,
            CurrencyCode = currencyCode,
            SupplierID = 1234,
            OrderType = Data.Enums.OrderType.Internal,
            Notes = "Test WHT calculation"
        };
    }

    /// <summary>
    /// Creates a WHTCalculationResult for testing
    /// </summary>
    public static WHTCalculationResult CreateWHTCalculationResult(
        decimal amount = 1000.00m,
        decimal whtRate = 0.03m)
    {
        var whtAmount = amount * whtRate;
        return new WHTCalculationResult
        {
            WHTAmount = whtAmount,
            NetAmount = amount - whtAmount,
            WHTRate = whtRate,
            IsApplicable = true,
            SubtotalAmount = amount,
            TaxBase = amount,
            CurrencyCode = "THB",
            WHTAmountTHB = whtAmount,
            Reason = "Standard WHT calculation"
        };
    }

    #endregion
}
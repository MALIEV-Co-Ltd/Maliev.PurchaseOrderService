using Maliev.PurchaseOrderService.Api.DTOs;
using Maliev.PurchaseOrderService.Data.Enums;
using Maliev.PurchaseOrderService.Data.Entities;

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
            CustomerPO = customerPO ?? (orderType == OrderType.External ? "CUST-PO-001" : null),
            ExpectedDeliveryDate = DateTime.UtcNow.AddDays(14),
            WhtRate = whtRate ?? (orderType == OrderType.Internal ? 0.03m : null), // 3% as decimal for internal orders only
            Notes = notes ?? "Test purchase order",
            ShippingAddress = CreateAddressRequest(AddressType.Shipping),
            BillingAddress = CreateAddressRequest(AddressType.Billing),
            OrderItems = new List<CreateOrderItemRequest>() // Initialize empty list
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
            OrderType = orderType,
            OrderItems = new List<CreateOrderItemRequest>() // Initialize empty list
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
            WhtRate = whtRate ?? 0.05m, // 5% as decimal (0.05)
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

    #region Test Scenario Factory Methods

    /// <summary>
    /// Creates test data for a complete business scenario - Employee creates internal PO
    /// </summary>
    public static (CreatePurchaseOrderRequest request, SupplierDto supplier, CurrencyDto currency, List<OrderItemDto> orderItems)
        CreateEmployeeInternalPOScenario(string employeeId = "emp123")
    {
        var supplier = CreateSupplierDto(name: "Internal Test Supplier");
        var currency = CreateCurrencyDto("THB", "Thai Baht");
        var orderItems = new List<OrderItemDto>
        {
            CreateOrderItemDto(id: 1, purchaseOrderId: 1, externalOrderItemId: 1, quantity: 2, unitPrice: 150.00m),
            CreateOrderItemDto(id: 2, purchaseOrderId: 1, externalOrderItemId: 2, quantity: 1, unitPrice: 300.00m)
        };

        var request = CreateCompletePurchaseOrderRequest(
            supplierID: (int)(supplier.Id.GetHashCode() & 0x7FFFFFFF), // Convert Guid to positive int
            orderID: orderItems.First().Id * 1000, // Derive from order items
            currencyID: currency.Code.GetHashCode() & 0x7FFFFFFF,
            orderType: OrderType.Internal
        );

        return (request, supplier, currency, orderItems);
    }

    /// <summary>
    /// Creates test data for external purchase order scenario
    /// </summary>
    public static (CreatePurchaseOrderRequest request, SupplierDto supplier, CurrencyDto currency, List<OrderItemDto> orderItems)
        CreateExternalPOScenario(string customerPO = "CUST-2024-001")
    {
        var supplier = CreateSupplierDto(name: "External Test Supplier");
        var currency = CreateCurrencyDto("USD", "US Dollar");
        var orderItems = new List<OrderItemDto>
        {
            CreateOrderItemDto(id: 1, purchaseOrderId: 1, externalOrderItemId: 1, quantity: 1, unitPrice: 1500.00m)
        };

        var request = CreateCompletePurchaseOrderRequest(
            supplierID: (int)(supplier.Id.GetHashCode() & 0x7FFFFFFF),
            orderID: orderItems.First().Id * 1000,
            currencyID: currency.Code.GetHashCode() & 0x7FFFFFFF,
            orderType: OrderType.External
        );
        request.CustomerPO = customerPO;

        return (request, supplier, currency, orderItems);
    }

    /// <summary>
    /// Creates a complete purchase order scenario with all dependencies for database seeding
    /// </summary>
    public static (PurchaseOrder purchaseOrder, List<OrderItem> orderItems, Address shippingAddress, Address billingAddress, SupplierDto supplier, CurrencyDto currency)
        CreateCompletePOScenarioForSeeding(
            OrderType orderType = OrderType.Internal,
            OrderStatus status = OrderStatus.Pending,
            string createdBy = "test-user",
            int itemCount = 2)
    {
        var supplier = CreateSupplierDto();
        var currency = CreateCurrencyDto();

        var supplierId = (int)(supplier.Id.GetHashCode() & 0x7FFFFFFF);
        var orderId = Random.Shared.Next(10000, 99999);
        var currencyId = currency.Code.GetHashCode() & 0x7FFFFFFF;

        var shippingAddress = CreateAddressEntity(addressType: AddressType.Shipping, createdBy: createdBy);
        var billingAddress = CreateAddressEntity(addressType: AddressType.Billing, createdBy: createdBy);

        var purchaseOrder = CreatePurchaseOrderEntity(
            supplierID: supplierId,
            orderID: orderId,
            currencyID: currencyId,
            orderType: orderType,
            status: status,
            createdBy: createdBy
        );

        var orderItems = new List<OrderItem>();
        for (int i = 0; i < itemCount; i++)
        {
            var orderItem = CreateOrderItemEntity(
                externalOrderItemId: i + 1,
                productName: $"Test Product {i + 1}",
                quantity: i + 1,
                unitPrice: (i + 1) * 100.00m
            );
            orderItems.Add(orderItem);
        }

        // Calculate totals properly
        var subtotal = orderItems.Sum(oi => oi.TotalPrice);
        purchaseOrder.SubtotalAmount = subtotal;

        if (orderType == OrderType.Internal && purchaseOrder.WHTRate.HasValue)
        {
            purchaseOrder.WHTAmount = Math.Round(subtotal * (purchaseOrder.WHTRate.Value / 100), 2);
            purchaseOrder.TotalAmount = subtotal - purchaseOrder.WHTAmount.Value;
        }
        else
        {
            purchaseOrder.TotalAmount = subtotal;
            purchaseOrder.WHTRate = null;
            purchaseOrder.WHTAmount = null;
        }

        return (purchaseOrder, orderItems, shippingAddress, billingAddress, supplier, currency);
    }

    /// <summary>
    /// Creates test scenarios for different purchase order statuses
    /// </summary>
    public static (PurchaseOrder pendingPO, PurchaseOrder approvedPO, PurchaseOrder cancelledPO)
        CreateMultiStatusPOScenarios(string createdBy = "test-user")
    {
        var (pendingPO, _, _, _, _, _) = CreateCompletePOScenarioForSeeding(
            OrderType.Internal, OrderStatus.Pending, createdBy, 2);

        var (approvedPO, _, _, _, _, _) = CreateCompletePOScenarioForSeeding(
            OrderType.External, OrderStatus.Approved, $"manager-{createdBy}", 1);
        approvedPO.ApprovedBy = "mgr123";
        approvedPO.ApprovedAt = DateTime.UtcNow.AddHours(-2);

        var (cancelledPO, _, _, _, _, _) = CreateCompletePOScenarioForSeeding(
            OrderType.Internal, OrderStatus.Cancelled, createdBy, 3);
        cancelledPO.CancelledBy = "mgr123";
        cancelledPO.CancelledAt = DateTime.UtcNow.AddMinutes(-30);
        // CancellationReason property doesn't exist - using Notes instead
        cancelledPO.Notes = "Test cancellation reason";

        return (pendingPO, approvedPO, cancelledPO);
    }

    #endregion

    #region External Service Response Factory

    /// <summary>
    /// Creates a SupplierDto for testing with deterministic IDs when needed
    /// </summary>
    public static SupplierDto CreateSupplierDto(
        Guid? id = null,
        string? name = null,
        bool isActive = true,
        string? code = null)
    {
        var supplierId = id ?? Guid.NewGuid();
        var supplierName = name ?? "Test Supplier";
        var supplierCode = code ?? $"SUP-{supplierId.ToString()[..8].ToUpper()}";

        return new SupplierDto
        {
            Id = supplierId,
            Name = supplierName,
            Code = supplierCode,
            Email = $"supplier.{supplierCode.ToLower()}@test.com",
            Phone = "+66-2-555-0100",
            IsActive = isActive,
            TaxId = $"{Random.Shared.NextInt64(1000000000000, 9999999999999)}",
            Address = new AddressDto
            {
                AddressLine1 = $"{Random.Shared.Next(1, 999)} Supplier Street",
                City = "Bangkok",
                PostalCode = "10100",
                Country = "Thailand"
            }
        };
    }

    /// <summary>
    /// Creates a SupplierDto with specific characteristics for testing scenarios
    /// </summary>
    public static SupplierDto CreateSpecificSupplierDto(
        string name,
        string code,
        string country = "Thailand",
        bool isActive = true)
    {
        return new SupplierDto
        {
            Id = Guid.NewGuid(),
            Name = name,
            Code = code,
            Email = $"{code.ToLower()}@test.com",
            Phone = "+66-2-555-0100",
            IsActive = isActive,
            TaxId = $"{Random.Shared.NextInt64(1000000000000, 9999999999999)}",
            Address = new AddressDto
            {
                AddressLine1 = $"{Random.Shared.Next(1, 999)} {name} Street",
                City = country == "Thailand" ? "Bangkok" : "City",
                PostalCode = country == "Thailand" ? "10100" : "12345",
                Country = country
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
    /// Creates an OrderItemDto for testing with realistic data
    /// </summary>
    public static OrderItemDto CreateOrderItemDto(
        int? id = null,
        int purchaseOrderId = 1,
        int? externalOrderItemId = null,
        int quantity = 1,
        decimal unitPrice = 1000.00m,
        string? productName = null,
        string? currency = null)
    {
        var itemId = id ?? Random.Shared.Next(1, 9999);
        var externalId = externalOrderItemId ?? itemId;
        var itemName = productName ?? $"Test Product {itemId}";
        var itemCode = $"PROD-{itemId:D4}";
        var itemCurrency = currency ?? "THB";

        return new OrderItemDto
        {
            Id = itemId,
            PurchaseOrderId = purchaseOrderId,
            ExternalOrderItemId = externalId,
            ProductCode = itemCode,
            ProductName = itemName,
            Quantity = quantity,
            UnitOfMeasure = "pcs",
            UnitPrice = unitPrice,
            TotalPrice = quantity * unitPrice,
            Currency = itemCurrency,
            DeliveryDate = DateTime.UtcNow.AddDays(14),
            Notes = $"Test item notes for {itemName}",
            CachedAt = DateTime.UtcNow,
            ExternallyModified = false
        };
    }

    /// <summary>
    /// Creates a list of OrderItemDto for testing scenarios
    /// </summary>
    public static List<OrderItemDto> CreateOrderItemDtoList(
        int count = 2,
        int purchaseOrderId = 1,
        string currency = "THB")
    {
        var items = new List<OrderItemDto>();
        for (int i = 0; i < count; i++)
        {
            items.Add(CreateOrderItemDto(
                id: i + 1,
                purchaseOrderId: purchaseOrderId,
                externalOrderItemId: i + 1,
                quantity: i + 1,
                unitPrice: (i + 1) * 100.00m,
                currency: currency
            ));
        }
        return items;
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
        decimal whtRate = 3.0m, // 3% stored as 3.0
        string currencyCode = "THB")
    {
        return new WHTCalculationRequest
        {
            PurchaseOrderId = 1001,
            SubtotalAmount = amount,
            WHTRate = whtRate,
            CurrencyCode = currencyCode,
            SupplierID = 1234,
            OrderType = Data.Enums.OrderType.Internal
        };
    }

    /// <summary>
    /// Creates a WHTCalculationResult for testing
    /// </summary>
    public static WHTCalculationResult CreateWHTCalculationResult(
        decimal amount = 1000.00m,
        decimal whtRate = 3.0m) // 3% stored as 3.0
    {
        var whtAmount = Math.Round(amount * (whtRate / 100), 2); // Convert percentage to decimal
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
            Reason = "Standard WHT calculation for Thailand supplier"
        };
    }

    #endregion

    #region Database Entity Factories

    /// <summary>
    /// Creates a test Address entity for database seeding with realistic variations
    /// </summary>
    public static Address CreateAddressEntity(
        int? id = null,
        AddressType addressType = AddressType.Shipping,
        string? contactName = null,
        string createdBy = "test-user",
        string? country = null)
    {
        var actualCountry = country ?? "Thailand";
        var addressId = Random.Shared.Next(1, 999);
        var isThaiAddress = actualCountry == "Thailand";

        var contactNameSuffix = addressType == AddressType.Shipping ? "Shipping" : "Billing";
        var actualContactName = contactName ?? $"Test {contactNameSuffix} Contact {addressId}";

        return new Address
        {
            Id = id ?? 0, // Let database assign ID
            AddressType = addressType,
            ContactName = actualContactName,
            AddressLine1 = $"{addressId} Test {contactNameSuffix} Street",
            AddressLine2 = addressType == AddressType.Shipping ? $"Loading Dock {addressId % 10}" : $"Suite {addressId}",
            City = isThaiAddress ? "Bangkok" : "Test City",
            StateProvince = isThaiAddress ? "Bangkok" : "Test State",
            PostalCode = isThaiAddress ? $"{10100 + (addressId % 900)}" : $"{12000 + (addressId % 9000)}",
            Country = actualCountry,
            PhoneNumber = isThaiAddress ? $"+66-2-555-{addressId:D4}" : $"+1-555-{addressId:D4}",
            EmailAddress = $"test.{contactNameSuffix.ToLower()}.{addressId}@maliev.com",
            IsActive = true,
            IsValidated = true,
            ValidatedAt = DateTime.UtcNow.AddDays(-Random.Shared.Next(1, 30)),
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow.AddDays(-Random.Shared.Next(1, 90)),
            UpdatedBy = createdBy,
            UpdatedAt = DateTime.UtcNow.AddHours(-Random.Shared.Next(1, 24)),
            IsDeleted = false
        };
    }

    /// <summary>
    /// Creates a test PurchaseOrder entity for database seeding with realistic data
    /// </summary>
    public static PurchaseOrder CreatePurchaseOrderEntity(
        int? id = null,
        int? supplierID = null,
        int? orderID = null,
        int? currencyID = null,
        OrderType orderType = OrderType.Internal,
        OrderStatus status = OrderStatus.Pending,
        string createdBy = "test-user")
    {
        // Generate realistic IDs if not provided
        var actualSupplierID = supplierID ?? Random.Shared.Next(10000, 99999);
        var actualOrderID = orderID ?? Random.Shared.Next(10000, 99999);
        var actualCurrencyID = currencyID ?? (orderType == OrderType.Internal ? 1 : 2);

        var orderNumber = $"PO-{DateTime.UtcNow:yyyy}-{Random.Shared.Next(100000, 999999)}";
        var currencyInfo = orderType == OrderType.Internal
            ? (code: "THB", symbol: "฿", name: "Thai Baht")
            : (code: "USD", symbol: "$", name: "US Dollar");

        var po = new PurchaseOrder
        {
            Id = id ?? 0, // Let database assign ID
            OrderNumber = orderNumber,
            CustomerPO = orderType == OrderType.External ? $"CUST-{orderNumber.Replace("PO-", "")}" : null,
            SupplierID = actualSupplierID,
            OrderID = actualOrderID,
            CurrencyID = actualCurrencyID,
            SupplierName = $"Test Supplier {actualSupplierID}",
            SupplierContactInfo = $"supplier{actualSupplierID}@test.com",
            CurrencyCode = currencyInfo.code,
            CurrencySymbol = currencyInfo.symbol,
            Currency = currencyInfo.code,
            OrderDate = DateTime.UtcNow.AddDays(-Random.Shared.Next(0, 30)), // Random recent date
            ExpectedDeliveryDate = DateTime.UtcNow.AddDays(Random.Shared.Next(7, 30)),
            Status = status,
            OrderType = orderType,
            SubtotalAmount = 1000.00m, // Will be recalculated by calling code
            TotalAmount = 1000.00m, // Will be recalculated by calling code
            WHTRate = orderType == OrderType.Internal ? 3.0m : null, // 3% stored as 3.0
            WHTAmount = null, // Will be calculated by calling code
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow.AddMinutes(-Random.Shared.Next(1, 1440)), // Random time in last day
            UpdatedBy = createdBy,
            UpdatedAt = DateTime.UtcNow.AddMinutes(-Random.Shared.Next(1, 60)), // Random recent modification
            Notes = $"Test purchase order created by {createdBy}",
            IsPdfGenerationEnabled = orderType == OrderType.Internal,
            IsPdfGenerated = false,
            IsDeleted = false,
            RowVersion = new byte[8] // Will be populated by EF Core
        };

        // Set status-specific fields
        switch (status)
        {
            case OrderStatus.Approved:
                po.ApprovedBy = $"mgr-{createdBy}";
                po.ApprovedAt = DateTime.UtcNow.AddHours(-Random.Shared.Next(1, 24));
                break;
            case OrderStatus.Cancelled:
                po.CancelledBy = $"mgr-{createdBy}";
                po.CancelledAt = DateTime.UtcNow.AddMinutes(-Random.Shared.Next(30, 1440));
                // CancellationReason property doesn't exist - using Notes instead
                po.Notes = "Test cancellation reason";
                break;
        }

        // Initialize row version
        Random.Shared.NextBytes(po.RowVersion);

        return po;
    }

    /// <summary>
    /// Creates a test OrderItem entity for database seeding with realistic data
    /// </summary>
    public static OrderItem CreateOrderItemEntity(
        int? id = null,
        int purchaseOrderId = 0, // Will be set by calling code
        int? externalOrderItemId = null,
        string? productName = null,
        decimal quantity = 1,
        decimal unitPrice = 1000.00m,
        string currency = "THB")
    {
        var actualExternalId = externalOrderItemId ?? Random.Shared.Next(1, 9999);
        var actualProductName = productName ?? $"Test Product {actualExternalId}";
        var productCode = $"PROD-{actualExternalId:D4}";

        return new OrderItem
        {
            Id = id ?? 0, // Let database assign ID
            PurchaseOrderId = purchaseOrderId, // Will be set by calling code after PO is saved
            ExternalOrderItemId = actualExternalId,
            ProductCode = productCode,
            ProductName = actualProductName,
            Quantity = quantity,
            UnitOfMeasure = "pcs",
            UnitPrice = unitPrice,
            TotalPrice = quantity * unitPrice,
            Currency = currency,
            DeliveryDate = DateTime.UtcNow.AddDays(Random.Shared.Next(7, 30)),
            Notes = $"Test item for {actualProductName}",
            CachedAt = DateTime.UtcNow.AddMinutes(-Random.Shared.Next(1, 60)),
            ExternallyModified = false,
            SourceService = "OrderService",
            ExternalVersion = "1.0",
            ExternalStatus = "Active",
            IsSyncSuccessful = true,
            CreatedAt = DateTime.UtcNow.AddMinutes(-Random.Shared.Next(1, 1440))
            // Note: Description, Category, Specifications, Manufacturer, Model, IsActive properties don't exist on OrderItem entity
        };
    }

    /// <summary>
    /// Creates a complete test purchase order with all related entities (Enhanced version)
    /// </summary>
    public static (PurchaseOrder purchaseOrder, List<OrderItem> orderItems, Address? shippingAddress, Address? billingAddress)
        CreateCompletePurchaseOrderWithEntities(
            OrderType orderType = OrderType.Internal,
            int itemCount = 2,
            string createdBy = "test-user",
            OrderStatus status = OrderStatus.Pending)
    {
        // Generate realistic IDs that don't conflict
        var baseId = Random.Shared.Next(10000, 99999);
        var supplierID = baseId + 1000;
        var orderID = baseId + 2000;
        var currencyID = orderType == OrderType.Internal ? 1 : 2; // THB=1, USD=2

        // Create addresses with proper relationships
        var shippingAddress = CreateAddressEntity(addressType: AddressType.Shipping, createdBy: createdBy);
        var billingAddress = CreateAddressEntity(addressType: AddressType.Billing, createdBy: createdBy);

        // Create purchase order with realistic data
        var purchaseOrder = CreatePurchaseOrderEntity(
            supplierID: supplierID,
            orderID: orderID,
            currencyID: currencyID,
            orderType: orderType,
            status: status,
            createdBy: createdBy);

        // Create order items with proper relationships
        var orderItems = new List<OrderItem>();
        for (int i = 0; i < itemCount; i++)
        {
            var orderItem = CreateOrderItemEntity(
                externalOrderItemId: i + 1,
                productName: $"Test Product {i + 1}",
                quantity: i + 1,
                unitPrice: (i + 1) * 100.00m);
            orderItems.Add(orderItem);
        }

        // Calculate totals accurately
        var subtotal = orderItems.Sum(oi => oi.TotalPrice);
        purchaseOrder.SubtotalAmount = subtotal;

        if (orderType == OrderType.Internal && purchaseOrder.WHTRate.HasValue)
        {
            // WHT calculation: Rate is stored as percentage (3.0 for 3%), so divide by 100
            purchaseOrder.WHTAmount = Math.Round(subtotal * (purchaseOrder.WHTRate.Value / 100), 2);
            purchaseOrder.TotalAmount = subtotal - purchaseOrder.WHTAmount.Value;
        }
        else
        {
            purchaseOrder.TotalAmount = subtotal;
            purchaseOrder.WHTRate = null;
            purchaseOrder.WHTAmount = null;
        }

        // Set proper currency based on order type
        purchaseOrder.CurrencyCode = orderType == OrderType.Internal ? "THB" : "USD";
        purchaseOrder.CurrencySymbol = orderType == OrderType.Internal ? "฿" : "$";
        purchaseOrder.Currency = purchaseOrder.CurrencyCode;

        return (purchaseOrder, orderItems, shippingAddress, billingAddress);
    }

    /// <summary>
    /// Creates a test PurchaseOrderFile entity for database seeding
    /// </summary>
    public static PurchaseOrderFile CreatePurchaseOrderFileEntity(
        int? id = null,
        int purchaseOrderId = 1,
        string fileName = "test-document.pdf",
        string uploadedBy = "test-user")
    {
        return new PurchaseOrderFile
        {
            Id = id ?? 0, // Let database assign ID
            PurchaseOrderId = purchaseOrderId,
            FileName = fileName,
            ObjectName = $"purchase-orders/{purchaseOrderId}/{fileName}",
            DocumentType = Data.Enums.DocumentType.Other,
            FileSize = 1024,
            ContentType = "application/pdf",
            FileHash = "abcdef123456",
            UploadedBy = uploadedBy,
            UploadedAt = DateTime.UtcNow,
            IsAvailable = true,
            DownloadCount = 0,
            IsSystemGenerated = false,
            VirusScanStatus = "Clean",
            VirusScanCompletedAt = DateTime.UtcNow,
            IsDeleted = false,
            RowVersion = new byte[] { 1, 2, 3, 4 }
        };
    }

    /// <summary>
    /// Creates a test DomainEvent entity for database seeding
    /// </summary>
    public static DomainEvent CreateDomainEventEntity(
        string eventType = "PurchaseOrderCreated",
        string aggregateId = "1",
        string userId = "test-user")
    {
        return new DomainEvent
        {
            EventType = eventType,
            AggregateType = "PurchaseOrder",
            AggregateId = aggregateId,
            EventData = "{}",
            UserId = userId,
            OccurredAt = DateTime.UtcNow,
            CorrelationId = Guid.NewGuid().ToString(),
            ProcessedAt = null
        };
    }

    /// <summary>
    /// Creates a test AuditLog entity for database seeding
    /// </summary>
    public static AuditLog CreateAuditLogEntity(
        string entityType = "PurchaseOrder",
        string entityId = "1",
        Data.Enums.AuditAction action = Data.Enums.AuditAction.Create,
        string userId = "test-user")
    {
        return new AuditLog
        {
            EntityType = entityType,
            EntityId = entityId,
            Action = action,
            UserId = userId,
            UserRole = "Employee",
            OldValues = null,
            NewValues = "{}",
            Timestamp = DateTime.UtcNow,
            IPAddress = "127.0.0.1",
            UserAgent = "Test",
            IsSuccessful = true
        };
    }

    #endregion

    #region Deterministic ID Helpers

    /// <summary>
    /// Generates a deterministic positive integer from a string (for consistent test IDs)
    /// </summary>
    public static int GenerateDeterministicId(string input)
    {
        return Math.Abs(input.GetHashCode()) % 999999 + 100000; // 6-digit positive number
    }

    /// <summary>
    /// Generates a unique sequence of IDs for tests to avoid conflicts
    /// </summary>
    private static int _idSequence = 100000;
    public static int GetNextUniqueId()
    {
        return Interlocked.Increment(ref _idSequence);
    }

    /// <summary>
    /// Resets the ID sequence (useful for test isolation)
    /// </summary>
    public static void ResetIdSequence(int startValue = 100000)
    {
        _idSequence = startValue;
    }

    #endregion
}
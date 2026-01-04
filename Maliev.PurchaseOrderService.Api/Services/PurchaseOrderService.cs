using Microsoft.EntityFrameworkCore;
using Maliev.PurchaseOrderService.Api.DTOs;
using Maliev.PurchaseOrderService.Api.Mapping;
using Maliev.PurchaseOrderService.Api.ExternalServices;
using Maliev.PurchaseOrderService.Data;
using Maliev.PurchaseOrderService.Data.Entities;
using Maliev.PurchaseOrderService.Common.Enumerations;
using MassTransit;
using Maliev.MessagingContracts.Generated;

namespace Maliev.PurchaseOrderService.Api.Services;

/// <summary>
/// Core implementation of Purchase Order Service
/// </summary>
public class PurchaseOrderService : IPurchaseOrderService
{
    private readonly PurchaseOrderContext _context;
    private readonly ISupplierServiceClient _supplierClient;
    private readonly IOrderServiceClient _orderClient;
    private readonly ICurrencyServiceClient _currencyClient;
    private readonly IWHTCalculationService _whtService;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<PurchaseOrderService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PurchaseOrderService"/> class.
    /// </summary>
    /// <param name="context">The purchase order database context.</param>
    /// <param name="supplierClient">The supplier service client.</param>
    /// <param name="orderClient">The order service client.</param>
    /// <param name="currencyClient">The currency service client.</param>
    /// <param name="whtService">The WHT calculation service.</param>
    /// <param name="publishEndpoint">The mass transit publish endpoint.</param>
    /// <param name="logger">The logger instance.</param>
    public PurchaseOrderService(
        PurchaseOrderContext context,
        ISupplierServiceClient supplierClient,
        IOrderServiceClient orderClient,
        ICurrencyServiceClient currencyClient,
        IWHTCalculationService whtService,
        IPublishEndpoint publishEndpoint,
        ILogger<PurchaseOrderService> logger)
    {
        _context = context;
        _supplierClient = supplierClient;
        _orderClient = orderClient;
        _currencyClient = currencyClient;
        _whtService = whtService;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<PurchaseOrderDetailResponse> CreatePurchaseOrderAsync(
        CreatePurchaseOrderRequest request,
        string userId,
        string userRole,
        CancellationToken cancellationToken = default)
    {
        // Validate external references
        var supplier = await _supplierClient.GetSupplierAsync(request.SupplierID, cancellationToken);
        if (supplier == null)
        {
            throw new InvalidOperationException($"Supplier {request.SupplierID} not found");
        }

        var order = await _orderClient.GetOrderAsync(request.OrderID, cancellationToken);
        if (order == null)
        {
            throw new InvalidOperationException($"Order {request.OrderID} not found");
        }

        var currency = await _currencyClient.GetCurrencyAsync(request.CurrencyID, cancellationToken);
        if (currency == null)
        {
            throw new InvalidOperationException($"Currency {request.CurrencyID} not found");
        }

        // Fetch order items
        var orderItems = await _orderClient.GetOrderItemsAsync(request.OrderID, cancellationToken);

        // Filter items if partial ordering
        if (request.Items != null && request.Items.Any())
        {
            var requestedItemIds = request.Items.Select(i => i.ExternalOrderItemId).ToHashSet();
            orderItems = orderItems.Where(i => requestedItemIds.Contains(i.Id)).ToList();
        }

        // Create purchase order entity
        var purchaseOrder = request.ToPurchaseOrder();
        purchaseOrder.OrderNumber = GenerateOrderNumber();
        purchaseOrder.CreatedBy = userId;
        purchaseOrder.SupplierName = supplier.Name;
        purchaseOrder.SupplierContactInfo = supplier.ContactInfo;
        purchaseOrder.CurrencyCode = currency.Code;
        purchaseOrder.CurrencySymbol = currency.Symbol;

        // Map order items
        purchaseOrder.Items = orderItems.Select(item => new OrderItem
        {
            ExternalOrderItemId = item.Id,
            ProductCode = item.ProductCode,
            ProductName = item.ProductName,
            Quantity = request.Items?.FirstOrDefault(i => i.ExternalOrderItemId == item.Id)?.Quantity ?? item.Quantity,
            UnitOfMeasure = item.UnitOfMeasure,
            UnitPrice = item.UnitPrice,
            TotalPrice = item.TotalPrice,
            Currency = item.Currency,
            Notes = item.Notes,
            CachedAt = DateTime.UtcNow,
            ExternallyModified = false
        }).ToList();

        // Calculate totals
        purchaseOrder.SubtotalAmount = purchaseOrder.Items.Sum(i => i.TotalPrice);
        purchaseOrder.WHTAmount = _whtService.CalculateWHT(purchaseOrder.SubtotalAmount, request.WHTRate);
        purchaseOrder.TotalAmount = purchaseOrder.SubtotalAmount - (purchaseOrder.WHTAmount ?? 0);

        // Handle addresses
        if (request.ShippingAddress != null)
        {
            var shippingAddress = request.ShippingAddress.ToAddress();
            _context.Addresses.Add(shippingAddress);
            await _context.SaveChangesAsync(cancellationToken);
            purchaseOrder.ShippingAddressId = shippingAddress.Id;
        }

        if (request.BillingAddress != null)
        {
            var billingAddress = request.BillingAddress.ToAddress();
            _context.Addresses.Add(billingAddress);
            await _context.SaveChangesAsync(cancellationToken);
            purchaseOrder.BillingAddressId = billingAddress.Id;
        }

        _context.PurchaseOrders.Add(purchaseOrder);
        await _context.SaveChangesAsync(cancellationToken);

        // Publish PurchaseOrderCreatedEvent
        await _publishEndpoint.Publish(new PurchaseOrderCreatedEvent(
            MessageId: Guid.NewGuid(),
            MessageName: "PurchaseOrderCreatedEvent",
            MessageType: MessageType.Event,
            MessageVersion: "1.0.0",
            PublishedBy: "PurchaseOrderService",
            ConsumedBy: ["MaterialService", "NotificationService"],
            CorrelationId: Guid.NewGuid(),
            CausationId: null,
            OccurredAtUtc: DateTimeOffset.UtcNow,
            IsPublic: false,
            Payload: new PurchaseOrderCreatedEventPayload(
                PurchaseOrderId: purchaseOrder.Id,
                PurchaseOrderNumber: purchaseOrder.OrderNumber,
                SupplierId: purchaseOrder.SupplierID,
                TotalAmount: (double)purchaseOrder.TotalAmount,
                Currency: purchaseOrder.CurrencyCode,
                RequestedDeliveryDate: purchaseOrder.ExpectedDeliveryDate.HasValue ?
                    new DateTimeOffset(purchaseOrder.ExpectedDeliveryDate.Value, TimeSpan.Zero) : null,
                CreatedBy: purchaseOrder.CreatedBy,
                CreatedAt: new DateTimeOffset(purchaseOrder.CreatedAt, TimeSpan.Zero)
            )
        ), cancellationToken);

        _logger.LogInformation("Published PurchaseOrderCreatedEvent for PO {OrderNumber}", purchaseOrder.OrderNumber);

        _logger.LogInformation("Created purchase order {OrderNumber} for user {UserId}",
            purchaseOrder.OrderNumber, userId);

        return await GetPurchaseOrderByIdAsync(purchaseOrder.Id, userId, userRole, cancellationToken)
            ?? throw new InvalidOperationException("Failed to retrieve created purchase order");
    }

    /// <inheritdoc/>
    public async Task<PurchaseOrderDetailResponse?> GetPurchaseOrderByIdAsync(
        int id,
        string userId,
        string userRole,
        CancellationToken cancellationToken = default)
    {
        var query = _context.PurchaseOrders
            .Include(po => po.Items)
            .Include(po => po.ShippingAddress)
            .Include(po => po.BillingAddress)
            .Include(po => po.Files)
            .AsQueryable();

        // Apply role-based filtering (Ownership check)
        if (userRole == "employee")
        {
            query = query.Where(po => po.CreatedBy == userId);
        }

        var purchaseOrder = await query.FirstOrDefaultAsync(po => po.Id == id, cancellationToken);

        if (purchaseOrder == null)
        {
            return null;
        }

        var response = purchaseOrder.ToPurchaseOrderDetailResponse();

        // Check if stale (simplified - would need OrderService call)
        response.IsStale = false;

        return response;
    }

    /// <inheritdoc/>
    public async Task<PaginatedResponse<PurchaseOrderResponse>> SearchPurchaseOrdersAsync(
        SearchPurchaseOrdersRequest request,
        string userId,
        string userRole,
        CancellationToken cancellationToken = default)
    {
        var query = _context.PurchaseOrders.AsQueryable();

        // Apply role-based filtering (Ownership check)
        if (userRole == "employee")
        {
            query = query.Where(po => po.CreatedBy == userId);
        }

        // Apply filters
        if (request.Status.HasValue)
        {
            query = query.Where(po => po.Status == request.Status.Value);
        }

        if (request.OrderType.HasValue)
        {
            query = query.Where(po => po.OrderType == request.OrderType.Value);
        }

        if (request.SupplierID.HasValue)
        {
            query = query.Where(po => po.SupplierID == request.SupplierID.Value);
        }

        if (request.OrderID.HasValue)
        {
            query = query.Where(po => po.OrderID == request.OrderID.Value);
        }

        if (request.CreatedFrom.HasValue)
        {
            query = query.Where(po => po.CreatedAt >= request.CreatedFrom.Value);
        }

        if (request.CreatedTo.HasValue)
        {
            query = query.Where(po => po.CreatedAt <= request.CreatedTo.Value);
        }

        // Apply sorting
        query = request.SortBy.ToLower() switch
        {
            "ordernumber" => request.SortDirection.ToLower() == "asc"
                ? query.OrderBy(po => po.OrderNumber)
                : query.OrderByDescending(po => po.OrderNumber),
            "supplierid" => request.SortDirection.ToLower() == "asc"
                ? query.OrderBy(po => po.SupplierID)
                : query.OrderByDescending(po => po.SupplierID),
            "totalamount" => request.SortDirection.ToLower() == "asc"
                ? query.OrderBy(po => po.TotalAmount)
                : query.OrderByDescending(po => po.TotalAmount),
            "status" => request.SortDirection.ToLower() == "asc"
                ? query.OrderBy(po => po.Status)
                : query.OrderByDescending(po => po.Status),
            _ => request.SortDirection.ToLower() == "asc"
                ? query.OrderBy(po => po.CreatedAt)
                : query.OrderByDescending(po => po.CreatedAt)
        };

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var responses = items.Select(po =>
        {
            var response = po.ToPurchaseOrderResponse();
            response.IsStale = false; // Simplified
            return response;
        }).ToList();

        return new PaginatedResponse<PurchaseOrderResponse>
        {
            Items = responses,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount
        };
    }

    /// <inheritdoc/>
    public async Task<PurchaseOrderDetailResponse> UpdatePurchaseOrderAsync(
        int id,
        UpdatePurchaseOrderRequest request,
        string userId,
        string userRole,
        CancellationToken cancellationToken = default)
    {
        var query = _context.PurchaseOrders
            .Include(po => po.Items)
            .Include(po => po.ShippingAddress)
            .Include(po => po.BillingAddress)
            .AsQueryable();

        // Maintain resource-level ownership check (IDOR protection)
        if (userRole == "employee")
        {
            query = query.Where(po => po.CreatedBy == userId);
        }

        var purchaseOrder = await query.FirstOrDefaultAsync(po => po.Id == id, cancellationToken);

        if (purchaseOrder == null)
        {
            throw new InvalidOperationException($"Purchase order {id} not found");
        }

        // Verify concurrency token
        var currentVersion = Convert.ToBase64String(purchaseOrder.RowVersion);
        if (currentVersion != request.RowVersion)
        {
            throw new DbUpdateConcurrencyException("Purchase order was modified by another user");
        }

        // Update fields
        if (request.CurrencyID.HasValue && request.CurrencyID.Value != purchaseOrder.CurrencyID)
        {
            var currency = await _currencyClient.GetCurrencyAsync(request.CurrencyID.Value, cancellationToken);
            if (currency != null)
            {
                purchaseOrder.CurrencyID = request.CurrencyID.Value;
                purchaseOrder.CurrencyCode = currency.Code;
                purchaseOrder.CurrencySymbol = currency.Symbol;
            }
        }

        if (request.CustomerPO != null)
        {
            purchaseOrder.CustomerPO = request.CustomerPO;
        }

        if (request.ExpectedDeliveryDate.HasValue)
        {
            purchaseOrder.ExpectedDeliveryDate = request.ExpectedDeliveryDate.Value;
        }

        if (request.WHTRate.HasValue)
        {
            purchaseOrder.WHTRate = request.WHTRate.Value;
            purchaseOrder.WHTAmount = _whtService.CalculateWHT(purchaseOrder.SubtotalAmount, request.WHTRate);
            purchaseOrder.TotalAmount = purchaseOrder.SubtotalAmount - (purchaseOrder.WHTAmount ?? 0);
        }

        if (request.Notes != null)
        {
            purchaseOrder.Notes = request.Notes;
        }

        purchaseOrder.LastModifiedBy = userId;
        purchaseOrder.LastModifiedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated purchase order {OrderNumber} by user {UserId}",
            purchaseOrder.OrderNumber, userId);

        return await GetPurchaseOrderByIdAsync(id, userId, userRole, cancellationToken)
            ?? throw new InvalidOperationException("Failed to retrieve updated purchase order");
    }

    /// <inheritdoc/>
    public async Task CancelPurchaseOrderAsync(
        int id,
        string userId,
        string userRole,
        CancellationToken cancellationToken = default)
    {
        var query = _context.PurchaseOrders.AsQueryable();

        // Maintain resource-level ownership check (IDOR protection)
        if (userRole == "employee")
        {
            query = query.Where(po => po.CreatedBy == userId);
        }

        var purchaseOrder = await query.FirstOrDefaultAsync(po => po.Id == id, cancellationToken);

        if (purchaseOrder == null)
        {
            throw new InvalidOperationException($"Purchase order {id} not found");
        }

        purchaseOrder.Status = OrderStatus.Cancelled;
        purchaseOrder.LastModifiedBy = userId;
        purchaseOrder.LastModifiedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        // Publish PurchaseOrderCancelledEvent
        // TODO: Add cancellationReason parameter to CancelPurchaseOrderAsync method
        await _publishEndpoint.Publish(new PurchaseOrderCancelledEvent(
            MessageId: Guid.NewGuid(),
            MessageName: "PurchaseOrderCancelledEvent",
            MessageType: MessageType.Event,
            MessageVersion: "1.0.0",
            PublishedBy: "PurchaseOrderService",
            ConsumedBy: ["MaterialService", "NotificationService"],
            CorrelationId: Guid.NewGuid(),
            CausationId: null,
            OccurredAtUtc: DateTimeOffset.UtcNow,
            IsPublic: false,
            Payload: new PurchaseOrderCancelledEventPayload(
                PurchaseOrderId: purchaseOrder.Id,
                PurchaseOrderNumber: purchaseOrder.OrderNumber,
                CancelledBy: userId,
                CancelledAt: new DateTimeOffset(purchaseOrder.LastModifiedAt ?? DateTime.UtcNow, TimeSpan.Zero),
                CancellationReason: "Cancelled by user"  // Default reason - should be parameterized
            )
        ), cancellationToken);

        _logger.LogInformation("Published PurchaseOrderCancelledEvent for PO {OrderNumber}", purchaseOrder.OrderNumber);

        _logger.LogInformation("Cancelled purchase order {OrderNumber} by user {UserId}",
            purchaseOrder.OrderNumber, userId);
    }

    /// <inheritdoc/>
    public async Task<PurchaseOrderDetailResponse> ApproveAsync(
        int id,
        string userId,
        string userRole,
        CancellationToken cancellationToken = default)
    {
        var query = _context.PurchaseOrders.AsQueryable();

        // Maintain resource-level ownership check (IDOR protection)
        if (userRole == "employee")
        {
            query = query.Where(po => po.CreatedBy == userId);
        }

        var purchaseOrder = await query.FirstOrDefaultAsync(po => po.Id == id, cancellationToken);

        if (purchaseOrder == null)
        {
            throw new InvalidOperationException($"Purchase order {id} not found");
        }

        // Validate current status
        if (purchaseOrder.Status != OrderStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot approve purchase order with status {purchaseOrder.Status}. Expected status: Pending");
        }

        // Update status and approval metadata
        purchaseOrder.Status = OrderStatus.Approved;
        purchaseOrder.ApprovedBy = userId;
        purchaseOrder.ApprovedAt = DateTime.UtcNow;
        purchaseOrder.LastModifiedBy = userId;
        purchaseOrder.LastModifiedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        // Publish PurchaseOrderApprovedEvent
        await _publishEndpoint.Publish(new PurchaseOrderApprovedEvent(
            MessageId: Guid.NewGuid(),
            MessageName: "PurchaseOrderApprovedEvent",
            MessageType: MessageType.Event,
            MessageVersion: "1.0.0",
            PublishedBy: "PurchaseOrderService",
            ConsumedBy: ["MaterialService", "NotificationService"],
            CorrelationId: Guid.NewGuid(),
            CausationId: null,
            OccurredAtUtc: DateTimeOffset.UtcNow,
            IsPublic: false,
            Payload: new PurchaseOrderApprovedEventPayload(
                PurchaseOrderId: purchaseOrder.Id,
                PurchaseOrderNumber: purchaseOrder.OrderNumber,
                ApprovedBy: userId,
                ApprovedAt: new DateTimeOffset(purchaseOrder.ApprovedAt.Value, TimeSpan.Zero)
            )
        ), cancellationToken);

        _logger.LogInformation("Published PurchaseOrderApprovedEvent for PO {OrderNumber}", purchaseOrder.OrderNumber);

        _logger.LogInformation("Approved purchase order {OrderNumber} by user {UserId}",
            purchaseOrder.OrderNumber, userId);

        return await GetPurchaseOrderByIdAsync(id, userId, userRole, cancellationToken)
            ?? throw new InvalidOperationException("Failed to retrieve approved purchase order");
    }

    /// <inheritdoc/>
    public async Task<PurchaseOrderDetailResponse> SendToSupplierAsync(
        int id,
        string userId,
        string userRole,
        CancellationToken cancellationToken = default)
    {
        var query = _context.PurchaseOrders.AsQueryable();

        // Maintain resource-level ownership check (IDOR protection)
        if (userRole == "employee")
        {
            query = query.Where(po => po.CreatedBy == userId);
        }

        var purchaseOrder = await query.FirstOrDefaultAsync(po => po.Id == id, cancellationToken);

        if (purchaseOrder == null)
        {
            throw new InvalidOperationException($"Purchase order {id} not found");
        }

        // Validate current status
        if (purchaseOrder.Status != OrderStatus.Approved)
        {
            throw new InvalidOperationException($"Cannot send purchase order with status {purchaseOrder.Status}. Expected status: Approved");
        }

        // Update status
        purchaseOrder.Status = OrderStatus.Ordered;
        purchaseOrder.LastModifiedBy = userId;
        purchaseOrder.LastModifiedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        // Publish PurchaseOrderSentToSupplierEvent
        await _publishEndpoint.Publish(new PurchaseOrderSentToSupplierEvent(
            MessageId: Guid.NewGuid(),
            MessageName: "PurchaseOrderSentToSupplierEvent",
            MessageType: MessageType.Event,
            MessageVersion: "1.0.0",
            PublishedBy: "PurchaseOrderService",
            ConsumedBy: ["MaterialService", "NotificationService"],
            CorrelationId: Guid.NewGuid(),
            CausationId: null,
            OccurredAtUtc: DateTimeOffset.UtcNow,
            IsPublic: false,
            Payload: new PurchaseOrderSentToSupplierEventPayload(
                PurchaseOrderId: purchaseOrder.Id,
                PurchaseOrderNumber: purchaseOrder.OrderNumber,
                SupplierId: purchaseOrder.SupplierID,
                SentAt: new DateTimeOffset(purchaseOrder.LastModifiedAt.Value, TimeSpan.Zero),
                SentBy: userId
            )
        ), cancellationToken);

        _logger.LogInformation("Published PurchaseOrderSentToSupplierEvent for PO {OrderNumber}", purchaseOrder.OrderNumber);

        _logger.LogInformation("Sent purchase order {OrderNumber} to supplier by user {UserId}",
            purchaseOrder.OrderNumber, userId);

        return await GetPurchaseOrderByIdAsync(id, userId, userRole, cancellationToken)
            ?? throw new InvalidOperationException("Failed to retrieve sent purchase order");
    }

    /// <inheritdoc/>
    public async Task<PurchaseOrderDetailResponse> ReceiveGoodsAsync(
        int id,
        bool isPartialReceipt,
        string userId,
        string userRole,
        CancellationToken cancellationToken = default)
    {
        var query = _context.PurchaseOrders.AsQueryable();

        // Maintain resource-level ownership check (IDOR protection)
        if (userRole == "employee")
        {
            query = query.Where(po => po.CreatedBy == userId);
        }

        var purchaseOrder = await query.FirstOrDefaultAsync(po => po.Id == id, cancellationToken);

        if (purchaseOrder == null)
        {
            throw new InvalidOperationException($"Purchase order {id} not found");
        }

        // Validate current status
        if (purchaseOrder.Status != OrderStatus.Ordered)
        {
            throw new InvalidOperationException($"Cannot receive goods for purchase order with status {purchaseOrder.Status}. Expected status: Ordered");
        }

        // Update status (only to Delivered if full receipt)
        if (!isPartialReceipt)
        {
            purchaseOrder.Status = OrderStatus.Delivered;
        }

        purchaseOrder.LastModifiedBy = userId;
        purchaseOrder.LastModifiedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        // Publish PurchaseOrderReceivedEvent
        await _publishEndpoint.Publish(new PurchaseOrderReceivedEvent(
            MessageId: Guid.NewGuid(),
            MessageName: "PurchaseOrderReceivedEvent",
            MessageType: MessageType.Event,
            MessageVersion: "1.0.0",
            PublishedBy: "PurchaseOrderService",
            ConsumedBy: ["MaterialService", "InventoryService", "NotificationService"],
            CorrelationId: Guid.NewGuid(),
            CausationId: null,
            OccurredAtUtc: DateTimeOffset.UtcNow,
            IsPublic: false,
            Payload: new PurchaseOrderReceivedEventPayload(
                PurchaseOrderId: purchaseOrder.Id,
                PurchaseOrderNumber: purchaseOrder.OrderNumber,
                IsPartialReceipt: isPartialReceipt,
                ReceivedBy: userId,
                ReceivedAt: new DateTimeOffset(purchaseOrder.LastModifiedAt.Value, TimeSpan.Zero)
            )
        ), cancellationToken);

        _logger.LogInformation("Published PurchaseOrderReceivedEvent for PO {OrderNumber} (partial: {IsPartial})",
            purchaseOrder.OrderNumber, isPartialReceipt);

        _logger.LogInformation("Received goods for purchase order {OrderNumber} by user {UserId} (partial: {IsPartial})",
            purchaseOrder.OrderNumber, userId, isPartialReceipt);

        return await GetPurchaseOrderByIdAsync(id, userId, userRole, cancellationToken)
            ?? throw new InvalidOperationException("Failed to retrieve purchase order after receiving goods");
    }

    private string GenerateOrderNumber()
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var random = new Random().Next(1000, 9999);
        return $"PO-{timestamp}-{random}";
    }
}
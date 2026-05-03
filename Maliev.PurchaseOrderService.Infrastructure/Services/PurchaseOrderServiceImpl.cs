using Maliev.PurchaseOrderService.Application.DTOs;
using Maliev.PurchaseOrderService.Application.Interfaces;
using Maliev.PurchaseOrderService.Domain.Entities;
using Maliev.PurchaseOrderService.Domain.Enumerations;
using Maliev.PurchaseOrderService.Infrastructure.Persistence;
using Maliev.PurchaseOrderService.Infrastructure.Search;
using Maliev.MessagingContracts.Contracts.PurchaseOrders;
using Maliev.MessagingContracts.Contracts.Shared;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Maliev.PurchaseOrderService.Infrastructure.Services;

public class PurchaseOrderServiceImpl : IPurchaseOrderService
{
    private readonly PurchaseOrderContext _context;
    private readonly ILogger<PurchaseOrderServiceImpl> _logger;
    private readonly ISupplierServiceClient _supplierClient;
    private readonly IOrderServiceClient _orderClient;
    private readonly ICurrencyServiceClient _currencyClient;
    private readonly IWHTCalculationService _whtCalculator;
    private readonly IAuditLogService _auditLogService;
    private readonly IPublishEndpoint _publishEndpoint;

    public PurchaseOrderServiceImpl(
        PurchaseOrderContext context,
        ILogger<PurchaseOrderServiceImpl> logger,
        ISupplierServiceClient supplierClient,
        IOrderServiceClient orderClient,
        ICurrencyServiceClient currencyClient,
        IWHTCalculationService whtCalculator,
        IAuditLogService auditLogService,
        IPublishEndpoint publishEndpoint)
    {
        _context = context;
        _logger = logger;
        _supplierClient = supplierClient;
        _orderClient = orderClient;
        _currencyClient = currencyClient;
        _whtCalculator = whtCalculator;
        _auditLogService = auditLogService;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<PurchaseOrderDetailResponse> CreateAsync(CreatePurchaseOrderRequest request, string userId, string userRole, CancellationToken cancellationToken = default)
    {
        var supplier = await _supplierClient.GetSupplierAsync(request.SupplierID, cancellationToken)
            ?? throw new InvalidOperationException($"Supplier with ID {request.SupplierID} not found");

        var order = await _orderClient.GetOrderAsync(request.OrderID, cancellationToken)
            ?? throw new InvalidOperationException($"Order with ID {request.OrderID} not found");

        var currency = await _currencyClient.GetCurrencyAsync(request.CurrencyID, cancellationToken)
            ?? throw new InvalidOperationException($"Currency with ID {request.CurrencyID} not found");

        var orderItems = await _orderClient.GetOrderItemsAsync(request.OrderID, cancellationToken);

        List<OrderItem> itemsToAdd;
        if (request.Items.Count > 0)
        {
            var itemDict = orderItems.ToDictionary(i => i.Id);
            itemsToAdd = new List<OrderItem>();

            foreach (var partialItem in request.Items)
            {
                if (!itemDict.TryGetValue(partialItem.ExternalOrderItemId, out var externalItem))
                    throw new InvalidOperationException($"Order item {partialItem.ExternalOrderItemId} not found");

                itemsToAdd.Add(new OrderItem
                {
                    ExternalOrderItemId = externalItem.Id,
                    ProductCode = externalItem.ProductCode,
                    ProductName = externalItem.ProductName,
                    Quantity = partialItem.Quantity,
                    UnitOfMeasure = externalItem.UnitOfMeasure,
                    UnitPrice = externalItem.UnitPrice,
                    TotalPrice = partialItem.Quantity * externalItem.UnitPrice,
                    Currency = externalItem.Currency,
                    CachedAt = DateTime.UtcNow,
                    ExternallyModified = false
                });
            }
        }
        else
        {
            itemsToAdd = orderItems.Select(i => new OrderItem
            {
                ExternalOrderItemId = i.Id,
                ProductCode = i.ProductCode,
                ProductName = i.ProductName,
                Quantity = i.Quantity,
                UnitOfMeasure = i.UnitOfMeasure,
                UnitPrice = i.UnitPrice,
                TotalPrice = i.TotalPrice,
                Currency = i.Currency,
                CachedAt = DateTime.UtcNow,
                ExternallyModified = false
            }).ToList();
        }

        var subtotal = itemsToAdd.Sum(i => i.TotalPrice);
        var whtAmount = _whtCalculator.CalculateWHT(subtotal, request.WHTRate > 0 ? request.WHTRate : null);
        var totalAmount = subtotal - whtAmount;

        var orderNumber = $"PO-{DateTime.UtcNow:yyyy}-{DateTime.UtcNow:HHmmss}";

        var purchaseOrder = new PurchaseOrder
        {
            OrderNumber = orderNumber,
            SupplierID = request.SupplierID,
            SupplierName = supplier.Name,
            SupplierContactInfo = supplier.ContactInfo,
            OrderID = request.OrderID,
            CurrencyID = request.CurrencyID,
            CurrencyCode = currency.Code,
            CurrencySymbol = currency.Symbol,
            OrderType = request.OrderType,
            OrderDate = DateTime.UtcNow,
            Status = OrderStatus.Pending,
            DepartmentId = 1,
            CustomerPO = request.CustomerPO,
            ExpectedDeliveryDate = request.ExpectedDeliveryDate,
            SubtotalAmount = subtotal,
            WHTRate = request.WHTRate > 0 ? request.WHTRate : null,
            WHTAmount = whtAmount,
            TotalAmount = totalAmount,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow,
            Notes = request.Notes,
            Items = itemsToAdd
        };

        if (request.ShippingAddress != null)
        {
            purchaseOrder.ShippingAddress = MapToAddress(request.ShippingAddress, userId);
        }

        if (request.BillingAddress != null)
        {
            purchaseOrder.BillingAddress = MapToAddress(request.BillingAddress, userId);
        }

        _context.PurchaseOrders.Add(purchaseOrder);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditLogService.LogAuditAsync(
            "PurchaseOrder",
            purchaseOrder.Id.ToString(),
            AuditAction.Create,
            userId,
            userRole,
            null,
            JsonSerializer.Serialize(new { purchaseOrder.OrderNumber, purchaseOrder.Status }),
            null,
            cancellationToken);

        await _publishEndpoint.Publish(new PurchaseOrderCreatedEvent(
            Guid.NewGuid(),
            "PurchaseOrderCreatedEvent",
            MessageType.Event,
            "1.0",
            "PurchaseOrderService",
            Array.Empty<string>(),
            Guid.NewGuid(),
            null,
            DateTimeOffset.UtcNow,
            false,
            new PurchaseOrderCreatedEventPayload(
                purchaseOrder.Id,
                purchaseOrder.OrderNumber,
                purchaseOrder.SupplierID,
                (double)purchaseOrder.TotalAmount,
                purchaseOrder.CurrencyCode,
                purchaseOrder.ExpectedDeliveryDate.HasValue ? new DateTimeOffset(purchaseOrder.ExpectedDeliveryDate.Value) : null,
                userId,
                new DateTimeOffset(purchaseOrder.CreatedAt)
            )), cancellationToken);

        await _publishEndpoint.Publish(
            PurchaseOrderSearchDocumentMapper.ToUpsertEvent(purchaseOrder, DateTimeOffset.UtcNow),
            cancellationToken);

        _logger.LogInformation("Created purchase order {OrderNumber} with ID {Id}", purchaseOrder.OrderNumber, purchaseOrder.Id);

        return MapToDetailResponse(purchaseOrder);
    }

    public async Task<PurchaseOrderDetailResponse?> GetByIdAsync(int id, string userId, string userRole, CancellationToken cancellationToken = default)
    {
        var query = _context.PurchaseOrders
            .Include(po => po.Items)
            .Include(po => po.ShippingAddress)
            .Include(po => po.BillingAddress)
            .Include(po => po.Files)
            .AsQueryable();

        if (userRole == "employee")
        {
            query = query.Where(po => po.CreatedBy == userId);
        }

        var purchaseOrder = await query.FirstOrDefaultAsync(po => po.Id == id, cancellationToken);

        if (purchaseOrder == null)
        {
            return null;
        }

        return MapToDetailResponse(purchaseOrder);
    }

    public async Task<PaginatedResponse<PurchaseOrderResponse>> SearchAsync(SearchPurchaseOrdersRequest request, string userId, string userRole, CancellationToken cancellationToken = default)
    {
        var query = _context.PurchaseOrders.AsNoTracking().AsQueryable();

        if (userRole == "employee")
        {
            query = query.Where(po => po.CreatedBy == userId);
        }

        if (request.SupplierId.HasValue)
        {
            query = query.Where(po => po.SupplierID == request.SupplierId.Value);
        }

        if (request.Status.HasValue)
        {
            query = query.Where(po => po.Status == request.Status.Value);
        }

        if (request.OrderType.HasValue)
        {
            query = query.Where(po => po.OrderType == request.OrderType.Value);
        }

        if (request.OrderId.HasValue)
        {
            query = query.Where(po => po.OrderID == request.OrderId.Value);
        }

        if (request.FromDate.HasValue)
        {
            query = query.Where(po => po.CreatedAt >= request.FromDate.Value);
        }

        if (request.ToDate.HasValue)
        {
            query = query.Where(po => po.CreatedAt <= request.ToDate.Value);
        }

        var sortBy = request.SortBy ?? "CreatedAt";
        var sortDir = request.SortDirection?.ToLower() ?? "desc";

        query = sortBy.ToLower() switch
        {
            "ordernumber" => sortDir == "asc" ? query.OrderBy(po => po.OrderNumber) : query.OrderByDescending(po => po.OrderNumber),
            "totalamount" => sortDir == "asc" ? query.OrderBy(po => po.TotalAmount) : query.OrderByDescending(po => po.TotalAmount),
            "status" => sortDir == "asc" ? query.OrderBy(po => po.Status) : query.OrderByDescending(po => po.Status),
            "createdat" => sortDir == "asc" ? query.OrderBy(po => po.CreatedAt) : query.OrderByDescending(po => po.CreatedAt),
            _ => sortDir == "asc" ? query.OrderBy(po => po.CreatedAt) : query.OrderByDescending(po => po.CreatedAt)
        };

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new PaginatedResponse<PurchaseOrderResponse>(
            items.Select(MapToResponse).ToList(),
            totalCount,
            request.Page,
            request.PageSize
        );
    }

    public async Task<PurchaseOrderDetailResponse> UpdateAsync(int id, UpdatePurchaseOrderRequest request, string userId, string userRole, CancellationToken cancellationToken = default)
    {
        var purchaseOrder = await _context.PurchaseOrders
            .Include(po => po.Items)
            .Include(po => po.ShippingAddress)
            .Include(po => po.BillingAddress)
            .FirstOrDefaultAsync(po => po.Id == id, cancellationToken)
            ?? throw new InvalidOperationException($"Purchase order with ID {id} not found");

        if (userRole == "employee" && purchaseOrder.CreatedBy != userId)
        {
            throw new InvalidOperationException("You can only update your own purchase orders");
        }

        if (purchaseOrder.Status != OrderStatus.Pending)
        {
            throw new InvalidOperationException("Only pending purchase orders can be updated");
        }

        if (!string.IsNullOrEmpty(request.RowVersion))
        {
            var currentVersion = _context.Entry(purchaseOrder).Property<uint>("xmin").CurrentValue.ToString();
            if (currentVersion != request.RowVersion)
            {
                throw new DbUpdateConcurrencyException("Purchase order was modified by another user");
            }
        }

        bool currencyChanged = false;
        if (request.CurrencyID.HasValue && request.CurrencyID.Value != purchaseOrder.CurrencyID)
        {
            var currency = await _currencyClient.GetCurrencyAsync(request.CurrencyID.Value, cancellationToken)
                ?? throw new InvalidOperationException($"Currency with ID {request.CurrencyID.Value} not found");

            purchaseOrder.CurrencyID = currency.Id;
            purchaseOrder.CurrencyCode = currency.Code;
            purchaseOrder.CurrencySymbol = currency.Symbol;
            currencyChanged = true;
        }

        if (!string.IsNullOrEmpty(request.CustomerPO))
        {
            purchaseOrder.CustomerPO = request.CustomerPO;
        }

        if (request.ExpectedDeliveryDate.HasValue)
        {
            purchaseOrder.ExpectedDeliveryDate = request.ExpectedDeliveryDate.Value;
        }

        if (request.WHTRate.HasValue)
        {
            purchaseOrder.WHTRate = request.WHTRate.Value > 0 ? request.WHTRate.Value : null;
        }

        if (!string.IsNullOrEmpty(request.Notes))
        {
            purchaseOrder.Notes = request.Notes;
        }

        if (request.Items != null && request.Items.Count > 0)
        {
            _context.OrderItems.RemoveRange(purchaseOrder.Items);

            var orderItems = await _orderClient.GetOrderItemsAsync(purchaseOrder.OrderID, cancellationToken);
            var itemDict = orderItems.ToDictionary(i => i.Id);

            foreach (var partialItem in request.Items)
            {
                if (!itemDict.TryGetValue(partialItem.ExternalOrderItemId, out var externalItem))
                    throw new InvalidOperationException($"Order item {partialItem.ExternalOrderItemId} not found");

                purchaseOrder.Items.Add(new OrderItem
                {
                    PurchaseOrderId = purchaseOrder.Id,
                    ExternalOrderItemId = externalItem.Id,
                    ProductCode = externalItem.ProductCode,
                    ProductName = externalItem.ProductName,
                    Quantity = partialItem.Quantity,
                    UnitOfMeasure = externalItem.UnitOfMeasure,
                    UnitPrice = externalItem.UnitPrice,
                    TotalPrice = partialItem.Quantity * externalItem.UnitPrice,
                    Currency = externalItem.Currency,
                    CachedAt = DateTime.UtcNow,
                    ExternallyModified = false
                });
            }

            var subtotal = purchaseOrder.Items.Sum(i => i.TotalPrice);
            var whtAmount = _whtCalculator.CalculateWHT(subtotal, purchaseOrder.WHTRate);
            purchaseOrder.SubtotalAmount = subtotal;
            purchaseOrder.WHTAmount = whtAmount;
            purchaseOrder.TotalAmount = subtotal - whtAmount;
        }
        else if (currencyChanged || request.WHTRate.HasValue)
        {
            var subtotal = purchaseOrder.Items.Sum(i => i.TotalPrice);
            var whtAmount = _whtCalculator.CalculateWHT(subtotal, purchaseOrder.WHTRate);
            purchaseOrder.SubtotalAmount = subtotal;
            purchaseOrder.WHTAmount = whtAmount;
            purchaseOrder.TotalAmount = subtotal - whtAmount;
        }

        if (request.ShippingAddress != null)
        {
            if (purchaseOrder.ShippingAddress != null)
            {
                purchaseOrder.ShippingAddress.ContactName = request.ShippingAddress.ContactName ?? purchaseOrder.ShippingAddress.ContactName;
                purchaseOrder.ShippingAddress.AddressLine1 = request.ShippingAddress.AddressLine1 ?? purchaseOrder.ShippingAddress.AddressLine1;
                purchaseOrder.ShippingAddress.AddressLine2 = request.ShippingAddress.AddressLine2 ?? purchaseOrder.ShippingAddress.AddressLine2;
                purchaseOrder.ShippingAddress.City = request.ShippingAddress.City ?? purchaseOrder.ShippingAddress.City;
                purchaseOrder.ShippingAddress.StateProvince = request.ShippingAddress.StateProvince ?? purchaseOrder.ShippingAddress.StateProvince;
                purchaseOrder.ShippingAddress.PostalCode = request.ShippingAddress.PostalCode ?? purchaseOrder.ShippingAddress.PostalCode;
                purchaseOrder.ShippingAddress.Country = request.ShippingAddress.Country ?? purchaseOrder.ShippingAddress.Country;
                purchaseOrder.ShippingAddress.LastModifiedBy = userId;
                purchaseOrder.ShippingAddress.LastModifiedAt = DateTime.UtcNow;
            }
            else
            {
                purchaseOrder.ShippingAddress = MapToAddress(request.ShippingAddress, userId);
            }
        }

        if (request.BillingAddress != null)
        {
            if (purchaseOrder.BillingAddress != null)
            {
                purchaseOrder.BillingAddress.ContactName = request.BillingAddress.ContactName ?? purchaseOrder.BillingAddress.ContactName;
                purchaseOrder.BillingAddress.AddressLine1 = request.BillingAddress.AddressLine1 ?? purchaseOrder.BillingAddress.AddressLine1;
                purchaseOrder.BillingAddress.AddressLine2 = request.BillingAddress.AddressLine2 ?? purchaseOrder.BillingAddress.AddressLine2;
                purchaseOrder.BillingAddress.City = request.BillingAddress.City ?? purchaseOrder.BillingAddress.City;
                purchaseOrder.BillingAddress.StateProvince = request.BillingAddress.StateProvince ?? purchaseOrder.BillingAddress.StateProvince;
                purchaseOrder.BillingAddress.PostalCode = request.BillingAddress.PostalCode ?? purchaseOrder.BillingAddress.PostalCode;
                purchaseOrder.BillingAddress.Country = request.BillingAddress.Country ?? purchaseOrder.BillingAddress.Country;
                purchaseOrder.BillingAddress.LastModifiedBy = userId;
                purchaseOrder.BillingAddress.LastModifiedAt = DateTime.UtcNow;
            }
            else
            {
                purchaseOrder.BillingAddress = MapToAddress(request.BillingAddress, userId);
            }
        }

        purchaseOrder.LastModifiedBy = userId;
        purchaseOrder.LastModifiedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        await _auditLogService.LogAuditAsync(
            "PurchaseOrder",
            purchaseOrder.Id.ToString(),
            AuditAction.Update,
            userId,
            userRole,
            null,
            JsonSerializer.Serialize(new { purchaseOrder.OrderNumber }),
            null,
            cancellationToken);

        await _publishEndpoint.Publish(
            PurchaseOrderSearchDocumentMapper.ToUpsertEvent(purchaseOrder, DateTimeOffset.UtcNow),
            cancellationToken);

        _logger.LogInformation("Updated purchase order {OrderNumber}", purchaseOrder.OrderNumber);

        return MapToDetailResponse(purchaseOrder);
    }

    public async Task<PurchaseOrderDetailResponse> ApproveAsync(int id, string userId, string userRole, CancellationToken cancellationToken = default)
    {
        var purchaseOrder = await _context.PurchaseOrders
            .Include(po => po.Items)
            .Include(po => po.ShippingAddress)
            .Include(po => po.BillingAddress)
            .Include(po => po.Files)
            .FirstOrDefaultAsync(po => po.Id == id, cancellationToken)
            ?? throw new InvalidOperationException($"Purchase order with ID {id} not found");

        if (purchaseOrder.Status != OrderStatus.Pending)
        {
            throw new InvalidOperationException("Purchase order must be in Pending status to approve");
        }

        purchaseOrder.Status = OrderStatus.Approved;
        purchaseOrder.ApprovedBy = userId;
        purchaseOrder.ApprovedAt = DateTime.UtcNow;
        purchaseOrder.LastModifiedBy = userId;
        purchaseOrder.LastModifiedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        await _auditLogService.LogAuditAsync(
            "PurchaseOrder",
            purchaseOrder.Id.ToString(),
            AuditAction.Approve,
            userId,
            userRole,
            null,
            JsonSerializer.Serialize(new { purchaseOrder.OrderNumber, Status = purchaseOrder.Status.ToString() }),
            null,
            cancellationToken);

        await _publishEndpoint.Publish(new PurchaseOrderApprovedEvent(
            Guid.NewGuid(),
            "PurchaseOrderApprovedEvent",
            MessageType.Event,
            "1.0",
            "PurchaseOrderService",
            Array.Empty<string>(),
            Guid.NewGuid(),
            null,
            DateTimeOffset.UtcNow,
            false,
            new PurchaseOrderApprovedEventPayload(
                purchaseOrder.Id,
                purchaseOrder.OrderNumber,
                userId,
                new DateTimeOffset(purchaseOrder.ApprovedAt!.Value)
            )), cancellationToken);

        await _publishEndpoint.Publish(
            PurchaseOrderSearchDocumentMapper.ToUpsertEvent(purchaseOrder, DateTimeOffset.UtcNow),
            cancellationToken);

        _logger.LogInformation("Approved purchase order {OrderNumber}", purchaseOrder.OrderNumber);

        return MapToDetailResponse(purchaseOrder);
    }

    public async Task<PurchaseOrderDetailResponse> CancelAsync(int id, string reason, string userId, string userRole, CancellationToken cancellationToken = default)
    {
        var purchaseOrder = await _context.PurchaseOrders
            .Include(po => po.Items)
            .Include(po => po.ShippingAddress)
            .Include(po => po.BillingAddress)
            .Include(po => po.Files)
            .FirstOrDefaultAsync(po => po.Id == id, cancellationToken)
            ?? throw new InvalidOperationException($"Purchase order with ID {id} not found");

        if (purchaseOrder.Status != OrderStatus.Pending && purchaseOrder.Status != OrderStatus.Approved)
        {
            throw new InvalidOperationException("Purchase order can only be cancelled from Pending or Approved status");
        }

        var oldStatus = purchaseOrder.Status;
        purchaseOrder.Status = OrderStatus.Cancelled;
        purchaseOrder.LastModifiedBy = userId;
        purchaseOrder.LastModifiedAt = DateTime.UtcNow;
        purchaseOrder.Notes = string.IsNullOrEmpty(purchaseOrder.Notes)
            ? $"Cancelled: {reason}"
            : $"{purchaseOrder.Notes}\nCancelled: {reason}";

        await _context.SaveChangesAsync(cancellationToken);

        await _auditLogService.LogAuditAsync(
            "PurchaseOrder",
            purchaseOrder.Id.ToString(),
            AuditAction.Cancel,
            userId,
            userRole,
            JsonSerializer.Serialize(new { oldStatus }),
            JsonSerializer.Serialize(new { purchaseOrder.Status }),
            reason,
            cancellationToken);

        await _publishEndpoint.Publish(new PurchaseOrderCancelledEvent(
            Guid.NewGuid(),
            "PurchaseOrderCancelledEvent",
            MessageType.Event,
            "1.0",
            "PurchaseOrderService",
            Array.Empty<string>(),
            Guid.NewGuid(),
            null,
            DateTimeOffset.UtcNow,
            false,
            new PurchaseOrderCancelledEventPayload(
                purchaseOrder.Id,
                purchaseOrder.OrderNumber,
                userId,
                new DateTimeOffset(purchaseOrder.LastModifiedAt!.Value),
                reason
            )), cancellationToken);

        await _publishEndpoint.Publish(
            PurchaseOrderSearchDocumentMapper.ToUpsertEvent(purchaseOrder, DateTimeOffset.UtcNow),
            cancellationToken);

        _logger.LogInformation("Cancelled purchase order {OrderNumber}, reason: {Reason}", purchaseOrder.OrderNumber, reason);

        return MapToDetailResponse(purchaseOrder);
    }

    public async Task<PurchaseOrderDetailResponse> SendToSupplierAsync(int id, string userId, string userRole, CancellationToken cancellationToken = default)
    {
        var purchaseOrder = await _context.PurchaseOrders
            .Include(po => po.Items)
            .Include(po => po.ShippingAddress)
            .Include(po => po.BillingAddress)
            .Include(po => po.Files)
            .FirstOrDefaultAsync(po => po.Id == id, cancellationToken)
            ?? throw new InvalidOperationException($"Purchase order with ID {id} not found");

        if (purchaseOrder.Status != OrderStatus.Approved)
        {
            throw new InvalidOperationException("Purchase order must be in Approved status to send to supplier");
        }

        purchaseOrder.Status = OrderStatus.Ordered;
        purchaseOrder.LastModifiedBy = userId;
        purchaseOrder.LastModifiedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        await _auditLogService.LogAuditAsync(
            "PurchaseOrder",
            purchaseOrder.Id.ToString(),
            AuditAction.ExternalValidation,
            userId,
            userRole,
            JsonSerializer.Serialize(new { Status = OrderStatus.Approved.ToString() }),
            JsonSerializer.Serialize(new { Status = OrderStatus.Ordered.ToString() }),
            cancellationToken: cancellationToken);

        await _publishEndpoint.Publish(new PurchaseOrderSentToSupplierEvent(
            Guid.NewGuid(),
            "PurchaseOrderSentToSupplierEvent",
            MessageType.Event,
            "1.0",
            "PurchaseOrderService",
            Array.Empty<string>(),
            Guid.NewGuid(),
            null,
            DateTimeOffset.UtcNow,
            false,
            new PurchaseOrderSentToSupplierEventPayload(
                purchaseOrder.Id,
                purchaseOrder.OrderNumber,
                purchaseOrder.SupplierID,
                new DateTimeOffset(purchaseOrder.LastModifiedAt!.Value),
                userId
            )), cancellationToken);

        await _publishEndpoint.Publish(
            PurchaseOrderSearchDocumentMapper.ToUpsertEvent(purchaseOrder, DateTimeOffset.UtcNow),
            cancellationToken);

        _logger.LogInformation("Sent purchase order {OrderNumber} to supplier", purchaseOrder.OrderNumber);

        return MapToDetailResponse(purchaseOrder);
    }

    public async Task<PurchaseOrderDetailResponse> ReceiveGoodsAsync(int id, bool isPartialReceipt, string userId, string userRole, CancellationToken cancellationToken = default)
    {
        var purchaseOrder = await _context.PurchaseOrders
            .Include(po => po.Items)
            .Include(po => po.ShippingAddress)
            .Include(po => po.BillingAddress)
            .Include(po => po.Files)
            .FirstOrDefaultAsync(po => po.Id == id, cancellationToken)
            ?? throw new InvalidOperationException($"Purchase order with ID {id} not found");

        if (purchaseOrder.Status != OrderStatus.Ordered)
        {
            throw new InvalidOperationException("Purchase order must be in Ordered status to receive goods");
        }

        if (!isPartialReceipt)
        {
            purchaseOrder.Status = OrderStatus.Delivered;
        }

        purchaseOrder.LastModifiedBy = userId;
        purchaseOrder.LastModifiedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        await _auditLogService.LogAuditAsync(
            "PurchaseOrder",
            purchaseOrder.Id.ToString(),
            AuditAction.Update,
            userId,
            userRole,
            JsonSerializer.Serialize(new { Status = OrderStatus.Ordered.ToString() }),
            JsonSerializer.Serialize(new { Status = purchaseOrder.Status.ToString(), IsPartialReceipt = isPartialReceipt }),
            cancellationToken: cancellationToken);

        await _publishEndpoint.Publish(new PurchaseOrderReceivedEvent(
            Guid.NewGuid(),
            "PurchaseOrderReceivedEvent",
            MessageType.Event,
            "1.0",
            "PurchaseOrderService",
            Array.Empty<string>(),
            Guid.NewGuid(),
            null,
            DateTimeOffset.UtcNow,
            false,
            new PurchaseOrderReceivedEventPayload(
                purchaseOrder.Id,
                purchaseOrder.OrderNumber,
                new DateTimeOffset(purchaseOrder.LastModifiedAt!.Value),
                userId,
                isPartialReceipt
            )), cancellationToken);

        await _publishEndpoint.Publish(
            PurchaseOrderSearchDocumentMapper.ToUpsertEvent(purchaseOrder, DateTimeOffset.UtcNow),
            cancellationToken);

        _logger.LogInformation("Received goods for purchase order {OrderNumber}, partial: {IsPartial}", purchaseOrder.OrderNumber, isPartialReceipt);

        return MapToDetailResponse(purchaseOrder);
    }

    private PurchaseOrderDetailResponse MapToDetailResponse(PurchaseOrder purchaseOrder)
    {
        return new PurchaseOrderDetailResponse
        {
            Id = purchaseOrder.Id,
            OrderNumber = purchaseOrder.OrderNumber,
            OrderType = purchaseOrder.OrderType.ToString(),
            Status = purchaseOrder.Status.ToString(),
            SupplierID = purchaseOrder.SupplierID,
            SupplierName = purchaseOrder.SupplierName ?? string.Empty,
            SupplierContactInfo = purchaseOrder.SupplierContactInfo,
            OrderID = purchaseOrder.OrderID,
            CustomerPO = purchaseOrder.CustomerPO,
            CurrencyID = purchaseOrder.CurrencyID,
            CurrencyCode = purchaseOrder.CurrencyCode ?? string.Empty,
            CurrencySymbol = purchaseOrder.CurrencySymbol ?? string.Empty,
            OrderDate = purchaseOrder.OrderDate,
            SubtotalAmount = purchaseOrder.SubtotalAmount,
            WHTRate = purchaseOrder.WHTRate,
            WHTAmount = purchaseOrder.WHTAmount,
            TotalAmount = purchaseOrder.TotalAmount,
            ExpectedDeliveryDate = purchaseOrder.ExpectedDeliveryDate,
            CreatedBy = purchaseOrder.CreatedBy,
            CreatedAt = purchaseOrder.CreatedAt,
            LastModifiedBy = purchaseOrder.LastModifiedBy,
            LastModifiedAt = purchaseOrder.LastModifiedAt,
            ApprovedBy = purchaseOrder.ApprovedBy,
            ApprovedAt = purchaseOrder.ApprovedAt,
            Notes = purchaseOrder.Notes,
            RowVersion = _context.Entry(purchaseOrder).Property<uint>("xmin").CurrentValue.ToString(),
            Items = purchaseOrder.Items?.Select(i => new OrderItemResponse
            {
                Id = i.Id,
                ExternalOrderItemId = i.ExternalOrderItemId,
                ProductCode = i.ProductCode ?? string.Empty,
                ProductName = i.ProductName,
                Quantity = i.Quantity,
                UnitOfMeasure = i.UnitOfMeasure,
                UnitPrice = i.UnitPrice,
                TotalPrice = i.TotalPrice,
                Currency = i.Currency,
                Notes = i.Notes,
                CachedAt = i.CachedAt,
                ExternallyModified = i.ExternallyModified
            }).ToList() ?? new List<OrderItemResponse>(),
            ShippingAddress = purchaseOrder.ShippingAddress != null ? new AddressResponse
            {
                Id = purchaseOrder.ShippingAddress.Id,
                AddressType = purchaseOrder.ShippingAddress.AddressType,
                CompanyName = purchaseOrder.ShippingAddress.CompanyName,
                ContactName = purchaseOrder.ShippingAddress.ContactName,
                AddressLine1 = purchaseOrder.ShippingAddress.AddressLine1,
                AddressLine2 = purchaseOrder.ShippingAddress.AddressLine2,
                City = purchaseOrder.ShippingAddress.City,
                StateProvince = purchaseOrder.ShippingAddress.StateProvince,
                PostalCode = purchaseOrder.ShippingAddress.PostalCode,
                Country = purchaseOrder.ShippingAddress.Country,
                PhoneNumber = purchaseOrder.ShippingAddress.PhoneNumber,
                EmailAddress = purchaseOrder.ShippingAddress.EmailAddress
            } : null,
            BillingAddress = purchaseOrder.BillingAddress != null ? new AddressResponse
            {
                Id = purchaseOrder.BillingAddress.Id,
                AddressType = purchaseOrder.BillingAddress.AddressType,
                CompanyName = purchaseOrder.BillingAddress.CompanyName,
                ContactName = purchaseOrder.BillingAddress.ContactName,
                AddressLine1 = purchaseOrder.BillingAddress.AddressLine1,
                AddressLine2 = purchaseOrder.BillingAddress.AddressLine2,
                City = purchaseOrder.BillingAddress.City,
                StateProvince = purchaseOrder.BillingAddress.StateProvince,
                PostalCode = purchaseOrder.BillingAddress.PostalCode,
                Country = purchaseOrder.BillingAddress.Country,
                PhoneNumber = purchaseOrder.BillingAddress.PhoneNumber,
                EmailAddress = purchaseOrder.BillingAddress.EmailAddress
            } : null,
            Files = purchaseOrder.Files?.Select(f => new PurchaseOrderFileResponse
            {
                Id = f.Id,
                PurchaseOrderId = f.PurchaseOrderId,
                FileName = f.FileName,
                ObjectName = f.ObjectName,
                FileSize = f.FileSize,
                ContentType = f.ContentType,
                DocumentType = f.DocumentType,
                UploadedAt = f.UploadedAt,
                UploadedBy = f.UploadedBy,
                Description = f.Description
            }).ToList() ?? new List<PurchaseOrderFileResponse>()
        };
    }

    private static PurchaseOrderResponse MapToResponse(PurchaseOrder purchaseOrder)
    {
        return new PurchaseOrderResponse
        {
            Id = purchaseOrder.Id,
            OrderNumber = purchaseOrder.OrderNumber,
            OrderType = purchaseOrder.OrderType.ToString(),
            Status = purchaseOrder.Status.ToString(),
            SupplierID = purchaseOrder.SupplierID,
            SupplierName = purchaseOrder.SupplierName ?? string.Empty,
            OrderID = purchaseOrder.OrderID,
            CurrencyCode = purchaseOrder.CurrencyCode ?? string.Empty,
            TotalAmount = purchaseOrder.TotalAmount,
            ExpectedDeliveryDate = purchaseOrder.ExpectedDeliveryDate,
            CreatedAt = purchaseOrder.CreatedAt
        };
    }

    private static Address MapToAddress(CreateAddressRequest request, string userId)
    {
        return new Address
        {
            AddressType = request.AddressType,
            CompanyName = request.CompanyName,
            ContactName = request.ContactName,
            AddressLine1 = request.AddressLine1,
            AddressLine2 = request.AddressLine2,
            City = request.City,
            StateProvince = request.StateProvince,
            PostalCode = request.PostalCode,
            Country = request.Country,
            PhoneNumber = request.PhoneNumber,
            EmailAddress = request.EmailAddress,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static Address MapToAddress(UpdateAddressRequest request, string userId)
    {
        return new Address
        {
            AddressType = request.AddressType ?? AddressType.Shipping,
            CompanyName = request.CompanyName,
            ContactName = request.ContactName ?? string.Empty,
            AddressLine1 = request.AddressLine1 ?? string.Empty,
            AddressLine2 = request.AddressLine2,
            City = request.City ?? string.Empty,
            StateProvince = request.StateProvince,
            PostalCode = request.PostalCode ?? string.Empty,
            Country = request.Country ?? string.Empty,
            PhoneNumber = request.PhoneNumber,
            EmailAddress = request.EmailAddress,
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow
        };
    }
}

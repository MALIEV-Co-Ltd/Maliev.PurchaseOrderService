using AutoMapper;
using Maliev.PurchaseOrderService.Api.DTOs;
using Maliev.PurchaseOrderService.Api.ExternalServices;
using Maliev.PurchaseOrderService.Common.Enumerations;
using Maliev.PurchaseOrderService.Data;
using Maliev.PurchaseOrderService.Data.Entities;
using Maliev.PurchaseOrderService.Data.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Text.Json;

namespace Maliev.PurchaseOrderService.Api.Services;

/// <summary>
/// Main service for purchase order business logic with CRUD operations
/// </summary>
public class PurchaseOrderService : IPurchaseOrderService
{
    private readonly PurchaseOrderContext _context;
    private readonly IMapper _mapper;
    private readonly IWHTCalculationService _whtService;
    private readonly IPdfGenerationService _pdfService;
    private readonly IDomainEventService _domainEventService;
    private readonly ISupplierServiceClient _supplierService;
    private readonly IOrderServiceClient _orderService;
    private readonly ICurrencyServiceClient _currencyService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<PurchaseOrderService> _logger;

    public PurchaseOrderService(
        PurchaseOrderContext context,
        IMapper mapper,
        IWHTCalculationService whtService,
        IPdfGenerationService pdfService,
        IDomainEventService domainEventService,
        ISupplierServiceClient supplierService,
        IOrderServiceClient orderService,
        ICurrencyServiceClient currencyService,
        IMemoryCache cache,
        ILogger<PurchaseOrderService> logger)
    {
        _context = context;
        _mapper = mapper;
        _whtService = whtService;
        _pdfService = pdfService;
        _domainEventService = domainEventService;
        _supplierService = supplierService;
        _orderService = orderService;
        _currencyService = currencyService;
        _cache = cache;
        _logger = logger;
    }

    public async Task<PaginatedResponse<PurchaseOrderDto>> GetPurchaseOrdersAsync(
        SearchPurchaseOrdersRequest searchRequest,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting purchase orders with search criteria");

        try
        {
            var query = _context.PurchaseOrders.AsQueryable();

            // Apply filters
            query = ApplyFilters(query, searchRequest);

            // Apply search term
            if (!string.IsNullOrWhiteSpace(searchRequest.SearchTerm))
            {
                var searchTerm = searchRequest.SearchTerm.Trim();
                query = query.Where(po =>
                    po.OrderNumber.Contains(searchTerm) ||
                    (po.CustomerPO != null && po.CustomerPO.Contains(searchTerm)) ||
                    (po.Notes != null && po.Notes.Contains(searchTerm)) ||
                    (po.SupplierName != null && po.SupplierName.Contains(searchTerm)));
            }

            // Include related data if requested
            if (searchRequest.IncludeOrderItems)
            {
                query = query.Include(po => po.OrderItems);
            }

            if (searchRequest.IncludeAddresses)
            {
                query = query.Include(po => po.ShippingAddress)
                           .Include(po => po.BillingAddress);
            }

            // Get total count
            var totalCount = await query.CountAsync(cancellationToken);

            // Apply sorting
            query = ApplySorting(query, searchRequest.SortBy, searchRequest.SortDirection);

            // Apply pagination
            var skip = (searchRequest.Page - 1) * searchRequest.PageSize;
            var purchaseOrders = await query
                .Skip(skip)
                .Take(searchRequest.PageSize)
                .ToListAsync(cancellationToken);

            var purchaseOrderDtos = _mapper.Map<List<PurchaseOrderDto>>(purchaseOrders);

            var totalPages = (int)Math.Ceiling((double)totalCount / searchRequest.PageSize);

            return new PaginatedResponse<PurchaseOrderDto>
            {
                Data = purchaseOrderDtos,
                Page = searchRequest.Page,
                PageSize = searchRequest.PageSize,
                TotalCount = totalCount,
                TotalPages = totalPages,
                HasPreviousPage = searchRequest.Page > 1,
                HasNextPage = searchRequest.Page < totalPages,
                Filters = searchRequest,
                Sort = new { searchRequest.SortBy, searchRequest.SortDirection }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting purchase orders");
            throw;
        }
    }

    public async Task<PurchaseOrderDto?> GetPurchaseOrderByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting purchase order by ID {PurchaseOrderId}", id);

        var purchaseOrder = await _context.PurchaseOrders
            .Include(po => po.OrderItems)
            .Include(po => po.ShippingAddress)
            .Include(po => po.BillingAddress)
            .FirstOrDefaultAsync(po => po.Id == id && !po.IsDeleted, cancellationToken);

        if (purchaseOrder == null)
        {
            return null;
        }

        return _mapper.Map<PurchaseOrderDto>(purchaseOrder);
    }

    public async Task<PurchaseOrderDto?> GetPurchaseOrderByIdAsync(
        int id,
        string userId,
        List<string> userRoles,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting purchase order by ID {PurchaseOrderId} for user {UserId} with roles {UserRoles}",
            id, userId, string.Join(",", userRoles));

        var purchaseOrder = await _context.PurchaseOrders
            .Include(po => po.OrderItems)
            .Include(po => po.ShippingAddress)
            .Include(po => po.BillingAddress)
            .FirstOrDefaultAsync(po => po.Id == id && !po.IsDeleted, cancellationToken);

        if (purchaseOrder == null)
        {
            return null;
        }

        // Role-based access control
        var hasAccessToAllOrders = userRoles.Any(role =>
            role.Equals("Manager", StringComparison.OrdinalIgnoreCase) ||
            role.Equals("Procurement", StringComparison.OrdinalIgnoreCase) ||
            role.Equals("Admin", StringComparison.OrdinalIgnoreCase));

        // Employees can only access their own purchase orders
        if (!hasAccessToAllOrders && userRoles.Contains("Employee"))
        {
            if (purchaseOrder.CreatedBy != userId)
            {
                throw new UnauthorizedAccessException($"User {userId} does not have access to purchase order {id}");
            }
        }

        return _mapper.Map<PurchaseOrderDto>(purchaseOrder);
    }

    public async Task<PurchaseOrderDto> CreatePurchaseOrderAsync(
        CreatePurchaseOrderRequest request,
        string createdBy,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating purchase order for supplier {SupplierID} by {CreatedBy}",
            request.SupplierID, createdBy);

        try
        {
            // Validate request
            var validationResult = await ValidatePurchaseOrderAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                throw new InvalidOperationException($"Validation failed: {string.Join(", ", validationResult.Errors.Select(e => e.Message))}");
            }

            // Generate PO number
            var poNumber = await GenerateOrderNumberAsync(cancellationToken);

            // Get currency information for caching (default to THB for now)
            // TODO: Implement proper currency lookup by ID
            var currencyCode = "THB"; // Default currency
            var currencySymbol = "฿"; // Default symbol

            // Get supplier information for caching
            var supplierDto = await _supplierService.GetSupplierAsync(request.SupplierID, cancellationToken);

            // Create purchase order entity
            var purchaseOrder = new PurchaseOrder
            {
                OrderNumber = poNumber,
                SupplierID = request.SupplierID,
                OrderID = request.OrderID,
                CurrencyID = request.CurrencyID,
                CustomerPO = request.CustomerPO,
                OrderType = request.OrderType,
                Notes = request.Notes,
                SubtotalAmount = 0m, // Will be calculated from order items
                TotalAmount = 0m, // Will be calculated from order items
                Status = OrderStatus.Pending,
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow,
                OrderDate = DateTime.UtcNow,
                ExpectedDeliveryDate = request.ExpectedDeliveryDate,
                WHTRate = request.WhtRate, // Nullable - allow null for no WHT scenario
                WHTAmount = null, // Initialize as null for PostgreSQL compatibility
                CurrencyCode = currencyCode,
                CurrencySymbol = currencySymbol,
                Currency = currencyCode,
                SupplierName = supplierDto?.Name ?? "",
                SupplierContactInfo = supplierDto?.ContactInfo ?? ""
            };

            _context.PurchaseOrders.Add(purchaseOrder);
            await _context.SaveChangesAsync(cancellationToken);

            // Load order items from external service if OrderId is provided
            if (request.OrderID > 0)
            {
                await LoadOrderItemsForEntityAsync(purchaseOrder, request.OrderID, cancellationToken);
            }

            // Calculate WHT if applicable and save changes
            await CalculateAndUpdateWHTForEntityAsync(purchaseOrder, cancellationToken, true);

            // Create audit log
            await CreateAuditLogAsync(purchaseOrder.Id, AuditAction.Create, "Purchase order created", createdBy, cancellationToken);

            // Publish domain event
            await _domainEventService.PublishEventAsync(new DomainEventDto
            {
                AggregateType = "PurchaseOrder",
                AggregateId = purchaseOrder.Id.ToString(),
                EventType = "PurchaseOrderCreated",
                EventData = JsonSerializer.Serialize(new { PurchaseOrderId = purchaseOrder.Id, OrderNumber = poNumber }),
                OccurredAt = DateTime.UtcNow,
                UserId = createdBy
            }, cancellationToken);

            // Reload with relationships
            var createdPurchaseOrder = await _context.PurchaseOrders
                .Include(po => po.OrderItems)
                .Include(po => po.ShippingAddress)
            .Include(po => po.BillingAddress)
                .FirstAsync(po => po.Id == purchaseOrder.Id, cancellationToken);

            _logger.LogInformation("Purchase order created successfully with ID {PurchaseOrderId} and PO number {OrderNumber}",
                createdPurchaseOrder.Id, createdPurchaseOrder.OrderNumber);

            return _mapper.Map<PurchaseOrderDto>(createdPurchaseOrder);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating purchase order");
            throw;
        }
    }

    public async Task<PurchaseOrderDto?> UpdatePurchaseOrderAsync(
        int id,
        UpdatePurchaseOrderRequest request,
        string lastModifiedBy,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating purchase order {PurchaseOrderId} by {UpdatedBy}", id, lastModifiedBy);

        try
        {
            var purchaseOrder = await _context.PurchaseOrders
                .Include(po => po.OrderItems)
                .Include(po => po.ShippingAddress)
                .Include(po => po.BillingAddress)
                .FirstOrDefaultAsync(po => po.Id == id && !po.IsDeleted, cancellationToken);

            if (purchaseOrder == null)
            {
                return null;
            }

            // Check if order can be updated (business rules)
            if (purchaseOrder.Status == OrderStatus.Cancelled)
            {
                throw new InvalidOperationException("Cannot update a canceled purchase order");
            }

            // Store original values for audit
            var originalData = JsonSerializer.Serialize(_mapper.Map<PurchaseOrderDto>(purchaseOrder));

            // Update allowed fields
            var hasChanges = false;

            if (!string.IsNullOrWhiteSpace(request.Notes) && request.Notes != purchaseOrder.Notes)
            {
                purchaseOrder.Notes = request.Notes;
                hasChanges = true;
            }

            if (!string.IsNullOrWhiteSpace(request.CustomerPO) && request.CustomerPO != purchaseOrder.CustomerPO)
            {
                purchaseOrder.CustomerPO = request.CustomerPO;
                hasChanges = true;
            }

            // SubtotalAmount is calculated from order items, not directly updated

            // SupplierID and OrderType are not updatable after creation

            if (request.CurrencyID.HasValue && request.CurrencyID.Value != purchaseOrder.CurrencyID)
            {
                purchaseOrder.CurrencyID = request.CurrencyID.Value;
                hasChanges = true;
            }

            if (!hasChanges)
            {
                _logger.LogInformation("No changes detected for purchase order {PurchaseOrderId}", id);
                return _mapper.Map<PurchaseOrderDto>(purchaseOrder);
            }

            // Set update metadata
            purchaseOrder.UpdatedBy = lastModifiedBy;
            purchaseOrder.UpdatedAt = DateTime.UtcNow;

            // Update currency information if changed
            if (request.CurrencyID.HasValue && request.CurrencyID.Value != purchaseOrder.CurrencyID)
            {
                // TODO: Implement proper currency lookup by ID
                purchaseOrder.CurrencyID = request.CurrencyID.Value;
                // For now, keep existing currency info or use defaults
                hasChanges = true;
            }

            // Update WHT rate if provided
            if (request.WhtRate.HasValue && request.WhtRate.Value != purchaseOrder.WHTRate)
            {
                purchaseOrder.WHTRate = request.WhtRate.Value;
                hasChanges = true;
            }

            // Update expected delivery date if provided
            if (request.ExpectedDeliveryDate.HasValue && request.ExpectedDeliveryDate.Value != purchaseOrder.ExpectedDeliveryDate)
            {
                purchaseOrder.ExpectedDeliveryDate = request.ExpectedDeliveryDate.Value;
                hasChanges = true;
            }

            // Recalculate WHT if currency or WHT rate changed
            if (request.CurrencyID.HasValue || request.WhtRate.HasValue)
            {
                await CalculateAndUpdateWHTAsync(id, cancellationToken);
            }

            await _context.SaveChangesAsync(cancellationToken);

            // Create audit log
            await CreateAuditLogAsync(id, AuditAction.Update, "Purchase order updated", lastModifiedBy, cancellationToken, originalData);

            // Publish domain event
            await _domainEventService.PublishEventAsync(new DomainEventDto
            {
                AggregateType = "PurchaseOrder",
                AggregateId = id.ToString(),
                EventType = "PurchaseOrderUpdated",
                EventData = JsonSerializer.Serialize(new { PurchaseOrderId = id, UpdatedBy = lastModifiedBy }),
                OccurredAt = DateTime.UtcNow,
                UserId = lastModifiedBy
            }, cancellationToken);

            _logger.LogInformation("Purchase order {PurchaseOrderId} updated successfully", id);

            return _mapper.Map<PurchaseOrderDto>(purchaseOrder);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating purchase order {PurchaseOrderId}", id);
            throw;
        }
    }

    public async Task<bool> DeletePurchaseOrderAsync(
        int id,
        string deletedBy,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting purchase order {PurchaseOrderId} by {DeletedBy}", id, deletedBy);

        try
        {
            var purchaseOrder = await _context.PurchaseOrders
                .FirstOrDefaultAsync(po => po.Id == id && !po.IsDeleted, cancellationToken);

            if (purchaseOrder == null)
            {
                return false;
            }

            // Check if order can be deleted (business rules)
            if (purchaseOrder.Status == OrderStatus.Approved)
            {
                throw new InvalidOperationException("Cannot delete an approved purchase order. Cancel it first.");
            }

            // Soft delete
            purchaseOrder.IsDeleted = true;
            // DeletedBy and DeletedAt properties not available in PurchaseOrder entity
            // Soft delete handled by IsDeleted flag
            purchaseOrder.UpdatedBy = deletedBy;
            purchaseOrder.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            // Create audit log
            await CreateAuditLogAsync(id, AuditAction.Delete, "Purchase order deleted", deletedBy, cancellationToken);

            _logger.LogInformation("Purchase order {PurchaseOrderId} deleted successfully", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting purchase order {PurchaseOrderId}", id);
            throw;
        }
    }

    // Additional methods will be added in the next part due to length constraints
    private IQueryable<PurchaseOrder> ApplyFilters(IQueryable<PurchaseOrder> query, SearchPurchaseOrdersRequest request)
    {
        if (!request.IncludeDeleted)
        {
            query = query.Where(po => !po.IsDeleted);
        }

        if (request.SupplierId.HasValue)
        {
            query = query.Where(po => po.SupplierID == request.SupplierId.Value);
        }

        if (request.OrderId.HasValue)
        {
            query = query.Where(po => po.OrderID == request.OrderId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.CurrencyCode))
        {
            query = query.Where(po => po.CurrencyCode == request.CurrencyCode);
        }

        if (!string.IsNullOrWhiteSpace(request.CustomerPoNumber))
        {
            query = query.Where(po => po.CustomerPO == request.CustomerPoNumber);
        }

        if (request.Status.HasValue)
        {
            query = query.Where(po => po.Status == request.Status.Value);
        }

        if (request.OrderType.HasValue)
        {
            query = query.Where(po => po.OrderType == request.OrderType.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.CreatedBy))
        {
            query = query.Where(po => po.CreatedBy == request.CreatedBy);
        }

        if (request.MinAmount.HasValue)
        {
            query = query.Where(po => po.TotalAmount >= request.MinAmount.Value);
        }

        if (request.MaxAmount.HasValue)
        {
            query = query.Where(po => po.TotalAmount <= request.MaxAmount.Value);
        }

        if (request.CreatedFrom.HasValue)
        {
            query = query.Where(po => po.CreatedAt >= request.CreatedFrom.Value);
        }

        if (request.CreatedTo.HasValue)
        {
            query = query.Where(po => po.CreatedAt <= request.CreatedTo.Value);
        }

        if (request.UpdatedFrom.HasValue)
        {
            query = query.Where(po => po.UpdatedAt >= request.UpdatedFrom.Value);
        }

        if (request.UpdatedTo.HasValue)
        {
            query = query.Where(po => po.UpdatedAt <= request.UpdatedTo.Value);
        }

        if (request.ExpectedDeliveryFrom.HasValue)
        {
            query = query.Where(po => po.ExpectedDeliveryDate >= request.ExpectedDeliveryFrom.Value);
        }

        if (request.ExpectedDeliveryTo.HasValue)
        {
            query = query.Where(po => po.ExpectedDeliveryDate <= request.ExpectedDeliveryTo.Value);
        }

        return query;
    }

    private static IQueryable<PurchaseOrder> ApplySorting(IQueryable<PurchaseOrder> query, PurchaseOrderSortType sortBy, string sortDirection)
    {
        var isDescending = sortDirection.Equals("desc", StringComparison.OrdinalIgnoreCase);

        return sortBy switch
        {
            PurchaseOrderSortType.OrderNumber => isDescending ? query.OrderByDescending(po => po.OrderNumber) : query.OrderBy(po => po.OrderNumber),
            PurchaseOrderSortType.CreatedAt => isDescending ? query.OrderByDescending(po => po.CreatedAt) : query.OrderBy(po => po.CreatedAt),
            PurchaseOrderSortType.CreatedAtDesc => query.OrderByDescending(po => po.CreatedAt), // Always descending for CreatedAtDesc
            PurchaseOrderSortType.TotalAmount => isDescending ? query.OrderByDescending(po => po.TotalAmount) : query.OrderBy(po => po.TotalAmount),
            PurchaseOrderSortType.Status => isDescending ? query.OrderByDescending(po => po.Status) : query.OrderBy(po => po.Status),
            PurchaseOrderSortType.SupplierId => isDescending ? query.OrderByDescending(po => po.SupplierID) : query.OrderBy(po => po.SupplierID),
            _ => query.OrderByDescending(po => po.CreatedAt)
        };
    }

    private async Task<string> GenerateOrderNumberAsync(CancellationToken cancellationToken)
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"PO{year}";

        var lastPO = await _context.PurchaseOrders
            .Where(po => po.OrderNumber.StartsWith(prefix))
            .OrderByDescending(po => po.OrderNumber)
            .FirstOrDefaultAsync(cancellationToken);

        var sequence = 1;
        if (lastPO != null && int.TryParse(lastPO.OrderNumber.Substring(prefix.Length), out var lastSequence))
        {
            sequence = lastSequence + 1;
        }

        return $"{prefix}{sequence:D6}"; // e.g., PO2025000001
    }

    private async Task LoadOrderItemsAsync(int purchaseOrderId, int orderId, CancellationToken cancellationToken)
    {
        try
        {
            // Get purchase order to access currency
            var purchaseOrder = await _context.PurchaseOrders
                .FirstOrDefaultAsync(po => po.Id == purchaseOrderId, cancellationToken);

            if (purchaseOrder == null)
            {
                _logger.LogWarning("Purchase order {PurchaseOrderId} not found for loading order items", purchaseOrderId);
                return;
            }

            await LoadOrderItemsForEntityAsync(purchaseOrder, orderId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load order items for order {OrderId}", orderId);
        }
    }

    private async Task LoadOrderItemsForEntityAsync(PurchaseOrder purchaseOrder, int orderId, CancellationToken cancellationToken)
    {
        try
        {
            var orderItemsDto = await _orderService.GetOrderItemsAsync(orderId, cancellationToken);

            if (orderItemsDto != null)
            {
                var orderItems = orderItemsDto.Select(item => new OrderItem
                {
                    PurchaseOrderId = purchaseOrder.Id,
                    ExternalOrderItemId = item.Id,
                    ProductCode = item.ProductCode,
                    ProductName = item.ProductName,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    TotalPrice = item.TotalPrice,
                    UnitOfMeasure = item.UnitOfMeasure,
                    Currency = purchaseOrder.Currency,
                    CachedAt = DateTime.UtcNow
                }).ToList();

                _context.OrderItems.AddRange(orderItems);
                await _context.SaveChangesAsync(cancellationToken);

                // Update subtotal amount based on order items
                purchaseOrder.SubtotalAmount = orderItems.Sum(oi => oi.TotalPrice);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load order items for order {OrderId}", orderId);
        }
    }

    private async Task CalculateAndUpdateWHTAsync(int purchaseOrderId, CancellationToken cancellationToken)
    {
        // Find the tracked entity or load it without tracking to avoid conflicts
        var purchaseOrder = _context.PurchaseOrders.Local.FirstOrDefault(po => po.Id == purchaseOrderId);
        if (purchaseOrder == null)
        {
            purchaseOrder = await _context.PurchaseOrders.FirstAsync(po => po.Id == purchaseOrderId, cancellationToken);
        }

        await CalculateAndUpdateWHTForEntityAsync(purchaseOrder, cancellationToken, true);
    }

    private async Task CalculateAndUpdateWHTForEntityAsync(PurchaseOrder purchaseOrder, CancellationToken cancellationToken, bool saveChanges = true)
    {
        try
        {
            // Get supplier data for WHT calculation
            var supplierDto = await _supplierService.GetSupplierAsync(purchaseOrder.SupplierID, cancellationToken);

            if (supplierDto != null)
            {
                var whtResult = await _whtService.CalculateWHTAsync(
                    supplierDto,
                    purchaseOrder.SubtotalAmount,
                    purchaseOrder.CurrencyCode,
                    cancellationToken);

                // Ensure proper nullable decimal assignment for PostgreSQL compatibility
                purchaseOrder.WHTAmount = whtResult.WHTAmount;
                purchaseOrder.WHTRate = whtResult.WHTRate;
                purchaseOrder.TotalAmount = whtResult.NetAmount;
            }
            else
            {
                // Set to null for no WHT scenario
                purchaseOrder.WHTAmount = null;
                purchaseOrder.WHTRate = null;
                purchaseOrder.TotalAmount = purchaseOrder.SubtotalAmount;
            }

            // Only save changes if requested and we have changes
            if (saveChanges && _context.ChangeTracker.HasChanges())
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to calculate WHT for purchase order {PurchaseOrderId}", purchaseOrder.Id);

            // Fallback to no WHT - use null for nullable fields
            purchaseOrder.WHTAmount = null;
            purchaseOrder.WHTRate = null;
            purchaseOrder.TotalAmount = purchaseOrder.SubtotalAmount;

            // Only save changes if requested and we have changes
            if (saveChanges && _context.ChangeTracker.HasChanges())
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
        }
    }

    private async Task CreateAuditLogAsync(int purchaseOrderId, AuditAction action, string description, string userId, CancellationToken cancellationToken, string? originalData = null)
    {
        var auditLog = new AuditLog
        {
            EntityType = "PurchaseOrder",
            EntityId = purchaseOrderId.ToString(),
            Action = action,
            ChangeReason = description,
            UserId = userId,
            UserRole = "Unknown", // TODO: Get actual user role from claims
            Timestamp = DateTime.UtcNow,
            OldValues = originalData
        };

        _context.AuditLogs.Add(auditLog);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<PurchaseOrderDto?> ApprovePurchaseOrderAsync(
        int id,
        ApprovePurchaseOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Approving purchase order {PurchaseOrderId} by {ApprovedBy}", id, request.ApprovedBy);

        try
        {
            var purchaseOrder = await _context.PurchaseOrders
                .Include(po => po.OrderItems)
                .Include(po => po.ShippingAddress)
                .Include(po => po.BillingAddress)
                .FirstOrDefaultAsync(po => po.Id == id && !po.IsDeleted, cancellationToken);

            if (purchaseOrder == null)
            {
                return null;
            }

            // Check if order can be approved
            if (purchaseOrder.Status != OrderStatus.Pending && purchaseOrder.Status != OrderStatus.Pending)
            {
                throw new InvalidOperationException($"Cannot approve purchase order with status {purchaseOrder.Status}");
            }

            // Update status and approval information
            purchaseOrder.Status = OrderStatus.Approved;
            purchaseOrder.ApprovedBy = request.ApprovedBy;
            purchaseOrder.ApprovedAt = request.ApprovedAt ?? DateTime.UtcNow;
            purchaseOrder.UpdatedBy = request.ApprovedBy;
            purchaseOrder.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            // Create audit log
            await CreateAuditLogAsync(id, AuditAction.Approve,
                $"Purchase order approved by {request.ApprovedBy}. Comments: {request.Comments}",
                request.ApprovedBy, cancellationToken);

            // Publish domain event
            await _domainEventService.PublishEventAsync(new DomainEventDto
            {
                AggregateType = "PurchaseOrder",
                AggregateId = id.ToString(),
                EventType = "PurchaseOrderApproved",
                EventData = JsonSerializer.Serialize(new {
                    PurchaseOrderId = id,
                    ApprovedBy = request.ApprovedBy,
                    ApprovedAt = purchaseOrder.ApprovedAt
                }),
                OccurredAt = DateTime.UtcNow,
                UserId = request.ApprovedBy
            }, cancellationToken);

            // Generate PDF if requested and applicable
            if (request.GeneratePdf && _pdfService.IsPdfGenerationApplicable(_mapper.Map<PurchaseOrderDto>(purchaseOrder)))
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _pdfService.GeneratePurchaseOrderPdfAsync(id, CancellationToken.None);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to generate PDF for approved purchase order {PurchaseOrderId}", id);
                    }
                }, cancellationToken);
            }

            _logger.LogInformation("Purchase order {PurchaseOrderId} approved successfully", id);
            return _mapper.Map<PurchaseOrderDto>(purchaseOrder);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving purchase order {PurchaseOrderId}", id);
            throw;
        }
    }

    public async Task<PurchaseOrderDto?> CancelPurchaseOrderAsync(
        int id,
        CancelPurchaseOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Canceling purchase order {PurchaseOrderId} by {CanceledBy}", id, request.CanceledBy);

        try
        {
            var purchaseOrder = await _context.PurchaseOrders
                .Include(po => po.OrderItems)
                .Include(po => po.ShippingAddress)
                .Include(po => po.BillingAddress)
                .FirstOrDefaultAsync(po => po.Id == id && !po.IsDeleted, cancellationToken);

            if (purchaseOrder == null)
            {
                return null;
            }

            // Check if order can be canceled
            if (purchaseOrder.Status == OrderStatus.Cancelled)
            {
                throw new InvalidOperationException("Purchase order is already canceled");
            }

            // Update status and cancellation information
            purchaseOrder.Status = OrderStatus.Cancelled;
            purchaseOrder.UpdatedBy = request.CanceledBy;
            purchaseOrder.UpdatedAt = DateTime.UtcNow;

            // Store cancellation details in Notes field
            var cancellationNote = $"Cancelled by {request.CanceledBy} at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC";
            if (!string.IsNullOrWhiteSpace(request.Reason))
            {
                cancellationNote += $". Reason: {request.Reason}";
            }

            if (string.IsNullOrWhiteSpace(purchaseOrder.Notes))
            {
                purchaseOrder.Notes = cancellationNote;
            }
            else
            {
                purchaseOrder.Notes += $"\n{cancellationNote}";
            }

            await _context.SaveChangesAsync(cancellationToken);

            // Create audit log
            await CreateAuditLogAsync(id, AuditAction.Cancel,
                $"Purchase order canceled by {request.CanceledBy}. Reason: {request.Reason}. Comments: {request.Comments}",
                request.CanceledBy, cancellationToken);

            // Publish domain event
            await _domainEventService.PublishEventAsync(new DomainEventDto
            {
                AggregateType = "PurchaseOrder",
                AggregateId = id.ToString(),
                EventType = "PurchaseOrderCanceled",
                EventData = JsonSerializer.Serialize(new {
                    PurchaseOrderId = id,
                    CanceledBy = request.CanceledBy,
                    CanceledAt = purchaseOrder.UpdatedAt,
                    Reason = request.Reason
                }),
                OccurredAt = DateTime.UtcNow,
                UserId = request.CanceledBy
            }, cancellationToken);

            _logger.LogInformation("Purchase order {PurchaseOrderId} canceled successfully", id);
            return _mapper.Map<PurchaseOrderDto>(purchaseOrder);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error canceling purchase order {PurchaseOrderId}", id);
            throw;
        }
    }

    public async Task<PurchaseOrderStatsDto> GetPurchaseOrderStatsAsync(
        string? userId = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting purchase order statistics for user {UserId}", userId ?? "all");

        try
        {
            var query = _context.PurchaseOrders.Where(po => !po.IsDeleted);

            if (!string.IsNullOrEmpty(userId))
            {
                query = query.Where(po => po.CreatedBy == userId);
            }

            var stats = new PurchaseOrderStatsDto
            {
                GeneratedAt = DateTime.UtcNow,
                UserId = userId
            };

            // Basic counts
            stats.TotalCount = await query.CountAsync(cancellationToken);
            stats.DraftCount = await query.CountAsync(po => po.Status == OrderStatus.Pending, cancellationToken);
            stats.PendingApprovalCount = await query.CountAsync(po => po.Status == OrderStatus.Pending, cancellationToken);
            stats.ApprovedCount = await query.CountAsync(po => po.Status == OrderStatus.Approved, cancellationToken);
            stats.CanceledCount = await query.CountAsync(po => po.Status == OrderStatus.Cancelled, cancellationToken);

            // Value calculations
            stats.TotalValue = await query.SumAsync(po => po.TotalAmount, cancellationToken);
            stats.ApprovedValue = await query.Where(po => po.Status == OrderStatus.Approved).SumAsync(po => po.TotalAmount, cancellationToken);
            stats.TotalWHTAmount = await query.SumAsync(po => po.WHTAmount ?? 0, cancellationToken);

            // Type counts
            stats.InternalCount = await query.CountAsync(po => po.OrderType == OrderType.Internal, cancellationToken);
            stats.ExternalCount = await query.CountAsync(po => po.OrderType == OrderType.External, cancellationToken);

            // Monthly statistics (last 12 months)
            var twelveMonthsAgo = DateTime.UtcNow.AddMonths(-12);
            var monthlyData = await query
                .Where(po => po.CreatedAt >= twelveMonthsAgo)
                .GroupBy(po => new { po.CreatedAt.Year, po.CreatedAt.Month })
                .Select(g => new MonthlyStatsDto
                {
                    Month = $"{g.Key.Year:D4}-{g.Key.Month:D2}",
                    Count = g.Count(),
                    TotalValue = g.Sum(po => po.TotalAmount),
                    AverageValue = g.Average(po => po.TotalAmount)
                })
                .OrderBy(m => m.Month)
                .ToListAsync(cancellationToken);

            stats.MonthlyStats = monthlyData;

            // Currency statistics
            var currencyData = await query
                .GroupBy(po => po.CurrencyCode)
                .Select(g => new CurrencyStatsDto
                {
                    CurrencyCode = g.Key,
                    Count = g.Count(),
                    TotalValue = g.Sum(po => po.TotalAmount),
                    Percentage = (decimal)g.Count() / stats.TotalCount * 100
                })
                .OrderByDescending(c => c.Count)
                .ToListAsync(cancellationToken);

            stats.CurrencyStats = currencyData;

            // Top suppliers (top 10)
            var supplierData = await query
                .Where(po => po.SupplierID > 0)
                .GroupBy(po => po.SupplierID)
                .Select(g => new SupplierStatsDto
                {
                    SupplierId = g.Key,
                    Count = g.Count(),
                    TotalValue = g.Sum(po => po.TotalAmount),
                    AverageValue = g.Average(po => po.TotalAmount),
                    Percentage = (decimal)g.Count() / stats.TotalCount * 100
                })
                .OrderByDescending(s => s.TotalValue)
                .Take(10)
                .ToListAsync(cancellationToken);

            stats.TopSuppliers = supplierData;

            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting purchase order statistics");
            throw;
        }
    }

    public async Task<ValidationResult> ValidatePurchaseOrderAsync(
        CreatePurchaseOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new ValidationResult { ValidatedAt = DateTime.UtcNow };

        try
        {
            // CreatedBy is set from authentication context in the controller, no validation needed here

            if (request.CurrencyID <= 0)
            {
                result.Errors.Add(new ValidationError
                {
                    Field = nameof(request.CurrencyID),
                    Message = "CurrencyID is required",
                    Code = "REQUIRED_FIELD"
                });
            }
            else
            {
                // Validate currency exists and is supported
                try
                {
                    // For now, we'll use a basic currency code mapping since we don't have the actual currency lookup by ID
                    // TODO: Implement proper currency lookup by ID from CurrencyService
                    var supportedCurrencies = await _currencyService.GetSupportedCurrenciesAsync(cancellationToken);
                    if (supportedCurrencies == null || !supportedCurrencies.Any())
                    {
                        result.Warnings.Add(new ValidationWarning
                        {
                            Field = nameof(request.CurrencyID),
                            Message = "Could not retrieve supported currencies for validation",
                            Code = "CURRENCY_SERVICE_NO_DATA"
                        });
                    }
                    else
                    {
                        // Basic validation - for now just check if we have any supported currencies
                        // TODO: Map CurrencyID to actual currency codes for proper validation
                        if (request.CurrencyID > 1000) // Arbitrary limit for demo
                        {
                            result.Errors.Add(new ValidationError
                            {
                                Field = nameof(request.CurrencyID),
                                Message = $"Currency with ID {request.CurrencyID} is not supported",
                                Code = "INVALID_CURRENCY",
                                Value = request.CurrencyID
                            });
                        }
                    }
                }
                catch (Exception)
                {
                    result.Warnings.Add(new ValidationWarning
                    {
                        Field = nameof(request.CurrencyID),
                        Message = "Could not validate currency due to service unavailability",
                        Code = "CURRENCY_SERVICE_UNAVAILABLE"
                    });
                }
            }

            // SubtotalAmount validation removed - will be calculated from order items

            // Validate supplier exists
            if (request.SupplierID > 0)
            {
                try
                {
                    var supplier = await _supplierService.GetSupplierAsync(request.SupplierID, cancellationToken);
                    if (supplier == null)
                    {
                        result.Errors.Add(new ValidationError
                        {
                            Field = nameof(request.SupplierID),
                            Message = $"Supplier with ID {request.SupplierID} not found",
                            Code = "SUPPLIER_NOT_FOUND",
                            Value = request.SupplierID
                        });
                    }
                }
                catch (Exception)
                {
                    result.Warnings.Add(new ValidationWarning
                    {
                        Field = nameof(request.SupplierID),
                        Message = "Could not validate supplier existence due to service unavailability",
                        Code = "SUPPLIER_SERVICE_UNAVAILABLE"
                    });
                }
            }

            // Validate order exists if provided
            if (request.OrderID > 0)
            {
                try
                {
                    var orderValid = await _orderService.ValidateOrderForPurchaseOrderAsync(request.OrderID, cancellationToken);
                    if (!orderValid)
                    {
                        result.Errors.Add(new ValidationError
                        {
                            Field = nameof(request.OrderID),
                            Message = $"Order with ID {request.OrderID} not found",
                            Code = "ORDER_NOT_FOUND",
                            Value = request.OrderID
                        });
                    }
                }
                catch (Exception)
                {
                    result.Warnings.Add(new ValidationWarning
                    {
                        Field = nameof(request.OrderID),
                        Message = "Could not validate order existence due to service unavailability",
                        Code = "ORDER_SERVICE_UNAVAILABLE"
                    });
                }
            }

            // Currency validation will be performed using CurrencyID

            // Amount validation will be performed after order items are added

            // Check for duplicate customer PO number
            if (!string.IsNullOrWhiteSpace(request.CustomerPO))
            {
                var existingPO = await _context.PurchaseOrders
                    .Where(po => po.CustomerPO == request.CustomerPO && !po.IsDeleted)
                    .FirstOrDefaultAsync(cancellationToken);

                if (existingPO != null)
                {
                    result.Warnings.Add(new ValidationWarning
                    {
                        Field = nameof(request.CustomerPO),
                        Message = $"Another purchase order with customer PO number {request.CustomerPO} already exists",
                        Code = "DUPLICATE_CUSTOMER_PO",
                        Value = request.CustomerPO
                    });
                }
            }

            result.IsValid = result.Errors.Count == 0;
            stopwatch.Stop();
            result.ValidationTime = stopwatch.Elapsed;

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during purchase order validation");
            throw;
        }
    }

    public async Task<IEnumerable<PurchaseOrderDto>> GetPurchaseOrdersByCustomerPoAsync(
        string customerPoNumber,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting purchase orders by customer PO number {CustomerPoNumber}", customerPoNumber);

        var purchaseOrders = await _context.PurchaseOrders
            .Include(po => po.OrderItems)
            .Include(po => po.ShippingAddress)
            .Include(po => po.BillingAddress)
            .Where(po => po.CustomerPO == customerPoNumber && !po.IsDeleted)
            .OrderByDescending(po => po.CreatedAt)
            .ToListAsync(cancellationToken);

        return _mapper.Map<IEnumerable<PurchaseOrderDto>>(purchaseOrders);
    }

    public async Task<PurchaseOrderDto?> RecalculateWHTAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Recalculating WHT for purchase order {PurchaseOrderId}", id);

        try
        {
            var purchaseOrder = await _context.PurchaseOrders
                .Include(po => po.OrderItems)
                .Include(po => po.ShippingAddress)
                .Include(po => po.BillingAddress)
                .FirstOrDefaultAsync(po => po.Id == id && !po.IsDeleted, cancellationToken);

            if (purchaseOrder == null)
            {
                return null;
            }

            var originalWHTAmount = purchaseOrder.WHTAmount;
            var originalTotalAmount = purchaseOrder.TotalAmount;

            // Recalculate WHT
            await CalculateAndUpdateWHTAsync(id, cancellationToken);

            // Reload to get updated values
            await _context.Entry(purchaseOrder).ReloadAsync(cancellationToken);

            // Create audit log if values changed
            if (originalWHTAmount != purchaseOrder.WHTAmount || originalTotalAmount != purchaseOrder.TotalAmount)
            {
                await CreateAuditLogAsync(id, AuditAction.Update,
                    $"WHT recalculated. WHT: {originalWHTAmount} → {purchaseOrder.WHTAmount}, Total: {originalTotalAmount} → {purchaseOrder.TotalAmount}",
                    "system-wht-recalculation", cancellationToken);
            }

            _logger.LogInformation("WHT recalculated for purchase order {PurchaseOrderId}", id);
            return _mapper.Map<PurchaseOrderDto>(purchaseOrder);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recalculating WHT for purchase order {PurchaseOrderId}", id);
            throw;
        }
    }

    public async Task<IEnumerable<AuditLogDto>> GetPurchaseOrderHistoryAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting history for purchase order {PurchaseOrderId}", id);

        var auditLogs = await _context.AuditLogs
            .Where(al => al.EntityType == "PurchaseOrder" && al.EntityId == id.ToString())
            .OrderByDescending(al => al.Timestamp)
            .ToListAsync(cancellationToken);

        return _mapper.Map<IEnumerable<AuditLogDto>>(auditLogs);
    }

    public async Task<WHTCalculationResult?> CalculateWHTAsync(
        int id,
        WHTCalculationRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Calculating WHT for purchase order {PurchaseOrderId}", id);

        try
        {
            var purchaseOrder = await _context.PurchaseOrders
                .Include(po => po.OrderItems)
                .FirstOrDefaultAsync(po => po.Id == id && !po.IsDeleted, cancellationToken);

            if (purchaseOrder == null)
            {
                return null;
            }

            // Validate WHT rate
            if (request.WHTRate < 0)
            {
                throw new ArgumentException("WHT rate cannot be negative");
            }

            if (request.WHTRate > 0.15m)
            {
                throw new ArgumentException("WHT rate cannot exceed 15% as per Thailand tax regulations");
            }

            // Get supplier information
            var supplier = await _supplierService.GetSupplierAsync(request.SupplierID, cancellationToken);
            if (supplier == null)
            {
                throw new ArgumentException($"Supplier {request.SupplierID} not found");
            }

            // Calculate WHT using external service
            var result = await _whtService.CalculateWHTAsync(supplier, request.TotalAmount ?? 0m, request.CurrencyCode, cancellationToken);

            // Store calculation in database if needed
            await CreateAuditLogAsync(id, AuditAction.Create,
                $"WHT calculated: {result.WHTAmount} {request.CurrencyCode} at {result.WHTRate:P} rate",
                "wht-calculation", cancellationToken);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating WHT for purchase order {PurchaseOrderId}", id);
            throw;
        }
    }

    public async Task<List<WHTCalculationResult>?> GetWHTHistoryAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting WHT history for purchase order {PurchaseOrderId}", id);

        try
        {
            var purchaseOrder = await _context.PurchaseOrders
                .FirstOrDefaultAsync(po => po.Id == id && !po.IsDeleted, cancellationToken);

            if (purchaseOrder == null)
            {
                return null;
            }

            // Get WHT calculation history from audit logs
            var whtAuditLogs = await _context.AuditLogs
                .Where(al => al.EntityType == "PurchaseOrder" &&
                           al.EntityId == id.ToString() &&
                           al.ChangeReason != null && al.ChangeReason.Contains("wht"))
                .OrderByDescending(al => al.Timestamp)
                .ToListAsync(cancellationToken);

            // For now, return empty list as we would need to parse audit logs or implement separate WHT history table
            // This is a placeholder implementation
            return new List<WHTCalculationResult>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting WHT history for purchase order {PurchaseOrderId}", id);
            throw;
        }
    }
}
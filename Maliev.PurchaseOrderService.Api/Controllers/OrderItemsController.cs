using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using Maliev.PurchaseOrderService.Api.DTOs;
using Maliev.PurchaseOrderService.Api.Services;
using Maliev.PurchaseOrderService.Api.Clients;
using Maliev.PurchaseOrderService.Data;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using System.Net;

namespace Maliev.PurchaseOrderService.Api.Controllers;

/// <summary>
/// Order Items API Controller for read-only operations and cache refresh
/// </summary>
[ApiController]
[Route("v{version:apiVersion}/purchase-orders/{purchaseOrderId:int}/orderitems")]
[ApiVersion("1.0")]
[ApiVersion("1")]
[Authorize]
[Produces("application/json")]
public class OrderItemsController : ControllerBase
{
    private readonly PurchaseOrderContext _context;
    private readonly IOrderServiceClient _orderService;
    private readonly IMapper _mapper;
    private readonly ILogger<OrderItemsController> _logger;

    public OrderItemsController(
        PurchaseOrderContext context,
        IOrderServiceClient orderService,
        IMapper mapper,
        ILogger<OrderItemsController> logger)
    {
        _context = context;
        _orderService = orderService;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Gets order items for a purchase order with optional pagination
    /// </summary>
    /// <param name="purchaseOrderId">Purchase order ID</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Items per page</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of order items or paginated response</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<OrderItemDto>), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(PaginatedResponse<OrderItemDto>), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.NotFound)]
    [ProducesResponseType(typeof(ValidationErrorResponse), (int)HttpStatusCode.BadRequest)]
    public async Task<ActionResult> GetOrderItems(
        int purchaseOrderId,
        [FromQuery] int? page = null,
        [FromQuery] int? pageSize = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting order items for purchase order {PurchaseOrderId}", purchaseOrderId);

            // Validate pagination parameters
            if (page.HasValue || pageSize.HasValue)
            {
                if (page.HasValue && page.Value < 1)
                {
                    return BadRequest(new ValidationErrorResponse
                    {
                        Message = "Invalid pagination parameters",
                        Code = "INVALID_PAGINATION",
                        Errors = new List<ValidationError>
                        {
                            new ValidationError
                            {
                                Field = "page",
                                Message = "Page number must be greater than 0",
                                Code = "INVALID_PAGE_NUMBER",
                                Value = page.Value
                            }
                        }
                    });
                }

                if (pageSize.HasValue && pageSize.Value < 1)
                {
                    return BadRequest(new ValidationErrorResponse
                    {
                        Message = "Invalid pagination parameters",
                        Code = "INVALID_PAGINATION",
                        Errors = new List<ValidationError>
                        {
                            new ValidationError
                            {
                                Field = "pageSize",
                                Message = "Page size must be greater than 0",
                                Code = "INVALID_PAGE_SIZE",
                                Value = pageSize.Value
                            }
                        }
                    });
                }
            }

            // Verify purchase order exists
            var purchaseOrderExists = await _context.PurchaseOrders
                .AnyAsync(po => po.Id == purchaseOrderId && !po.IsDeleted, cancellationToken);

            if (!purchaseOrderExists)
            {
                return NotFound(new ErrorResponse
                {
                    Error = new ErrorInfo
                    {
                        Message = $"Purchase order with ID {purchaseOrderId} not found",
                        Code = "PURCHASE_ORDER_NOT_FOUND"
                    }
                });
            }

            var query = _context.OrderItems
                .Where(oi => oi.PurchaseOrderId == purchaseOrderId)
                .OrderBy(oi => oi.ProductName);

            // If pagination is requested
            if (page.HasValue && pageSize.HasValue)
            {
                var totalCount = await query.CountAsync(cancellationToken);
                var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize.Value);

                var orderItems = await query
                    .Skip((page.Value - 1) * pageSize.Value)
                    .Take(pageSize.Value)
                    .ToListAsync(cancellationToken);

                var orderItemDtos = _mapper.Map<IEnumerable<OrderItemDto>>(orderItems);

                var response = new PaginatedResponse<OrderItemDto>
                {
                    Data = orderItemDtos,
                    Page = page.Value,
                    PageSize = pageSize.Value,
                    TotalCount = totalCount,
                    TotalPages = totalPages,
                    HasPreviousPage = page.Value > 1,
                    HasNextPage = page.Value < totalPages
                };

                // Add Cache-Control header for paginated results
                Response.Headers["Cache-Control"] = "private, max-age=300"; // 5 minutes

                return Ok(response);
            }
            else
            {
                // Return all items without pagination
                var orderItems = await query.ToListAsync(cancellationToken);
                var orderItemDtos = _mapper.Map<IEnumerable<OrderItemDto>>(orderItems);

                // Add Cache-Control header for non-paginated results
                Response.Headers["Cache-Control"] = "private, max-age=300"; // 5 minutes

                return Ok(orderItemDtos);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting order items for purchase order {PurchaseOrderId}", purchaseOrderId);
            return StatusCode(500, new ErrorResponse
            {
                Error = new ErrorInfo
                {
                    Message = "An error occurred while retrieving order items",
                    Code = "INTERNAL_ERROR"
                }
            });
        }
    }

    /// <summary>
    /// Gets a specific order item by ID
    /// </summary>
    /// <param name="purchaseOrderId">Purchase order ID</param>
    /// <param name="itemId">Order item ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Order item details</returns>
    [HttpGet("{itemId:int}")]
    [ProducesResponseType(typeof(OrderItemDto), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.NotFound)]
    public async Task<ActionResult<OrderItemDto>> GetOrderItem(
        int purchaseOrderId,
        int itemId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting order item {ItemId} for purchase order {PurchaseOrderId}", itemId, purchaseOrderId);

            var orderItem = await _context.OrderItems
                .FirstOrDefaultAsync(oi => oi.Id == itemId && oi.PurchaseOrderId == purchaseOrderId, cancellationToken);

            if (orderItem == null)
            {
                return NotFound(new ErrorResponse
                {
                    Error = new ErrorInfo
                    {
                        Message = $"Order item with ID {itemId} not found for purchase order {purchaseOrderId}",
                        Code = "ORDER_ITEM_NOT_FOUND"
                    }
                });
            }

            var orderItemDto = _mapper.Map<OrderItemDto>(orderItem);
            return Ok(orderItemDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting order item {ItemId} for purchase order {PurchaseOrderId}", itemId, purchaseOrderId);
            return StatusCode(500, new ErrorResponse
            {
                Error = new ErrorInfo
                {
                    Message = "An error occurred while retrieving the order item",
                    Code = "INTERNAL_ERROR"
                }
            });
        }
    }

    /// <summary>
    /// Refreshes order items from the external OrderService
    /// </summary>
    /// <param name="purchaseOrderId">Purchase order ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated list of order items</returns>
    [HttpPut("refresh")]
    [ProducesResponseType(typeof(OrderItemRefreshResult), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.BadRequest)]
    [Authorize(Roles = "Employee,Manager,Procurement,Admin")]
    public async Task<ActionResult<OrderItemRefreshResult>> RefreshOrderItems(
        int purchaseOrderId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Refreshing order items for purchase order {PurchaseOrderId}", purchaseOrderId);

            // Get purchase order with order ID
            var purchaseOrder = await _context.PurchaseOrders
                .FirstOrDefaultAsync(po => po.Id == purchaseOrderId && !po.IsDeleted, cancellationToken);

            if (purchaseOrder == null)
            {
                return NotFound(new ErrorResponse
                {
                    Error = new ErrorInfo
                    {
                        Message = $"Purchase order with ID {purchaseOrderId} not found",
                        Code = "PURCHASE_ORDER_NOT_FOUND"
                    }
                });
            }

            // Note: OrderID is required, so no null check needed
            if (purchaseOrder.OrderID == 0)
            {
                return BadRequest(new ErrorResponse
                {
                    Error = new ErrorInfo
                    {
                        Message = "Purchase order is not linked to an external order",
                        Code = "NO_EXTERNAL_ORDER"
                    }
                });
            }

            // Get current order items count
            var currentItemsCount = await _context.OrderItems
                .CountAsync(oi => oi.PurchaseOrderId == purchaseOrderId, cancellationToken);

            var result = new OrderItemRefreshResult
            {
                PurchaseOrderId = purchaseOrderId,
                OrderId = purchaseOrder.OrderID,
                RefreshedAt = DateTime.UtcNow,
                RefreshedBy = User.Identity?.Name ?? "unknown",
                PreviousItemCount = currentItemsCount
            };

            try
            {
                // Fetch fresh data from OrderService
                var externalOrderItems = await _orderService.GetOrderItemsAsync(
                    purchaseOrder.OrderID, cancellationToken);

                if (externalOrderItems == null || !externalOrderItems.Any())
                {
                    result.Success = false;
                    result.ErrorMessage = "No order items found in external service";
                    return Ok(result);
                }

                // Remove existing order items
                var existingItems = await _context.OrderItems
                    .Where(oi => oi.PurchaseOrderId == purchaseOrderId)
                    .ToListAsync(cancellationToken);

                _context.OrderItems.RemoveRange(existingItems);

                // Add refreshed order items
                var newOrderItems = externalOrderItems.Select(item => new Data.Entities.OrderItem
                {
                    PurchaseOrderId = purchaseOrderId,
                    ExternalOrderItemId = item.Id,
                    ProductCode = item.ProductCode,
                    ProductName = item.ProductName ?? "Unknown Product",
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    TotalPrice = item.TotalPrice,
                    UnitOfMeasure = item.UnitOfMeasure ?? "each",
                    Currency = purchaseOrder.Currency,
                    CachedAt = DateTime.UtcNow
                }).ToList();

                _context.OrderItems.AddRange(newOrderItems);
                await _context.SaveChangesAsync(cancellationToken);

                // Update result
                result.Success = true;
                result.NewItemCount = newOrderItems.Count;
                result.ItemsAdded = newOrderItems.Count;
                result.ItemsRemoved = existingItems.Count;
                result.OrderItems = _mapper.Map<List<OrderItemDto>>(newOrderItems);

                // Recalculate purchase order totals if items changed
                if (result.NewItemCount != result.PreviousItemCount)
                {
                    var newSubtotal = newOrderItems.Sum(oi => oi.TotalPrice);
                    if (purchaseOrder.SubtotalAmount != newSubtotal)
                    {
                        purchaseOrder.SubtotalAmount = newSubtotal;
                        purchaseOrder.UpdatedAt = DateTime.UtcNow;
                        purchaseOrder.UpdatedBy = result.RefreshedBy;

                        await _context.SaveChangesAsync(cancellationToken);

                        result.SubtotalUpdated = true;
                        result.NewSubtotal = newSubtotal;
                    }
                }

                _logger.LogInformation("Order items refreshed for purchase order {PurchaseOrderId}: {ItemsAdded} added, {ItemsRemoved} removed",
                    purchaseOrderId, result.ItemsAdded, result.ItemsRemoved);

                return Ok(result);
            }
            catch (Exception serviceEx)
            {
                _logger.LogWarning(serviceEx, "Failed to refresh order items from external service for purchase order {PurchaseOrderId}", purchaseOrderId);

                result.Success = false;
                result.ErrorMessage = "Failed to connect to external order service";
                return Ok(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing order items for purchase order {PurchaseOrderId}", purchaseOrderId);
            return StatusCode(500, new ErrorResponse
            {
                Error = new ErrorInfo
                {
                    Message = "An error occurred while refreshing order items",
                    Code = "INTERNAL_ERROR"
                }
            });
        }
    }

    /// <summary>
    /// Gets order items summary for a purchase order
    /// </summary>
    /// <param name="purchaseOrderId">Purchase order ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Order items summary statistics</returns>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(OrderItemsSummaryDto), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.NotFound)]
    public async Task<ActionResult<OrderItemsSummaryDto>> GetOrderItemsSummary(
        int purchaseOrderId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting order items summary for purchase order {PurchaseOrderId}", purchaseOrderId);

            // Verify purchase order exists
            var purchaseOrderExists = await _context.PurchaseOrders
                .AnyAsync(po => po.Id == purchaseOrderId && !po.IsDeleted, cancellationToken);

            if (!purchaseOrderExists)
            {
                return NotFound(new ErrorResponse
                {
                    Error = new ErrorInfo
                    {
                        Message = $"Purchase order with ID {purchaseOrderId} not found",
                        Code = "PURCHASE_ORDER_NOT_FOUND"
                    }
                });
            }

            // Calculate summary statistics
            var itemsQuery = _context.OrderItems.Where(oi => oi.PurchaseOrderId == purchaseOrderId);

            var summary = new OrderItemsSummaryDto
            {
                PurchaseOrderId = purchaseOrderId,
                TotalItems = await itemsQuery.CountAsync(cancellationToken),
                TotalQuantity = await itemsQuery.SumAsync(oi => oi.Quantity, cancellationToken),
                TotalValue = await itemsQuery.SumAsync(oi => oi.TotalPrice, cancellationToken),
                UniqueCategories = 0, // ItemCategory property not available in OrderItem entity
                LastUpdated = await itemsQuery
                    .MaxAsync(oi => (DateTime?)oi.CachedAt, cancellationToken) ?? DateTime.UtcNow
            };

            // Category breakdown not available since ItemCategory property doesn't exist
            summary.CategoryBreakdown = new List<CategorySummaryDto>();

            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting order items summary for purchase order {PurchaseOrderId}", purchaseOrderId);
            return StatusCode(500, new ErrorResponse
            {
                Error = new ErrorInfo
                {
                    Message = "An error occurred while retrieving order items summary",
                    Code = "INTERNAL_ERROR"
                }
            });
        }
    }
}
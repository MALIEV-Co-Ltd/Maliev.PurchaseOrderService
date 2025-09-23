using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using AutoMapper;
using Maliev.PurchaseOrderService.Api.DTOs;
using Maliev.PurchaseOrderService.Api.Services;
using System.Net;

namespace Maliev.PurchaseOrderService.Api.Controllers;

/// <summary>
/// Purchase Orders API Controller with full CRUD operations
/// </summary>
[ApiController]
[Route("v{version:apiVersion}/purchase-orders")]
[ApiVersion("1")]
[Authorize]
[Produces("application/json")]
public class PurchaseOrdersController : ControllerBase
{
    private readonly IPurchaseOrderService _purchaseOrderService;
    private readonly IMapper _mapper;
    private readonly ILogger<PurchaseOrdersController> _logger;

    public PurchaseOrdersController(
        IPurchaseOrderService purchaseOrderService,
        IMapper mapper,
        ILogger<PurchaseOrdersController> logger)
    {
        _purchaseOrderService = purchaseOrderService;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Gets all purchase orders with optional filtering and pagination
    /// </summary>
    /// <param name="request">Search and filter criteria</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of purchase orders</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedPurchaseOrdersResponse), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.BadRequest)]
    public async Task<ActionResult<PaginatedPurchaseOrdersResponse>> GetPurchaseOrders(
        [FromQuery] SearchPurchaseOrdersRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting purchase orders with search criteria");

            // Validate page size limits
            if (request.PageSize <= 0)
            {
                return BadRequest(new ErrorResponse
                {
                    Error = new ErrorInfo
                    {
                        Message = "Page size must be greater than 0",
                        Code = "INVALID_PAGE_SIZE"
                    }
                });
            }

            if (request.PageSize > 100)
            {
                return BadRequest(new ErrorResponse
                {
                    Error = new ErrorInfo
                    {
                        Message = "Page size cannot exceed 100",
                        Code = "INVALID_PAGE_SIZE"
                    }
                });
            }

            var result = await _purchaseOrderService.GetPurchaseOrdersAsync(request, cancellationToken);

            // Convert to expected response format
            var response = new PaginatedPurchaseOrdersResponse
            {
                Items = _mapper.Map<List<PurchaseOrderResponse>>(result.Data),
                Pagination = new PaginationInfo
                {
                    Page = result.Page,
                    PageSize = result.PageSize,
                    TotalCount = result.TotalCount,
                    TotalPages = result.TotalPages,
                    HasNextPage = result.HasNextPage,
                    HasPreviousPage = result.HasPreviousPage
                }
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting purchase orders");
            return StatusCode(500, new ErrorResponse
            {
                Error = new ErrorInfo
                {
                    Message = "An error occurred while retrieving purchase orders",
                    Code = "INTERNAL_ERROR"
                }
            });
        }
    }

    /// <summary>
    /// Gets a purchase order by ID
    /// </summary>
    /// <param name="id">Purchase order ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Purchase order details</returns>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(PurchaseOrderDto), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.NotFound)]
    public async Task<ActionResult<PurchaseOrderDto>> GetPurchaseOrder(
        int id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting purchase order {PurchaseOrderId}", id);

            var purchaseOrder = await _purchaseOrderService.GetPurchaseOrderByIdAsync(id, cancellationToken);

            if (purchaseOrder == null)
            {
                return NotFound(new ErrorResponse
                {
                    Error = new ErrorInfo
                    {
                        Message = $"Purchase order with ID {id} not found",
                        Code = "PURCHASE_ORDER_NOT_FOUND"
                    }
                });
            }

            return Ok(purchaseOrder);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting purchase order {PurchaseOrderId}", id);
            return StatusCode(500, new ErrorResponse
            {
                Error = new ErrorInfo
                {
                    Message = "An error occurred while retrieving the purchase order",
                    Code = "INTERNAL_ERROR"
                }
            });
        }
    }

    /// <summary>
    /// Creates a new purchase order
    /// </summary>
    /// <param name="request">Create purchase order request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created purchase order</returns>
    [HttpPost]
    [ProducesResponseType(typeof(PurchaseOrderDto), (int)HttpStatusCode.Created)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(ValidationErrorResponse), (int)HttpStatusCode.UnprocessableEntity)]
    public async Task<ActionResult<PurchaseOrderDto>> CreatePurchaseOrder(
        [FromBody] CreatePurchaseOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating purchase order for supplier {SupplierID}", request.SupplierID);

            if (!ModelState.IsValid)
            {
                return BadRequest(new ErrorResponse
                {
                    Error = new ErrorInfo
                    {
                        Message = "Invalid request data",
                        Code = "INVALID_REQUEST",
                        Details = ModelState.ToDictionary(
                            kvp => kvp.Key,
                            kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>()
                        ).Select(kvp => new ErrorDetail
                        {
                            Field = kvp.Key,
                            Message = string.Join(", ", kvp.Value)
                        }).ToList()
                    }
                });
            }

            // Get CreatedBy from current user context
            var createdBy = User.Identity?.Name ?? "unknown";

            // Validate business rules
            var validationResult = await _purchaseOrderService.ValidatePurchaseOrderAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                return UnprocessableEntity(new ValidationErrorResponse
                {
                    Message = "Validation failed",
                    Code = "VALIDATION_FAILED",
                    Errors = validationResult.Errors,
                    Warnings = validationResult.Warnings
                });
            }

            var createdPurchaseOrder = await _purchaseOrderService.CreatePurchaseOrderAsync(request, createdBy, cancellationToken);

            return CreatedAtAction(
                nameof(GetPurchaseOrder),
                new { id = createdPurchaseOrder.Id },
                createdPurchaseOrder);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when creating purchase order");
            return BadRequest(new ErrorResponse
            {
                Error = new ErrorInfo
                {
                    Message = ex.Message,
                    Code = "INVALID_OPERATION"
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating purchase order");
            return StatusCode(500, new ErrorResponse
            {
                Error = new ErrorInfo
                {
                    Message = "An error occurred while creating the purchase order",
                    Code = "INTERNAL_ERROR"
                }
            });
        }
    }

    /// <summary>
    /// Updates an existing purchase order
    /// </summary>
    /// <param name="id">Purchase order ID</param>
    /// <param name="request">Update purchase order request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated purchase order</returns>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(PurchaseOrderDto), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.BadRequest)]
    public async Task<ActionResult<PurchaseOrderDto>> UpdatePurchaseOrder(
        int id,
        [FromBody] UpdatePurchaseOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Updating purchase order {PurchaseOrderId}", id);

            if (!ModelState.IsValid)
            {
                return BadRequest(new ErrorResponse
                {
                    Error = new ErrorInfo
                    {
                        Message = "Invalid request data",
                        Code = "INVALID_REQUEST"
                    }
                });
            }

            // Get UpdatedBy from current user context
            var lastModifiedBy = User.Identity?.Name ?? "unknown";

            var updatedPurchaseOrder = await _purchaseOrderService.UpdatePurchaseOrderAsync(id, request, lastModifiedBy, cancellationToken);

            if (updatedPurchaseOrder == null)
            {
                return NotFound(new ErrorResponse
                {
                    Error = new ErrorInfo
                    {
                        Message = $"Purchase order with ID {id} not found",
                        Code = "PURCHASE_ORDER_NOT_FOUND"
                    }
                });
            }

            return Ok(updatedPurchaseOrder);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when updating purchase order {PurchaseOrderId}", id);
            return BadRequest(new ErrorResponse
            {
                Error = new ErrorInfo
                {
                    Message = ex.Message,
                    Code = "INVALID_OPERATION"
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating purchase order {PurchaseOrderId}", id);
            return StatusCode(500, new ErrorResponse
            {
                Error = new ErrorInfo
                {
                    Message = "An error occurred while updating the purchase order",
                    Code = "INTERNAL_ERROR"
                }
            });
        }
    }

    /// <summary>
    /// Deletes a purchase order (soft delete)
    /// </summary>
    /// <param name="id">Purchase order ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content if successful</returns>
    [HttpDelete("{id:int}")]
    [ProducesResponseType((int)HttpStatusCode.NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.BadRequest)]
    public async Task<ActionResult> DeletePurchaseOrder(
        int id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Deleting purchase order {PurchaseOrderId}", id);

            var deletedBy = User.Identity?.Name ?? "unknown";
            var deleted = await _purchaseOrderService.DeletePurchaseOrderAsync(id, deletedBy, cancellationToken);

            if (!deleted)
            {
                return NotFound(new ErrorResponse
                {
                    Error = new ErrorInfo
                    {
                        Message = $"Purchase order with ID {id} not found",
                        Code = "PURCHASE_ORDER_NOT_FOUND"
                    }
                });
            }

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when deleting purchase order {PurchaseOrderId}", id);
            return BadRequest(new ErrorResponse
            {
                Error = new ErrorInfo
                {
                    Message = ex.Message,
                    Code = "INVALID_OPERATION"
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting purchase order {PurchaseOrderId}", id);
            return StatusCode(500, new ErrorResponse
            {
                Error = new ErrorInfo
                {
                    Message = "An error occurred while deleting the purchase order",
                    Code = "INTERNAL_ERROR"
                }
            });
        }
    }

    /// <summary>
    /// Approves a purchase order
    /// </summary>
    /// <param name="id">Purchase order ID</param>
    /// <param name="request">Approval request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Approved purchase order</returns>
    [HttpPost("{id:int}/approve")]
    [ProducesResponseType(typeof(PurchaseOrderDto), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.BadRequest)]
    [Authorize(Roles = "Manager,Procurement,Admin")]
    public async Task<ActionResult<PurchaseOrderDto>> ApprovePurchaseOrder(
        int id,
        [FromBody] ApprovePurchaseOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Approving purchase order {PurchaseOrderId}", id);

            if (!ModelState.IsValid)
            {
                return BadRequest(new ErrorResponse
                {
                    Error = new ErrorInfo
                    {
                        Message = "Invalid request data",
                        Code = "INVALID_REQUEST"
                    }
                });
            }

            // Set ApprovedBy from current user context
            request.ApprovedBy = User.Identity?.Name ?? "unknown";

            var approvedPurchaseOrder = await _purchaseOrderService.ApprovePurchaseOrderAsync(id, request, cancellationToken);

            if (approvedPurchaseOrder == null)
            {
                return NotFound(new ErrorResponse
                {
                    Error = new ErrorInfo
                    {
                        Message = $"Purchase order with ID {id} not found",
                        Code = "PURCHASE_ORDER_NOT_FOUND"
                    }
                });
            }

            return Ok(approvedPurchaseOrder);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when approving purchase order {PurchaseOrderId}", id);
            return BadRequest(new ErrorResponse
            {
                Error = new ErrorInfo
                {
                    Message = ex.Message,
                    Code = "INVALID_OPERATION"
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving purchase order {PurchaseOrderId}", id);
            return StatusCode(500, new ErrorResponse
            {
                Error = new ErrorInfo
                {
                    Message = "An error occurred while approving the purchase order",
                    Code = "INTERNAL_ERROR"
                }
            });
        }
    }

    /// <summary>
    /// Cancels a purchase order
    /// </summary>
    /// <param name="id">Purchase order ID</param>
    /// <param name="request">Cancellation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Canceled purchase order</returns>
    [HttpPost("{id:int}/cancel")]
    [ProducesResponseType(typeof(PurchaseOrderDto), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.BadRequest)]
    public async Task<ActionResult<PurchaseOrderDto>> CancelPurchaseOrder(
        int id,
        [FromBody] CancelPurchaseOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Canceling purchase order {PurchaseOrderId}", id);

            if (!ModelState.IsValid)
            {
                return BadRequest(new ErrorResponse
                {
                    Error = new ErrorInfo
                    {
                        Message = "Invalid request data",
                        Code = "INVALID_REQUEST"
                    }
                });
            }

            // Set CanceledBy from current user context
            request.CanceledBy = User.Identity?.Name ?? "unknown";

            var canceledPurchaseOrder = await _purchaseOrderService.CancelPurchaseOrderAsync(id, request, cancellationToken);

            if (canceledPurchaseOrder == null)
            {
                return NotFound(new ErrorResponse
                {
                    Error = new ErrorInfo
                    {
                        Message = $"Purchase order with ID {id} not found",
                        Code = "PURCHASE_ORDER_NOT_FOUND"
                    }
                });
            }

            return Ok(canceledPurchaseOrder);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when canceling purchase order {PurchaseOrderId}", id);
            return BadRequest(new ErrorResponse
            {
                Error = new ErrorInfo
                {
                    Message = ex.Message,
                    Code = "INVALID_OPERATION"
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error canceling purchase order {PurchaseOrderId}", id);
            return StatusCode(500, new ErrorResponse
            {
                Error = new ErrorInfo
                {
                    Message = "An error occurred while canceling the purchase order",
                    Code = "INTERNAL_ERROR"
                }
            });
        }
    }

    /// <summary>
    /// Gets purchase order statistics
    /// </summary>
    /// <param name="userId">Optional user ID to filter statistics</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Purchase order statistics</returns>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(PurchaseOrderStatsDto), (int)HttpStatusCode.OK)]
    public async Task<ActionResult<PurchaseOrderStatsDto>> GetPurchaseOrderStats(
        [FromQuery] string? userId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting purchase order statistics");

            var stats = await _purchaseOrderService.GetPurchaseOrderStatsAsync(userId, cancellationToken);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting purchase order statistics");
            return StatusCode(500, new ErrorResponse
            {
                Error = new ErrorInfo
                {
                    Message = "An error occurred while retrieving statistics",
                    Code = "INTERNAL_ERROR"
                }
            });
        }
    }

    /// <summary>
    /// Gets purchase orders by customer PO number
    /// </summary>
    /// <param name="customerPoNumber">Customer PO number</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Purchase orders matching the customer PO number</returns>
    [HttpGet("customer-po/{customerPoNumber}")]
    [ProducesResponseType(typeof(IEnumerable<PurchaseOrderDto>), (int)HttpStatusCode.OK)]
    public async Task<ActionResult<IEnumerable<PurchaseOrderDto>>> GetPurchaseOrdersByCustomerPo(
        string customerPoNumber,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting purchase orders by customer PO number {CustomerPoNumber}", customerPoNumber);

            var purchaseOrders = await _purchaseOrderService.GetPurchaseOrdersByCustomerPoAsync(customerPoNumber, cancellationToken);
            return Ok(purchaseOrders);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting purchase orders by customer PO number {CustomerPoNumber}", customerPoNumber);
            return StatusCode(500, new ErrorResponse
            {
                Error = new ErrorInfo
                {
                    Message = "An error occurred while retrieving purchase orders",
                    Code = "INTERNAL_ERROR"
                }
            });
        }
    }

    /// <summary>
    /// Calculates WHT for a purchase order
    /// </summary>
    /// <param name="id">Purchase order ID</param>
    /// <param name="request">WHT calculation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>WHT calculation result</returns>
    [HttpPost("{id:int}/calculate-wht")]
    [ProducesResponseType(typeof(WHTCalculationResult), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.BadRequest)]
    [Authorize(Roles = "Employee,Manager,Procurement,Admin")]
    public async Task<ActionResult<WHTCalculationResult>> CalculateWHT(
        int id,
        [FromBody] WHTCalculationRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Calculating WHT for purchase order {PurchaseOrderId}", id);

            var result = await _purchaseOrderService.CalculateWHTAsync(id, request, cancellationToken);

            if (result == null)
            {
                return NotFound(new ErrorResponse
                {
                    Error = new ErrorInfo
                    {
                        Message = "Purchase order not found",
                        Code = "PURCHASE_ORDER_NOT_FOUND"
                    }
                });
            }

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid WHT calculation request for purchase order {PurchaseOrderId}", id);
            return BadRequest(new ErrorResponse
            {
                Error = new ErrorInfo
                {
                    Message = ex.Message,
                    Code = "INVALID_WHT_REQUEST"
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating WHT for purchase order {PurchaseOrderId}", id);
            return StatusCode(500, new ErrorResponse
            {
                Error = new ErrorInfo
                {
                    Message = "An error occurred while calculating WHT",
                    Code = "INTERNAL_ERROR"
                }
            });
        }
    }

    /// <summary>
    /// Gets WHT calculation history for a purchase order
    /// </summary>
    /// <param name="id">Purchase order ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of WHT calculations</returns>
    [HttpGet("{id:int}/wht-history")]
    [ProducesResponseType(typeof(List<WHTCalculationResult>), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.NotFound)]
    [Authorize(Roles = "Employee,Manager,Procurement,Admin")]
    public async Task<ActionResult<List<WHTCalculationResult>>> GetWHTHistory(
        int id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting WHT history for purchase order {PurchaseOrderId}", id);

            var history = await _purchaseOrderService.GetWHTHistoryAsync(id, cancellationToken);

            if (history == null)
            {
                return NotFound(new ErrorResponse
                {
                    Error = new ErrorInfo
                    {
                        Message = "Purchase order not found",
                        Code = "PURCHASE_ORDER_NOT_FOUND"
                    }
                });
            }

            return Ok(history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting WHT history for purchase order {PurchaseOrderId}", id);
            return StatusCode(500, new ErrorResponse
            {
                Error = new ErrorInfo
                {
                    Message = "An error occurred while retrieving WHT history",
                    Code = "INTERNAL_ERROR"
                }
            });
        }
    }

    /// <summary>
    /// Recalculates WHT for a purchase order
    /// </summary>
    /// <param name="id">Purchase order ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Purchase order with recalculated WHT</returns>
    [HttpPost("{id:int}/recalculate-wht")]
    [ProducesResponseType(typeof(PurchaseOrderDto), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.NotFound)]
    [Authorize(Roles = "Procurement,Admin")]
    public async Task<ActionResult<PurchaseOrderDto>> RecalculateWHT(
        int id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Recalculating WHT for purchase order {PurchaseOrderId}", id);

            var purchaseOrder = await _purchaseOrderService.RecalculateWHTAsync(id, cancellationToken);

            if (purchaseOrder == null)
            {
                return NotFound(new ErrorResponse
                {
                    Error = new ErrorInfo
                    {
                        Message = $"Purchase order with ID {id} not found",
                        Code = "PURCHASE_ORDER_NOT_FOUND"
                    }
                });
            }

            return Ok(purchaseOrder);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recalculating WHT for purchase order {PurchaseOrderId}", id);
            return StatusCode(500, new ErrorResponse
            {
                Error = new ErrorInfo
                {
                    Message = "An error occurred while recalculating WHT",
                    Code = "INTERNAL_ERROR"
                }
            });
        }
    }

    /// <summary>
    /// Gets purchase order change history
    /// </summary>
    /// <param name="id">Purchase order ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of audit log entries</returns>
    [HttpGet("{id:int}/history")]
    [ProducesResponseType(typeof(IEnumerable<AuditLogDto>), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.NotFound)]
    public async Task<ActionResult<IEnumerable<AuditLogDto>>> GetPurchaseOrderHistory(
        int id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting history for purchase order {PurchaseOrderId}", id);

            var history = await _purchaseOrderService.GetPurchaseOrderHistoryAsync(id, cancellationToken);
            return Ok(history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting history for purchase order {PurchaseOrderId}", id);
            return StatusCode(500, new ErrorResponse
            {
                Error = new ErrorInfo
                {
                    Message = "An error occurred while retrieving purchase order history",
                    Code = "INTERNAL_ERROR"
                }
            });
        }
    }
}
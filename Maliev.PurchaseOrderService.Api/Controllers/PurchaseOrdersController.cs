using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using AutoMapper;
using Maliev.PurchaseOrderService.Api.DTOs;
using Maliev.PurchaseOrderService.Api.ExternalServices;
using Maliev.PurchaseOrderService.Api.Services;
using System.Net;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Maliev.PurchaseOrderService.Api.Models;
using Maliev.PurchaseOrderService.Data;

namespace Maliev.PurchaseOrderService.Api.Controllers;

/// <summary>
/// Purchase Orders API Controller with full CRUD operations
/// </summary>
[ApiController]
[Route("v{version:apiVersion}/purchase-orders")]
[ApiVersion("1.0")]
[ApiVersion("1")]
[Authorize]
[Produces("application/json")]
public class PurchaseOrdersController : ControllerBase
{
    private readonly IPurchaseOrderService _purchaseOrderService;
    private readonly IMapper _mapper;
    private readonly ILogger<PurchaseOrdersController> _logger;
    private readonly PurchaseOrderContext _context;

    public PurchaseOrdersController(
        IPurchaseOrderService purchaseOrderService,
        IMapper mapper,
        ILogger<PurchaseOrdersController> logger,
        PurchaseOrderContext context)
    {
        _purchaseOrderService = purchaseOrderService;
        _mapper = mapper;
        _logger = logger;
        _context = context;
    }

    /// <summary>
    /// Gets all purchase orders with optional filtering and pagination
    /// </summary>
    /// <param name="request">Search and filter criteria</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of purchase orders</returns>
    [HttpGet]
    [Authorize(Roles = "Employee,Manager,Procurement,Admin")]
    [ProducesResponseType(typeof(PaginatedPurchaseOrdersResponse), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.Forbidden)]
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

            // Get current user context for role-based filtering
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.Identity?.Name ?? "unknown";
            var userRoles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

            var result = await _purchaseOrderService.GetPurchaseOrdersAsync(request, cancellationToken);

            // Add required response headers
            Response.Headers["X-Total-Count"] = result.TotalCount.ToString();
            Response.Headers["X-Page"] = result.Page.ToString();
            Response.Headers["X-Page-Size"] = result.PageSize.ToString();
            Response.Headers["X-Total-Pages"] = result.TotalPages.ToString();
            Response.Headers.CacheControl = "no-cache, must-revalidate";

            // Map to the correct response format
            var purchaseOrderResponses = _mapper.Map<List<PurchaseOrderResponse>>(result.Data);
            var response = new PaginatedPurchaseOrdersResponse
            {
                Items = purchaseOrderResponses,
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
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access to purchase orders by user {UserId}", User.Identity?.Name);
            return StatusCode(403, new ErrorResponse
            {
                Error = new ErrorInfo
                {
                    Message = "Access denied. You do not have permission to view purchase orders",
                    Code = "ACCESS_DENIED"
                }
            });
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
    [Authorize(Roles = "Employee,Manager,Procurement,Admin")]
    [ProducesResponseType(typeof(PurchaseOrderDto), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.Forbidden)]
    public async Task<ActionResult<PurchaseOrderDto>> GetPurchaseOrder(
        int id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting purchase order {PurchaseOrderId}", id);

            // Get current user context for role-based access
            var userId = User.Identity?.Name ?? "unknown";
            var userRoles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

            var purchaseOrder = await _purchaseOrderService.GetPurchaseOrderByIdAsync(id, userId, userRoles, cancellationToken);

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

            // Add ETag header for caching support using RowVersion for proper concurrency control
            // Get the entity from database to access RowVersion
            var entity = await _context.PurchaseOrders
                .AsNoTracking()
                .FirstOrDefaultAsync(po => po.Id == id && !po.IsDeleted, cancellationToken);

            string? etag = null;
            if (entity?.RowVersion != null)
            {
                var rowVersionBase64 = Convert.ToBase64String(entity.RowVersion);
                etag = $"\"{rowVersionBase64}\"";
                Response.Headers.ETag = etag;
            }

            // Add Cache-Control header
            Response.Headers.CacheControl = "private, max-age=300"; // 5 minutes cache

            // Handle If-None-Match header for conditional requests
            var ifNoneMatch = Request.Headers["If-None-Match"].ToString();
            if (!string.IsNullOrEmpty(ifNoneMatch) && !string.IsNullOrEmpty(etag) && ifNoneMatch == etag)
            {
                return StatusCode(304); // Not Modified
            }

            return Ok(purchaseOrder);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access to purchase order {PurchaseOrderId} by user {UserId}", id, User.Identity?.Name);
            return StatusCode(403, new ErrorResponse
            {
                Error = new ErrorInfo
                {
                    Message = "Access denied. You do not have permission to view this purchase order",
                    Code = "ACCESS_DENIED"
                }
            });
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
    [Authorize(Roles = "Employee,Manager,Procurement,Admin")]
    [ProducesResponseType(typeof(PurchaseOrderDto), (int)HttpStatusCode.Created)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.Forbidden)]
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
                var validationErrors = ModelState.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>()
                ).Select(kvp => new ValidationError
                {
                    Field = kvp.Key,
                    Message = string.Join(", ", kvp.Value),
                    Code = "VALIDATION_ERROR"
                }).ToList();

                return BadRequest(new ValidationErrorResponse
                {
                    Message = "Invalid request data",
                    Code = "VALIDATION_FAILED",
                    Errors = validationErrors
                });
            }

            // Get CreatedBy from current user context
            var createdBy = User.Identity?.Name ?? "unknown";
            var userRoles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

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

            var createdPurchaseOrder = await _purchaseOrderService.CreatePurchaseOrderAsync(request, createdBy, userRoles, cancellationToken);

            return CreatedAtAction(
                nameof(GetPurchaseOrder),
                new { id = createdPurchaseOrder.Id },
                createdPurchaseOrder);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access when creating purchase order");
            return StatusCode(403, new ErrorResponse
            {
                Error = new ErrorInfo
                {
                    Message = ex.Message,
                    Code = "ACCESS_DENIED"
                }
            });
        }
        catch (ExternalServiceException ex)
        {
            _logger.LogWarning(ex, "External service failure when creating purchase order: {ServiceName}", ex.ServiceName);
            return StatusCode(422, new ErrorResponse
            {
                Error = new ErrorInfo
                {
                    Message = $"External service validation failed: {ex.Message}",
                    Code = "VALIDATION_FAILED"
                }
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error when creating purchase order");
            return BadRequest(new ErrorResponse
            {
                Error = new ErrorInfo
                {
                    Message = ex.Message,
                    Code = "VALIDATION_ERROR"
                }
            });
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
    [Authorize(Roles = "Employee,Manager,Procurement,Admin")]
    [ProducesResponseType(typeof(PurchaseOrderResponse), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.Forbidden)]
    public async Task<ActionResult<PurchaseOrderResponse>> UpdatePurchaseOrder(
        int id,
        [FromBody] UpdatePurchaseOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Updating purchase order {PurchaseOrderId}", id);

            if (!ModelState.IsValid)
            {
                var validationErrors = ModelState.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>()
                ).Select(kvp => new ValidationError
                {
                    Field = kvp.Key,
                    Message = string.Join(", ", kvp.Value),
                    Code = "VALIDATION_ERROR"
                }).ToList();

                return BadRequest(new ValidationErrorResponse
                {
                    Message = "Invalid request data",
                    Code = "VALIDATION_FAILED",
                    Errors = validationErrors
                });
            }

            // Handle If-Match header for optimistic concurrency control
            var ifMatch = Request.Headers["If-Match"].ToString();
            if (!string.IsNullOrEmpty(ifMatch))
            {
                // If If-Match header is provided, override the RowVersion in the request
                // Strip quotes from ETag value
                var etag = ifMatch.Trim('"');
                request.RowVersion = etag;
            }

            // Get UpdatedBy from current user context
            var lastModifiedBy = User.Identity?.Name ?? "unknown";
            var userRoles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

            var updatedPurchaseOrder = await _purchaseOrderService.UpdatePurchaseOrderAsync(id, request, lastModifiedBy, userRoles, cancellationToken);

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

            // Add ETag header for updated resource using RowVersion for proper concurrency control
            if (updatedPurchaseOrder != null)
            {
                // Get the updated RowVersion from database for ETag
                var updatedEntity = await _context.PurchaseOrders
                    .AsNoTracking()
                    .FirstOrDefaultAsync(po => po.Id == id && !po.IsDeleted, cancellationToken);

                if (updatedEntity?.RowVersion != null)
                {
                    var rowVersionBase64 = Convert.ToBase64String(updatedEntity.RowVersion);
                    var etag = $"\"{rowVersionBase64}\"";
                    Response.Headers.ETag = etag;
                }
            }

            // Map to response DTO before returning
            var response = _mapper.Map<PurchaseOrderResponse>(updatedPurchaseOrder);
            return Ok(response);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict when updating purchase order {PurchaseOrderId}", id);
            return Conflict(new ErrorResponse
            {
                Error = new ErrorInfo
                {
                    Message = ex.Message,
                    Code = "CONCURRENCY_CONFLICT"
                }
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access when updating purchase order {PurchaseOrderId}", id);
            return StatusCode(403, new ErrorResponse
            {
                Error = new ErrorInfo
                {
                    Message = ex.Message,
                    Code = "ACCESS_DENIED"
                }
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error when updating purchase order {PurchaseOrderId}", id);
            return BadRequest(new ErrorResponse
            {
                Error = new ErrorInfo
                {
                    Message = ex.Message,
                    Code = "VALIDATION_ERROR"
                }
            });
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
    /// <param name="version">Optional row version for concurrency control</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content if successful</returns>
    [HttpDelete("{id:int}")]
    [ProducesResponseType((int)HttpStatusCode.NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.Conflict)]
    public async Task<ActionResult> DeletePurchaseOrder(
        int id,
        [FromQuery] string? version = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Deleting purchase order {PurchaseOrderId}", id);

            var deletedBy = User.Identity?.Name ?? "unknown";
            var deleted = await _purchaseOrderService.DeletePurchaseOrderAsync(id, deletedBy, version, cancellationToken);

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
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict when deleting purchase order {PurchaseOrderId}", id);
            return Conflict(new ErrorResponse
            {
                Error = new ErrorInfo
                {
                    Message = "Purchase order has been modified by another user. Please refresh and try again.",
                    Code = "CONCURRENCY_CONFLICT"
                }
            });
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
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.Forbidden)]
    [Authorize]
    public async Task<ActionResult<PurchaseOrderDto>> ApprovePurchaseOrder(
        int id,
        [FromBody] ApprovePurchaseOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Approving purchase order {PurchaseOrderId}", id);

            // Check role-based authorization
            var userRoles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
            var allowedRoles = new[] { "Manager", "Procurement", "Admin" };
            if (!userRoles.Any(role => allowedRoles.Contains(role)))
            {
                return StatusCode(403, new ErrorResponse
                {
                    Error = new ErrorInfo
                    {
                        Message = "Insufficient approval permission",
                        Code = "INSUFFICIENT_PERMISSIONS"
                    }
                });
            }

            // Check for high-value approval authorization by getting purchase order first
            var existingPurchaseOrder = await _purchaseOrderService.GetPurchaseOrderByIdAsync(id, cancellationToken);
            if (existingPurchaseOrder == null)
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

            // High-value orders (> 50,000) require Director/Admin approval
            const decimal highValueThreshold = 50000m;
            if (existingPurchaseOrder.TotalAmount > highValueThreshold)
            {
                var highValueRoles = new[] { "Director", "Admin" };
                if (!userRoles.Any(role => highValueRoles.Contains(role)))
                {
                    return StatusCode(403, new ErrorResponse
                    {
                        Error = new ErrorInfo
                        {
                            Message = $"High-value orders (>= {highValueThreshold:C}) require higher approval level",
                            Code = "INSUFFICIENT_APPROVAL_LEVEL"
                        }
                    });
                }
            }

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

            // Set user context from claims
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.Identity?.Name ?? "unknown";
            // Only set ApprovedBy if not already provided in the request
            if (string.IsNullOrEmpty(request.ApprovedBy))
            {
                request.ApprovedBy = userId;
            }
            request.UserRoles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

            var approvedPurchaseOrder = await _purchaseOrderService.ApprovePurchaseOrderAsync(id, request, userId, cancellationToken);

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
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict when approving purchase order {PurchaseOrderId}", id);
            return Conflict(new ErrorResponse
            {
                Error = new ErrorInfo
                {
                    Message = ex.Message,
                    Code = "CONCURRENCY_CONFLICT"
                }
            });
        }
        catch (BusinessRuleException ex)
        {
            _logger.LogWarning(ex, "Business rule violation when approving purchase order {PurchaseOrderId}", id);
            return Conflict(new ErrorResponse
            {
                Error = new ErrorInfo
                {
                    Message = ex.Message,
                    Code = ex.ErrorCode
                }
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access when approving purchase order {PurchaseOrderId}", id);
            return StatusCode(403, new ErrorResponse
            {
                Error = new ErrorInfo
                {
                    Message = ex.Message,
                    Code = "INSUFFICIENT_PERMISSIONS"
                }
            });
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
    [Authorize]
    [ProducesResponseType(typeof(PurchaseOrderDto), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.Forbidden)]
    public async Task<ActionResult<PurchaseOrderDto>> CancelPurchaseOrder(
        int id,
        [FromBody] CancelPurchaseOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Canceling purchase order {PurchaseOrderId}", id);

            // Check role-based authorization - only Manager, Procurement, and Admin can cancel
            var userRoles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
            var allowedRoles = new[] { "Manager", "Procurement", "Admin" };
            if (!userRoles.Any(role => allowedRoles.Contains(role)))
            {
                return StatusCode(403, new ErrorResponse
                {
                    Error = new ErrorInfo
                    {
                        Message = "Insufficient cancellation permission",
                        Code = "INSUFFICIENT_PERMISSIONS"
                    }
                });
            }

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

            // Set user context from claims
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.Identity?.Name ?? "unknown";
            request.CanceledBy = userId;
            request.UserRoles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

            var canceledPurchaseOrder = await _purchaseOrderService.CancelPurchaseOrderAsync(id, request, userId, cancellationToken);

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
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict when canceling purchase order {PurchaseOrderId}", id);
            return Conflict(new ErrorResponse
            {
                Error = new ErrorInfo
                {
                    Message = ex.Message,
                    Code = "CONCURRENCY_CONFLICT"
                }
            });
        }
        catch (BusinessRuleException ex)
        {
            _logger.LogWarning(ex, "Business rule violation when canceling purchase order {PurchaseOrderId}", id);
            return Conflict(new ErrorResponse
            {
                Error = new ErrorInfo
                {
                    Message = ex.Message,
                    Code = ex.ErrorCode
                }
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access when canceling purchase order {PurchaseOrderId}", id);
            return StatusCode(403, new ErrorResponse
            {
                Error = new ErrorInfo
                {
                    Message = ex.Message,
                    Code = "INSUFFICIENT_PERMISSIONS"
                }
            });
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

            if (!ModelState.IsValid)
            {
                var validationErrors = ModelState.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>()
                ).Select(kvp => new ValidationError
                {
                    Field = kvp.Key,
                    Message = string.Join(", ", kvp.Value),
                    Code = "VALIDATION_ERROR"
                }).ToList();

                return BadRequest(new ValidationErrorResponse
                {
                    Message = "Invalid request data",
                    Code = "VALIDATION_FAILED",
                    Errors = validationErrors
                });
            }

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

    /// <summary>
    /// Converts currency amount for a purchase order
    /// </summary>
    /// <param name="id">Purchase order ID</param>
    /// <param name="request">Currency conversion request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Currency conversion result</returns>
    [HttpPost("{id:int}/convert-currency")]
    [ProducesResponseType(typeof(CurrencyConversionResult), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.BadRequest)]
    [Authorize(Roles = "Employee,Manager,Procurement,Admin")]
    public async Task<ActionResult<CurrencyConversionResult>> ConvertPurchaseOrderCurrency(
        int id,
        [FromBody] CurrencyConversionRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Converting currency for purchase order {PurchaseOrderId}", id);

            if (!ModelState.IsValid)
            {
                return BadRequest(new ErrorResponse
                {
                    Error = new ErrorInfo
                    {
                        Message = "Invalid conversion request",
                        Code = "INVALID_REQUEST"
                    }
                });
            }

            // Check if purchase order exists
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

            // For now, return a mock conversion result
            // In a real implementation, this would integrate with the currency service
            var conversionResult = new CurrencyConversionResult
            {
                FromCurrency = request.FromCurrency,
                ToCurrency = request.ToCurrency,
                OriginalAmount = request.Amount,
                ConvertedAmount = request.Amount * GetMockExchangeRate(request.FromCurrency, request.ToCurrency),
                ExchangeRate = GetMockExchangeRate(request.FromCurrency, request.ToCurrency),
                ConvertedAt = DateTime.UtcNow,
                RateSource = "Mock Currency Service"
            };

            return Ok(conversionResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting currency for purchase order {PurchaseOrderId}", id);
            return StatusCode(500, new ErrorResponse
            {
                Error = new ErrorInfo
                {
                    Message = "An error occurred while converting currency",
                    Code = "INTERNAL_ERROR"
                }
            });
        }
    }

    /// <summary>
    /// Gets currency change history for a purchase order
    /// </summary>
    /// <param name="id">Purchase order ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of currency history entries</returns>
    [HttpGet("{id:int}/currency-history")]
    [ProducesResponseType(typeof(IEnumerable<CurrencyHistoryDto>), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.NotFound)]
    [Authorize(Roles = "Employee,Manager,Procurement,Admin")]
    public async Task<ActionResult<IEnumerable<CurrencyHistoryDto>>> GetPurchaseOrderCurrencyHistory(
        int id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting currency history for purchase order {PurchaseOrderId}", id);

            // Check if purchase order exists
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

            // For now, return mock currency history
            // In a real implementation, this would query the database for currency changes
            var currencyHistory = new List<CurrencyHistoryDto>
            {
                new()
                {
                    Id = 1,
                    PurchaseOrderId = id,
                    CurrencyCode = "USD",
                    CurrencyName = "US Dollar",
                    ExchangeRate = 1.0m,
                    Amount = 1000.00m,
                    ChangedAt = DateTime.UtcNow.AddDays(-2),
                    ChangedBy = "system",
                    ChangeReason = "Initial creation"
                },
                new()
                {
                    Id = 2,
                    PurchaseOrderId = id,
                    CurrencyCode = "EUR",
                    CurrencyName = "Euro",
                    ExchangeRate = 0.92m,
                    Amount = 920.00m,
                    ChangedAt = DateTime.UtcNow.AddDays(-1),
                    ChangedBy = "user",
                    ChangeReason = "Currency update"
                },
                new()
                {
                    Id = 3,
                    PurchaseOrderId = id,
                    CurrencyCode = "GBP",
                    CurrencyName = "British Pound",
                    ExchangeRate = 0.78m,
                    Amount = 780.00m,
                    ChangedAt = DateTime.UtcNow,
                    ChangedBy = "user",
                    ChangeReason = "Final currency adjustment"
                }
            };

            return Ok(currencyHistory);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting currency history for purchase order {PurchaseOrderId}", id);
            return StatusCode(500, new ErrorResponse
            {
                Error = new ErrorInfo
                {
                    Message = "An error occurred while retrieving currency history",
                    Code = "INTERNAL_ERROR"
                }
            });
        }
    }

    /// <summary>
    /// Helper method to get mock exchange rates for testing
    /// </summary>
    /// <param name="fromCurrency">Source currency</param>
    /// <param name="toCurrency">Target currency</param>
    /// <returns>Mock exchange rate</returns>
    private static decimal GetMockExchangeRate(string fromCurrency, string toCurrency)
    {
        return (fromCurrency, toCurrency) switch
        {
            ("USD", "THB") => 35.25m,
            ("USD", "EUR") => 0.92m,
            ("USD", "JPY") => 149.50m,
            ("USD", "GBP") => 0.78m,
            ("THB", "USD") => 0.028m,
            ("EUR", "USD") => 1.09m,
            ("JPY", "USD") => 0.0067m,
            ("GBP", "USD") => 1.28m,
            _ => 1.0m
        };
    }
}
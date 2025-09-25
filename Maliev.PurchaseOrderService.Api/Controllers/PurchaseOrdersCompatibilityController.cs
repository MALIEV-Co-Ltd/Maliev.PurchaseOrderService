using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Maliev.PurchaseOrderService.Api.DTOs;
using Maliev.PurchaseOrderService.Api.Services;
using System.Net;

namespace Maliev.PurchaseOrderService.Api.Controllers;

/// <summary>
/// Compatibility controller for non-versioned purchase order endpoints
/// Used by tests and legacy clients
/// </summary>
[ApiController]
[Route("purchase-orders")]
[Authorize]
[Produces("application/json")]
public class PurchaseOrdersCompatibilityController : ControllerBase
{
    private readonly IPurchaseOrderService _purchaseOrderService;
    private readonly ILogger<PurchaseOrdersCompatibilityController> _logger;

    public PurchaseOrdersCompatibilityController(
        IPurchaseOrderService purchaseOrderService,
        ILogger<PurchaseOrdersCompatibilityController> logger)
    {
        _purchaseOrderService = purchaseOrderService;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new purchase order
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(PurchaseOrderResponse), (int)HttpStatusCode.Created)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(ValidationErrorResponse), (int)HttpStatusCode.UnprocessableEntity)]
    public async Task<ActionResult<PurchaseOrderResponse>> CreatePurchaseOrder(
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

            // Return the DTO directly - it should already be in the correct format
            var response = createdPurchaseOrder;

            return CreatedAtAction(
                nameof(GetPurchaseOrder),
                new { id = createdPurchaseOrder.Id },
                response);
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
    /// Gets a purchase order by ID
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(PurchaseOrderResponse), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.NotFound)]
    public async Task<ActionResult<PurchaseOrderResponse>> GetPurchaseOrder(
        int id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting purchase order {PurchaseOrderId}", id);

            // Get current user context for role-based access
            var userId = User.Identity?.Name ?? "unknown";
            var userRoles = User.FindAll(System.Security.Claims.ClaimTypes.Role).Select(c => c.Value).ToList();

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

            // Return the DTO directly
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
    /// Updates an existing purchase order
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(PurchaseOrderResponse), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.BadRequest)]
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

            // Return the DTO directly
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
    /// Cancels a purchase order
    /// </summary>
    [HttpPost("{id:int}/cancel")]
    [ProducesResponseType(typeof(PurchaseOrderResponse), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.BadRequest)]
    public async Task<ActionResult<PurchaseOrderResponse>> CancelPurchaseOrder(
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

            // Set CanceledBy from current user context
            var canceledBy = User.Identity?.Name ?? "unknown";
            request.CanceledBy = canceledBy;

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

            // Return the DTO directly
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
    /// Approves a purchase order
    /// </summary>
    [HttpPost("{id:int}/approve")]
    [ProducesResponseType(typeof(PurchaseOrderResponse), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.BadRequest)]
    public async Task<ActionResult<PurchaseOrderResponse>> ApprovePurchaseOrder(
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

            // Set ApprovedBy from current user context
            var approvedBy = User.Identity?.Name ?? "unknown";
            request.ApprovedBy = approvedBy;

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

            // Return the DTO directly
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
}
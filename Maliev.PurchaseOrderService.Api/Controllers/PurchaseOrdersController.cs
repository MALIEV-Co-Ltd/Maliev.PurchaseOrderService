using Asp.Versioning;
using Maliev.PurchaseOrderService.Application.Interfaces;
using Maliev.PurchaseOrderService.Domain.Entities;
using Maliev.PurchaseOrderService.Domain.Constants;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.PurchaseOrderService.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Maliev.PurchaseOrderService.Api.Controllers;

/// <summary>
/// Controller for managing purchase orders
/// </summary>
[ApiController]
[ApiVersion("1")]
[Route("purchase-order/v{version:apiVersion}/purchase-orders")]
public class PurchaseOrdersController : ControllerBase
{
    private readonly IPurchaseOrderService _purchaseOrderService;
    private readonly ILogger<PurchaseOrdersController> _logger;

    /// <summary>
    /// Initializes a new instance of the PurchaseOrdersController class with the specified purchase order service and
    /// logger.
    /// </summary>
    /// <param name="purchaseOrderService">The service used to manage and process purchase order operations.</param>
    /// <param name="logger">The logger used to record diagnostic and operational information for the controller.</param>
    public PurchaseOrdersController(
        IPurchaseOrderService purchaseOrderService,
        ILogger<PurchaseOrdersController> logger)
    {
        _purchaseOrderService = purchaseOrderService;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new purchase order using the specified request data.
    /// </summary>
    /// <remarks>Requires the caller to have 'purchase-order.orders.create' permission. Returns a 400 Bad Request if
    /// the request is invalid, a 401 Unauthorized if the user is not authenticated, or a 502 Bad Gateway if an external
    /// service is unavailable.</remarks>
    /// <param name="request">The details of the purchase order to create. Must include all required fields for a valid purchase order.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>An ActionResult containing the details of the newly created purchase order if successful. Returns a 201 Created
    /// response with the purchase order details, or an error response if creation fails.</returns>
    [HttpPost]
    [RequirePermission(PurchaseOrderPermissions.Orders.Create)]
    [ProducesResponseType(typeof(PurchaseOrderDetailResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<PurchaseOrderDetailResponse>> CreatePurchaseOrder(
        [FromBody] CreatePurchaseOrderRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "employee";

            var result = await _purchaseOrderService.CreateAsync(
                request, userId, userRole, cancellationToken);

            return CreatedAtAction(
                nameof(GetPurchaseOrderById),
                new { id = result.Id },
                result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to create purchase order");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "External service error during purchase order creation");
            return StatusCode(StatusCodes.Status502BadGateway, new { error = "External service unavailable" });
        }
    }

    /// <summary>
    /// Retrieves the details of a purchase order by its unique identifier.
    /// </summary>
    /// <remarks>Returns status code 404 if the purchase order does not exist, or 403 if the user does not
    /// have 'purchase-order.orders.read' permission.</remarks>
    /// <param name="id">The unique identifier of the purchase order to retrieve.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>An <see cref="ActionResult{PurchaseOrderDetailResponse}"/> containing the purchase order details if found;
    /// otherwise, a response with status code 404 (Not Found) or 403 (Forbidden) if access is denied.</returns>
    [HttpGet("{id}")]
    [RequirePermission(PurchaseOrderPermissions.Orders.Read)]
    [ProducesResponseType(typeof(PurchaseOrderDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PurchaseOrderDetailResponse>> GetPurchaseOrderById(
        int id,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "employee";

        var result = await _purchaseOrderService.GetByIdAsync(
            id, userId, userRole, cancellationToken);

        if (result == null)
        {
            return NotFound();
        }

        return Ok(result);
    }

    /// <summary>
    /// Searches for purchase orders that match the specified criteria and returns a paginated list of results.
    /// </summary>
    /// <remarks>The results are filtered based on the current user's identity and role. Requires 'purchase-order.orders.read' permission.</remarks>
    /// <param name="request">The search criteria used to filter purchase orders. Includes parameters such as date range, status, supplier,
    /// and pagination options. Cannot be null.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>An ActionResult containing a paginated response with purchase order details that match the search criteria.
    /// Returns an empty list if no purchase orders are found.</returns>
    [HttpGet]
    [RequirePermission(PurchaseOrderPermissions.Orders.Read)]
    [ProducesResponseType(typeof(PaginatedResponse<PurchaseOrderResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResponse<PurchaseOrderResponse>>> SearchPurchaseOrders(
        [FromQuery] SearchPurchaseOrdersRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "employee";

        var result = await _purchaseOrderService.SearchAsync(
            request, userId, userRole, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Updates an existing purchase order with the specified changes and returns the updated details.
    /// </summary>
    /// <remarks>Requires 'purchase-order.orders.update' permission. Returns a 404 Not Found response if the
    /// purchase order does not exist, a 400 Bad Request response if the input is invalid, and a 409 Conflict response
    /// if a concurrency conflict occurs during the update.</remarks>
    /// <param name="id">The unique identifier of the purchase order to update.</param>
    /// <param name="request">An object containing the updated purchase order information. Cannot be null.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the update operation.</param>
    /// <returns>An ActionResult containing the updated purchase order details if the update is successful; otherwise, an error
    /// response indicating the reason for failure.</returns>
    [HttpPut("{id}")]
    [RequirePermission(PurchaseOrderPermissions.Orders.Update)]
    [ProducesResponseType(typeof(PurchaseOrderDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PurchaseOrderDetailResponse>> UpdatePurchaseOrder(
        int id,
        [FromBody] UpdatePurchaseOrderRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value
                ?? User.FindFirst("role")?.Value
                ?? "manager";

            var result = await _purchaseOrderService.UpdateAsync(
                id, request, userId, userRole, cancellationToken);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to update purchase order {Id}", id);
            return NotFound(new { error = ex.Message });
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict updating purchase order {Id}", id);
            return Conflict(new { error = "Purchase order was modified by another user" });
        }
    }

    /// <summary>
    /// Attempts to cancel the purchase order with the specified identifier.
    /// </summary>
    /// <remarks>This action requires 'purchase-order.orders.cancel' permission. The operation is performed
    /// asynchronously and supports cancellation via the provided token.</remarks>
    /// <param name="id">The unique identifier of the purchase order to cancel.</param>
    /// <param name="request">The cancellation request containing the reason.</param>
    /// <param name="cancellationToken">A token that can be used to request cancellation of the operation.</param>
    /// <returns>A 204 No Content response if the purchase order is successfully canceled; otherwise, a 404 Not Found response if
    /// the purchase order does not exist or cannot be canceled.</returns>
    [HttpPost("{id}/cancel")]
    [RequirePermission(PurchaseOrderPermissions.Orders.Cancel)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelPurchaseOrder(
        int id,
        [FromBody] CancelPurchaseOrderRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value
                ?? User.FindFirst("role")?.Value
                ?? "manager";
            await _purchaseOrderService.CancelAsync(
                id, request.Reason, userId, userRole, cancellationToken);

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to cancel purchase order {Id}", id);
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Approves the purchase order with the specified identifier.
    /// </summary>
    /// <remarks>Requires 'purchase-order.orders.approve' permission. Transitions the order status
    /// from Pending to Approved. Returns 404 if the order does not exist, and 400 if the order
    /// is not in Pending status.</remarks>
    /// <param name="id">The unique identifier of the purchase order to approve.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A 200 OK response with the updated purchase order details if successful; otherwise,
    /// a 404 Not Found or 400 Bad Request response indicating the reason for failure.</returns>
    [HttpPost("{id}/approve")]
    [RequirePermission(PurchaseOrderPermissions.Orders.Approve)]
    [ProducesResponseType(typeof(PurchaseOrderDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PurchaseOrderDetailResponse>> ApprovePurchaseOrder(
        int id,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value
                ?? User.FindFirst("role")?.Value
                ?? "manager";

            var result = await _purchaseOrderService.ApproveAsync(
                id, userId, userRole, cancellationToken);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to approve purchase order {Id}", id);

            if (ex.Message.Contains("not found"))
            {
                return NotFound(new { error = ex.Message });
            }

            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Sends the approved purchase order to the supplier.
    /// </summary>
    /// <remarks>Requires 'purchase-order.orders.send' permission. Transitions the order status
    /// from Approved to Ordered. Returns 404 if the order does not exist, and 400 if the order
    /// is not in Approved status.</remarks>
    /// <param name="id">The unique identifier of the purchase order to send to supplier.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A 200 OK response with the updated purchase order details if successful; otherwise,
    /// a 404 Not Found or 400 Bad Request response indicating the reason for failure.</returns>
    [HttpPost("{id}/send-to-supplier")]
    [RequirePermission(PurchaseOrderPermissions.Orders.Send)]
    [ProducesResponseType(typeof(PurchaseOrderDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PurchaseOrderDetailResponse>> SendToSupplier(
        int id,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value
                ?? User.FindFirst("role")?.Value
                ?? "manager";

            var result = await _purchaseOrderService.SendToSupplierAsync(
                id, userId, userRole, cancellationToken);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to send purchase order {Id} to supplier", id);

            if (ex.Message.Contains("not found"))
            {
                return NotFound(new { error = ex.Message });
            }

            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Marks goods as received for the specified purchase order.
    /// </summary>
    /// <remarks>Requires 'purchase-order.orders.receive' permission. Transitions the order status
    /// from Ordered to Delivered (if full receipt). Returns 404 if the order does not exist, and 400 if the order
    /// is not in Ordered status.</remarks>
    /// <param name="id">The unique identifier of the purchase order for which goods are being received.</param>
    /// <param name="isPartialReceipt">Indicates whether this is a partial receipt. If false, the order status
    /// will be updated to Delivered. If true, the order remains in Ordered status for future partial receipts.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>A 200 OK response with the updated purchase order details if successful; otherwise,
    /// a 404 Not Found or 400 Bad Request response indicating the reason for failure.</returns>
    [HttpPost("{id}/receive")]
    [RequirePermission(PurchaseOrderPermissions.Orders.Receive)]
    [ProducesResponseType(typeof(PurchaseOrderDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PurchaseOrderDetailResponse>> ReceiveItems(
        int id,
        [FromQuery] bool isPartialReceipt = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value
                ?? User.FindFirst("role")?.Value
                ?? "manager";

            var result = await _purchaseOrderService.ReceiveGoodsAsync(
                id, isPartialReceipt, userId, userRole, cancellationToken);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to receive goods for purchase order {Id}", id);

            if (ex.Message.Contains("not found"))
            {
                return NotFound(new { error = ex.Message });
            }

            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Exports the purchase order details to a specified format.
    /// </summary>
    /// <remarks>Requires 'purchase-order.orders.export' permission.</remarks>
    [HttpGet("{id}/export")]
    [RequirePermission(PurchaseOrderPermissions.Orders.Export)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ExportPurchaseOrder(
        int id,
        [FromQuery] string format = "pdf",
        CancellationToken cancellationToken = default)
    {
        // Implementation logic would go here
        return Ok(new { message = $"Exporting PO {id} as {format}" });
    }
}

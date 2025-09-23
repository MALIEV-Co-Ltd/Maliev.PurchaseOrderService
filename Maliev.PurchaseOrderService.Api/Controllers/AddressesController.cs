using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;
using Maliev.PurchaseOrderService.Api.DTOs;
using Maliev.PurchaseOrderService.Data;
using Maliev.PurchaseOrderService.Data.Entities;
using Maliev.PurchaseOrderService.Data.Enums;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using System.Net;

namespace Maliev.PurchaseOrderService.Api.Controllers;

/// <summary>
/// Addresses API Controller for managing purchase order addresses
/// </summary>
[ApiController]
[Route("v{version:apiVersion}/addresses")]
[ApiVersion("1.0")]
[Authorize]
[Produces("application/json")]
public class AddressesController : ControllerBase
{
    private readonly PurchaseOrderContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<AddressesController> _logger;

    public AddressesController(
        PurchaseOrderContext context,
        IMapper mapper,
        ILogger<AddressesController> logger)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Gets all addresses for purchase orders (filtered by user access)
    /// </summary>
    /// <param name="type">Address type filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of addresses</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<AddressDto>), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.NotFound)]
    public async Task<ActionResult<IEnumerable<AddressDto>>> GetAddresses(
        [FromQuery] AddressType? type = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting addresses (filtered by user access and type: {AddressType})", type);

            // Get all addresses from purchase orders that user has access to
            var query = _context.Addresses.AsQueryable();

            // Apply address type filter if specified
            if (type.HasValue)
            {
                query = query.Where(a => a.AddressType == type.Value);
            }

            // TODO: Apply user access filtering based on user role and ownership
            // For now, returning all addresses (this should be filtered by user permissions)

            var addresses = await query.OrderBy(a => a.CreatedAt).ToListAsync(cancellationToken);
            var addressDtos = _mapper.Map<IEnumerable<AddressDto>>(addresses);

            return Ok(addressDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting addresses (type: {AddressType})", type);
            return StatusCode(500, new ErrorResponse
            {
                Error = new ErrorInfo
                {
                    Message = "An error occurred while retrieving addresses",
                    Code = "INTERNAL_ERROR"
                }
            });
        }
    }

    /// <summary>
    /// Gets a specific address by ID
    /// </summary>
    /// <param name="purchaseOrderId">Purchase order ID</param>
    /// <param name="addressId">Address ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Address details</returns>
    [HttpGet("{addressId:int}")]
    [ProducesResponseType(typeof(AddressDto), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.NotFound)]
    public async Task<ActionResult<AddressDto>> GetAddress(
        int purchaseOrderId,
        int addressId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting address {AddressId} for purchase order {PurchaseOrderId}", addressId, purchaseOrderId);

            // Get purchase order with addresses to verify relationship
            var purchaseOrder = await _context.PurchaseOrders
                .Include(po => po.ShippingAddress)
                .Include(po => po.BillingAddress)
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

            // Check if the requested address is associated with this purchase order
            Address? address = null;
            if (purchaseOrder.ShippingAddress?.Id == addressId)
                address = purchaseOrder.ShippingAddress;
            else if (purchaseOrder.BillingAddress?.Id == addressId)
                address = purchaseOrder.BillingAddress;

            if (address == null)
            {
                return NotFound(new ErrorResponse
                {
                    Error = new ErrorInfo
                    {
                        Message = $"Address with ID {addressId} not found for purchase order {purchaseOrderId}",
                        Code = "ADDRESS_NOT_FOUND"
                    }
                });
            }

            var addressDto = _mapper.Map<AddressDto>(address);
            return Ok(addressDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting address {AddressId} for purchase order {PurchaseOrderId}", addressId, purchaseOrderId);
            return StatusCode(500, new ErrorResponse
            {
                Error = new ErrorInfo
                {
                    Message = "An error occurred while retrieving the address",
                    Code = "INTERNAL_ERROR"
                }
            });
        }
    }

    /// <summary>
    /// Creates a new address
    /// </summary>
    /// <param name="request">Create address request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Created address</returns>
    [HttpPost]
    [ProducesResponseType(typeof(AddressDto), (int)HttpStatusCode.Created)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.BadRequest)]
    public async Task<ActionResult<AddressDto>> CreateAddress(
        [FromBody] CreateAddressRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating new address");

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

            // Create new address
            var address = new Address
            {
                AddressType = request.AddressType,
                ContactName = request.ContactName,
                CompanyName = request.CompanyName,
                AddressLine1 = request.AddressLine1,
                AddressLine2 = request.AddressLine2,
                City = request.City,
                StateProvince = request.StateProvince,
                PostalCode = request.PostalCode,
                Country = request.Country,
                PhoneNumber = request.PhoneNumber,
                EmailAddress = request.EmailAddress,
                CreatedAt = DateTime.UtcNow
            };

            _context.Addresses.Add(address);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Address created successfully with ID {AddressId}", address.Id);

            var addressDto = _mapper.Map<AddressDto>(address);

            return CreatedAtAction(
                nameof(GetAddresses),
                null,
                addressDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating address");
            return StatusCode(500, new ErrorResponse
            {
                Error = new ErrorInfo
                {
                    Message = "An error occurred while creating the address",
                    Code = "INTERNAL_ERROR"
                }
            });
        }
    }

    /// <summary>
    /// Updates an existing address
    /// </summary>
    /// <param name="purchaseOrderId">Purchase order ID</param>
    /// <param name="addressId">Address ID</param>
    /// <param name="request">Update address request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated address</returns>
    [HttpPut("{addressId:int}")]
    [ProducesResponseType(typeof(AddressDto), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.BadRequest)]
    public async Task<ActionResult<AddressDto>> UpdateAddress(
        int purchaseOrderId,
        int addressId,
        [FromBody] UpdateAddressRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Updating address {AddressId} for purchase order {PurchaseOrderId}", addressId, purchaseOrderId);

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

            // Get purchase order with addresses to verify relationship and status
            var purchaseOrder = await _context.PurchaseOrders
                .Include(po => po.ShippingAddress)
                .Include(po => po.BillingAddress)
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

            // Check if the requested address is associated with this purchase order
            Address? address = null;
            if (purchaseOrder.ShippingAddress?.Id == addressId)
                address = purchaseOrder.ShippingAddress;
            else if (purchaseOrder.BillingAddress?.Id == addressId)
                address = purchaseOrder.BillingAddress;

            if (address == null)
            {
                return NotFound(new ErrorResponse
                {
                    Error = new ErrorInfo
                    {
                        Message = $"Address with ID {addressId} not found for purchase order {purchaseOrderId}",
                        Code = "ADDRESS_NOT_FOUND"
                    }
                });
            }

            // Check purchase order status
            if (purchaseOrder.Status == OrderStatus.Approved || purchaseOrder.Status == OrderStatus.Cancelled)
            {
                return BadRequest(new ErrorResponse
                {
                    Error = new ErrorInfo
                    {
                        Message = $"Cannot update addresses for a {purchaseOrder.Status.ToString().ToLower()} purchase order",
                        Code = "INVALID_PURCHASE_ORDER_STATUS"
                    }
                });
            }

            // Store original data for audit
            var originalData = System.Text.Json.JsonSerializer.Serialize(_mapper.Map<AddressDto>(address));

            // Update address fields
            var hasChanges = false;

            if (!string.IsNullOrWhiteSpace(request.ContactName) && request.ContactName != address.ContactName)
            {
                address.ContactName = request.ContactName;
                hasChanges = true;
            }

            if (request.CompanyName != address.CompanyName)
            {
                address.CompanyName = request.CompanyName;
                hasChanges = true;
            }

            if (!string.IsNullOrWhiteSpace(request.AddressLine1) && request.AddressLine1 != address.AddressLine1)
            {
                address.AddressLine1 = request.AddressLine1;
                hasChanges = true;
            }

            if (request.AddressLine2 != address.AddressLine2)
            {
                address.AddressLine2 = request.AddressLine2;
                hasChanges = true;
            }

            if (!string.IsNullOrWhiteSpace(request.City) && request.City != address.City)
            {
                address.City = request.City;
                hasChanges = true;
            }

            if (request.StateProvince != address.StateProvince)
            {
                address.StateProvince = request.StateProvince;
                hasChanges = true;
            }

            if (!string.IsNullOrWhiteSpace(request.PostalCode) && request.PostalCode != address.PostalCode)
            {
                address.PostalCode = request.PostalCode;
                hasChanges = true;
            }

            if (!string.IsNullOrWhiteSpace(request.Country) && request.Country != address.Country)
            {
                address.Country = request.Country;
                hasChanges = true;
            }

            if (request.PhoneNumber != address.PhoneNumber)
            {
                address.PhoneNumber = request.PhoneNumber;
                hasChanges = true;
            }

            if (request.EmailAddress != address.EmailAddress)
            {
                address.EmailAddress = request.EmailAddress;
                hasChanges = true;
            }

            if (!hasChanges)
            {
                _logger.LogInformation("No changes detected for address {AddressId}", addressId);
                return Ok(_mapper.Map<AddressDto>(address));
            }

            // Set update metadata
            address.UpdatedAt = DateTime.UtcNow;

            // Update purchase order modification timestamp
            purchaseOrder.UpdatedBy = User.Identity?.Name ?? "unknown";
            purchaseOrder.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            // Create audit log
            await CreateAuditLogAsync(purchaseOrderId, AuditAction.Update,
                $"Address updated: {address.AddressType} - {address.AddressLine1}, {address.City}",
                User.Identity?.Name ?? "unknown", cancellationToken, originalData);

            _logger.LogInformation("Address {AddressId} updated successfully", addressId);

            var addressDto = _mapper.Map<AddressDto>(address);
            return Ok(addressDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating address {AddressId} for purchase order {PurchaseOrderId}", addressId, purchaseOrderId);
            return StatusCode(500, new ErrorResponse
            {
                Error = new ErrorInfo
                {
                    Message = "An error occurred while updating the address",
                    Code = "INTERNAL_ERROR"
                }
            });
        }
    }

    /// <summary>
    /// Deletes an address (removes the reference from purchase order)
    /// </summary>
    /// <param name="purchaseOrderId">Purchase order ID</param>
    /// <param name="addressId">Address ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content if successful</returns>
    [HttpDelete("{addressId:int}")]
    [ProducesResponseType((int)HttpStatusCode.NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.BadRequest)]
    public async Task<ActionResult> DeleteAddress(
        int purchaseOrderId,
        int addressId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Deleting address {AddressId} for purchase order {PurchaseOrderId}", addressId, purchaseOrderId);

            // Get purchase order with addresses to verify relationship and status
            var purchaseOrder = await _context.PurchaseOrders
                .Include(po => po.ShippingAddress)
                .Include(po => po.BillingAddress)
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

            // Check if the requested address is associated with this purchase order
            Address? address = null;
            bool isShippingAddress = false;
            bool isBillingAddress = false;

            if (purchaseOrder.ShippingAddress?.Id == addressId)
            {
                address = purchaseOrder.ShippingAddress;
                isShippingAddress = true;
            }
            else if (purchaseOrder.BillingAddress?.Id == addressId)
            {
                address = purchaseOrder.BillingAddress;
                isBillingAddress = true;
            }

            if (address == null)
            {
                return NotFound(new ErrorResponse
                {
                    Error = new ErrorInfo
                    {
                        Message = $"Address with ID {addressId} not found for purchase order {purchaseOrderId}",
                        Code = "ADDRESS_NOT_FOUND"
                    }
                });
            }

            // Check purchase order status
            if (purchaseOrder.Status == OrderStatus.Approved || purchaseOrder.Status == OrderStatus.Cancelled)
            {
                return BadRequest(new ErrorResponse
                {
                    Error = new ErrorInfo
                    {
                        Message = $"Cannot delete addresses from a {purchaseOrder.Status.ToString().ToLower()} purchase order",
                        Code = "INVALID_PURCHASE_ORDER_STATUS"
                    }
                });
            }

            // Remove the address reference from purchase order
            if (isShippingAddress)
            {
                purchaseOrder.ShippingAddressId = null;
                purchaseOrder.ShippingAddress = null;
            }
            else if (isBillingAddress)
            {
                purchaseOrder.BillingAddressId = null;
                purchaseOrder.BillingAddress = null;
            }

            // Update purchase order modification metadata
            purchaseOrder.UpdatedBy = User.Identity?.Name ?? "unknown";
            purchaseOrder.UpdatedAt = DateTime.UtcNow;

            // Note: We're not physically deleting the address from the database
            // to maintain data integrity and audit trail. We're only removing
            // the reference from the purchase order.

            await _context.SaveChangesAsync(cancellationToken);

            // Create audit log
            await CreateAuditLogAsync(purchaseOrderId, AuditAction.Delete,
                $"Address removed from purchase order: {address.AddressType} - {address.AddressLine1}, {address.City}",
                User.Identity?.Name ?? "unknown", cancellationToken);

            _logger.LogInformation("Address {AddressId} removed from purchase order {PurchaseOrderId} successfully", addressId, purchaseOrderId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting address {AddressId} for purchase order {PurchaseOrderId}", addressId, purchaseOrderId);
            return StatusCode(500, new ErrorResponse
            {
                Error = new ErrorInfo
                {
                    Message = "An error occurred while deleting the address",
                    Code = "INTERNAL_ERROR"
                }
            });
        }
    }

    /// <summary>
    /// Gets addresses filtered by type
    /// </summary>
    /// <param name="purchaseOrderId">Purchase order ID</param>
    /// <param name="addressType">Address type filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Addresses of the specified type</returns>
    [HttpGet("by-type/{addressType}")]
    [ProducesResponseType(typeof(IEnumerable<AddressDto>), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.NotFound)]
    public async Task<ActionResult<IEnumerable<AddressDto>>> GetAddressesByType(
        int purchaseOrderId,
        AddressType addressType,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting {AddressType} addresses for purchase order {PurchaseOrderId}", addressType, purchaseOrderId);

            // Get purchase order with addresses
            var purchaseOrder = await _context.PurchaseOrders
                .Include(po => po.ShippingAddress)
                .Include(po => po.BillingAddress)
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

            // Get addresses of specified type
            var addresses = new List<Address>();

            if (addressType == AddressType.Shipping && purchaseOrder.ShippingAddress != null)
            {
                addresses.Add(purchaseOrder.ShippingAddress);
            }
            else if (addressType == AddressType.Billing && purchaseOrder.BillingAddress != null)
            {
                addresses.Add(purchaseOrder.BillingAddress);
            }

            var addressDtos = _mapper.Map<IEnumerable<AddressDto>>(addresses);

            return Ok(addressDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting {AddressType} addresses for purchase order {PurchaseOrderId}", addressType, purchaseOrderId);
            return StatusCode(500, new ErrorResponse
            {
                Error = new ErrorInfo
                {
                    Message = "An error occurred while retrieving addresses",
                    Code = "INTERNAL_ERROR"
                }
            });
        }
    }

    private async Task CreateAuditLogAsync(int purchaseOrderId, AuditAction action, string description, string userId, CancellationToken cancellationToken, string? originalData = null)
    {
        var auditLog = new AuditLog
        {
            EntityType = "Address",
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
}
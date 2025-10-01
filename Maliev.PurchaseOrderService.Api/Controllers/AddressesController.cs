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
[Route("v{version:apiVersion}/purchase-orders/{purchaseOrderId:int}/addresses")]
[ApiVersion("1.0")]
[ApiVersion("1")]
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
    /// Gets addresses for purchase orders with optional pagination and filtering
    /// </summary>
    /// <param name="purchaseOrderId">Optional purchase order ID for nested route</param>
    /// <param name="type">Address type filter</param>
    /// <param name="country">Country filter</param>
    /// <param name="city">City filter</param>
    /// <param name="search">Search query for address fields</param>
    /// <param name="sortBy">Sort field</param>
    /// <param name="sortOrder">Sort order (asc/desc)</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Items per page</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of addresses or paginated response</returns>
    [Authorize(Roles = "Employee,Manager,Procurement,Admin")]
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<AddressDto>), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(PaginatedResponse<AddressDto>), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.NotFound)]
    [ProducesResponseType(typeof(ValidationErrorResponse), (int)HttpStatusCode.BadRequest)]
    public async Task<ActionResult> GetAddresses(
        [FromRoute] int? purchaseOrderId = null,
        [FromQuery] AddressType? type = null,
        [FromQuery] string? country = null,
        [FromQuery] string? city = null,
        [FromQuery] string? search = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortOrder = null,
        [FromQuery] int? page = null,
        [FromQuery] int? pageSize = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting addresses (PO: {PurchaseOrderId}, Type: {AddressType})", purchaseOrderId, type);

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

            // Validate sortBy parameter
            if (!string.IsNullOrWhiteSpace(sortBy))
            {
                var validSortFields = new[] { "contactname", "companyname", "city", "country", "addresstype", "createdat" };
                if (!validSortFields.Contains(sortBy.ToLower()))
                {
                    return BadRequest(new ValidationErrorResponse
                    {
                        Message = "Invalid sort field",
                        Code = "INVALID_SORT_FIELD",
                        Errors = new List<ValidationError>
                        {
                            new ValidationError
                            {
                                Field = "sortBy",
                                Message = $"Sort field must be one of: {string.Join(", ", validSortFields)}",
                                Code = "INVALID_SORT_FIELD",
                                Value = sortBy
                            }
                        }
                    });
                }
            }

            // If purchase order ID is specified (nested route), verify it exists
            if (purchaseOrderId.HasValue)
            {
                var purchaseOrderExists = await _context.PurchaseOrders
                    .AnyAsync(po => po.Id == purchaseOrderId.Value && !po.IsDeleted, cancellationToken);

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
            }

            // Build query
            var baseQuery = _context.Addresses.AsQueryable();

            // Filter by purchase order if specified
            if (purchaseOrderId.HasValue)
            {
                baseQuery = baseQuery.Where(a =>
                    _context.PurchaseOrders
                        .Any(po => po.Id == purchaseOrderId.Value &&
                                   (po.ShippingAddressId == a.Id || po.BillingAddressId == a.Id)));
            }

            // Apply address type filter if specified
            if (type.HasValue)
            {
                baseQuery = baseQuery.Where(a => a.AddressType == type.Value);
            }

            // Apply country filter if specified
            if (!string.IsNullOrWhiteSpace(country))
            {
                baseQuery = baseQuery.Where(a => a.Country.Contains(country));
            }

            // Apply city filter if specified
            if (!string.IsNullOrWhiteSpace(city))
            {
                baseQuery = baseQuery.Where(a => a.City.Contains(city));
            }

            // Apply search filter if specified
            if (!string.IsNullOrWhiteSpace(search))
            {
                baseQuery = baseQuery.Where(a =>
                    a.ContactName.Contains(search) ||
                    a.CompanyName != null && a.CompanyName.Contains(search) ||
                    a.AddressLine1.Contains(search) ||
                    a.AddressLine2 != null && a.AddressLine2.Contains(search) ||
                    a.City.Contains(search) ||
                    a.StateProvince != null && a.StateProvince.Contains(search) ||
                    a.PostalCode.Contains(search) ||
                    a.Country.Contains(search));
            }

            // TODO: Apply user access filtering based on user role and ownership
            // For now, returning all addresses (this should be filtered by user permissions)

            // Apply sorting
            var query = baseQuery.AsQueryable();

            if (!string.IsNullOrWhiteSpace(sortBy))
            {
                var isDescending = sortOrder?.ToLower() == "desc";

                query = sortBy.ToLower() switch
                {
                    "contactname" => isDescending ? query.OrderByDescending(a => a.ContactName) : query.OrderBy(a => a.ContactName),
                    "companyname" => isDescending ? query.OrderByDescending(a => a.CompanyName) : query.OrderBy(a => a.CompanyName),
                    "city" => isDescending ? query.OrderByDescending(a => a.City) : query.OrderBy(a => a.City),
                    "country" => isDescending ? query.OrderByDescending(a => a.Country) : query.OrderBy(a => a.Country),
                    "addresstype" => isDescending ? query.OrderByDescending(a => a.AddressType) : query.OrderBy(a => a.AddressType),
                    "createdat" => isDescending ? query.OrderByDescending(a => a.CreatedAt) : query.OrderBy(a => a.CreatedAt),
                    _ => query.OrderBy(a => a.CreatedAt) // Default sort
                };
            }
            else
            {
                query = query.OrderBy(a => a.CreatedAt);
            }

            // If pagination is requested
            if (page.HasValue && pageSize.HasValue)
            {
                var totalCount = await query.CountAsync(cancellationToken);
                var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize.Value);

                var addresses = await query
                    .Skip((page.Value - 1) * pageSize.Value)
                    .Take(pageSize.Value)
                    .ToListAsync(cancellationToken);

                var addressDtos = _mapper.Map<IEnumerable<AddressDto>>(addresses);

                var response = new PaginatedResponse<AddressDto>
                {
                    Data = addressDtos,
                    Page = page.Value,
                    PageSize = pageSize.Value,
                    TotalCount = totalCount,
                    TotalPages = totalPages,
                    HasPreviousPage = page.Value > 1,
                    HasNextPage = page.Value < totalPages
                };

                // Add Cache-Control header
                Response.Headers["Cache-Control"] = "private, max-age=300"; // 5 minutes

                return Ok(response);
            }
            else
            {
                // Return all addresses without pagination
                var addresses = await query.ToListAsync(cancellationToken);
                var addressDtos = _mapper.Map<IEnumerable<AddressDto>>(addresses);

                // Add Cache-Control header
                Response.Headers["Cache-Control"] = "private, max-age=300"; // 5 minutes

                return Ok(addressDtos);
            }
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
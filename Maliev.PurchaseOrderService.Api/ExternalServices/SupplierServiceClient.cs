using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Maliev.PurchaseOrderService.Api.Configuration;
using Maliev.PurchaseOrderService.Api.DTOs;

namespace Maliev.PurchaseOrderService.Api.ExternalServices;

/// <summary>
/// HTTP client implementation for Supplier Service integration
/// Handles supplier validation, address management, and product catalog operations
/// </summary>
public class SupplierServiceClient : ISupplierServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SupplierServiceClient> _logger;
    private readonly ExternalServiceOptions _options;
    private readonly JsonSerializerOptions _jsonOptions;

    public SupplierServiceClient(
        HttpClient httpClient,
        ILogger<SupplierServiceClient> logger,
        IOptions<ExternalServiceOptions> options)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
    }

    /// <inheritdoc />
    public async Task<SupplierDto?> GetSupplierAsync(int supplierId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting supplier information for ID: {SupplierId}", supplierId);

            var response = await _httpClient.GetAsync($"/suppliers/{supplierId}", cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Supplier not found for ID: {SupplierId}", supplierId);
                return null;
            }

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var supplier = JsonSerializer.Deserialize<SupplierDto>(content, _jsonOptions);

            _logger.LogInformation("Successfully retrieved supplier information for ID: {SupplierId}", supplierId);
            return supplier;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error occurred while getting supplier {SupplierId}", supplierId);
            throw new ExternalServiceException($"Failed to get supplier {supplierId}: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogError(ex, "Timeout occurred while getting supplier {SupplierId}", supplierId);
            throw new ExternalServiceException($"Timeout while getting supplier {supplierId}", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON deserialization error while getting supplier {SupplierId}", supplierId);
            throw new ExternalServiceException($"Invalid response format while getting supplier {supplierId}", ex);
        }
    }

    /// <inheritdoc />
    public async Task<SupplierContactDto?> GetSupplierContactAsync(int supplierId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting supplier contact information for ID: {SupplierId}", supplierId);

            var response = await _httpClient.GetAsync($"/suppliers/{supplierId}/contact", cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Supplier contact not found for ID: {SupplierId}", supplierId);
                return null;
            }

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var contact = JsonSerializer.Deserialize<SupplierContactDto>(content, _jsonOptions);

            _logger.LogInformation("Successfully retrieved supplier contact for ID: {SupplierId}", supplierId);
            return contact;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error occurred while getting supplier contact {SupplierId}", supplierId);
            throw new ExternalServiceException($"Failed to get supplier contact {supplierId}: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogError(ex, "Timeout occurred while getting supplier contact {SupplierId}", supplierId);
            throw new ExternalServiceException($"Timeout while getting supplier contact {supplierId}", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON deserialization error while getting supplier contact {SupplierId}", supplierId);
            throw new ExternalServiceException($"Invalid response format while getting supplier contact {supplierId}", ex);
        }
    }

    /// <inheritdoc />
    public async Task<SupplierDto?> ValidateSupplierAsync(int supplierId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Validating supplier for ID: {SupplierId}", supplierId);

            var response = await _httpClient.GetAsync($"/suppliers/{supplierId}/validate", cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Supplier validation failed - not found for ID: {SupplierId}", supplierId);
                return null;
            }

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var supplier = JsonSerializer.Deserialize<SupplierDto>(content, _jsonOptions);

            _logger.LogInformation("Supplier validation successful for ID {SupplierId}", supplierId);
            return supplier;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error occurred while validating supplier {SupplierId}", supplierId);
            throw new ExternalServiceException($"Failed to validate supplier {supplierId}: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogError(ex, "Timeout occurred while validating supplier {SupplierId}", supplierId);
            throw new ExternalServiceException($"Timeout while validating supplier {supplierId}", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON deserialization error while validating supplier {SupplierId}", supplierId);
            throw new ExternalServiceException($"Invalid response format while validating supplier {supplierId}", ex);
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<SupplierProductDto>> GetSupplierProductsAsync(int supplierId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting supplier products for ID: {SupplierId}", supplierId);

            var response = await _httpClient.GetAsync($"/suppliers/{supplierId}/products", cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Supplier products not found for ID: {SupplierId}", supplierId);
                return Enumerable.Empty<SupplierProductDto>();
            }

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var products = JsonSerializer.Deserialize<IEnumerable<SupplierProductDto>>(content, _jsonOptions) ??
                          Enumerable.Empty<SupplierProductDto>();

            _logger.LogInformation("Successfully retrieved {ProductCount} products for supplier ID: {SupplierId}",
                products.Count(), supplierId);
            return products;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error occurred while getting supplier products {SupplierId}", supplierId);
            throw new ExternalServiceException($"Failed to get supplier products {supplierId}: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogError(ex, "Timeout occurred while getting supplier products {SupplierId}", supplierId);
            throw new ExternalServiceException($"Timeout while getting supplier products {supplierId}", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON deserialization error while getting supplier products {SupplierId}", supplierId);
            throw new ExternalServiceException($"Invalid response format while getting supplier products {supplierId}", ex);
        }
    }

    /// <inheritdoc />
    public async Task<SupplierPaymentTermsDto?> GetSupplierPaymentTermsAsync(int supplierId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting supplier payment terms for ID: {SupplierId}", supplierId);

            var response = await _httpClient.GetAsync($"/suppliers/{supplierId}/payment-terms", cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Supplier payment terms not found for ID: {SupplierId}", supplierId);
                return null;
            }

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var paymentTerms = JsonSerializer.Deserialize<SupplierPaymentTermsDto>(content, _jsonOptions);

            _logger.LogInformation("Successfully retrieved supplier payment terms for ID: {SupplierId}", supplierId);
            return paymentTerms;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error occurred while getting supplier payment terms {SupplierId}", supplierId);
            throw new ExternalServiceException($"Failed to get supplier payment terms {supplierId}: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogError(ex, "Timeout occurred while getting supplier payment terms {SupplierId}", supplierId);
            throw new ExternalServiceException($"Timeout while getting supplier payment terms {supplierId}", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON deserialization error while getting supplier payment terms {SupplierId}", supplierId);
            throw new ExternalServiceException($"Invalid response format while getting supplier payment terms {supplierId}", ex);
        }
    }
}


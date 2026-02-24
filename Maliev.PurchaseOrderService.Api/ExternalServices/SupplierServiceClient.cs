namespace Maliev.PurchaseOrderService.Api.ExternalServices;

/// <summary>
/// Implementation of SupplierService client
/// </summary>
public class SupplierServiceClient : ISupplierServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SupplierServiceClient> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SupplierServiceClient"/> class.
    /// </summary>
    /// <param name="httpClientFactory">The HTTP client factory.</param>
    /// <param name="logger">The logger instance.</param>
    public SupplierServiceClient(IHttpClientFactory httpClientFactory, ILogger<SupplierServiceClient> logger)
    {
        _httpClient = httpClientFactory.CreateClient("SupplierService");
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<SupplierDto?> GetSupplierAsync(int supplierId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{supplierId}", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch supplier {SupplierId}: {StatusCode}", supplierId, response.StatusCode);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<SupplierDto>(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching supplier {SupplierId}", supplierId);
            throw new ExternalServiceException($"Failed to fetch supplier {supplierId}", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<bool> ValidateSupplierExistsAsync(int supplierId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{supplierId}", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating supplier {SupplierId}", supplierId);
            return false;
        }
    }
}

/// <summary>
/// Exception thrown when an external service call fails.
/// </summary>
public class ExternalServiceException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExternalServiceException"/> class.
    /// </summary>
    public ExternalServiceException() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExternalServiceException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public ExternalServiceException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExternalServiceException"/> class with a specified error message
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception, or a null reference
    /// if no inner exception is specified.</param>
    public ExternalServiceException(string message, Exception innerException) : base(message, innerException) { }
}

namespace Maliev.PurchaseOrderService.Api.Configuration;

/// <summary>
/// Configuration options for external service endpoints
/// </summary>
public class ExternalServiceOptions
{
    public const string SectionName = "ExternalServices";

    public ServiceEndpoint SupplierService { get; set; } = new();
    public ServiceEndpoint OrderService { get; set; } = new();
    public ServiceEndpoint CurrencyService { get; set; } = new();
    public ServiceEndpoint UploadService { get; set; } = new();
    public ServiceEndpoint PdfService { get; set; } = new();
    public ServiceEndpoint AuthService { get; set; } = new();
}

/// <summary>
/// Configuration for individual service endpoint with resilience policies
/// </summary>
public class ServiceEndpoint
{
    /// <summary>
    /// Base URL for the service
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// HTTP client timeout in seconds
    /// </summary>
    public int TimeoutInSeconds { get; set; } = 30;

    /// <summary>
    /// Number of retry attempts for transient failures
    /// </summary>
    public int RetryCount { get; set; } = 3;

    /// <summary>
    /// Circuit breaker failure threshold before opening
    /// </summary>
    public int CircuitBreakerThreshold { get; set; } = 5;

    /// <summary>
    /// Circuit breaker duration in seconds when open
    /// </summary>
    public int CircuitBreakerDurationInSeconds { get; set; } = 30;

    /// <summary>
    /// Enable or disable retry policy
    /// </summary>
    public bool EnableRetryPolicy { get; set; } = true;

    /// <summary>
    /// Enable or disable circuit breaker policy
    /// </summary>
    public bool EnableCircuitBreaker { get; set; } = true;

    /// <summary>
    /// Base delay for exponential backoff in milliseconds
    /// </summary>
    public int RetryBaseDelayMs { get; set; } = 1000;

    /// <summary>
    /// Maximum jitter in milliseconds for retry delay
    /// </summary>
    public int RetryJitterMs { get; set; } = 100;

    /// <summary>
    /// Gets the configured timeout as TimeSpan
    /// </summary>
    public TimeSpan Timeout => TimeSpan.FromSeconds(TimeoutInSeconds);

    /// <summary>
    /// Gets the circuit breaker duration as TimeSpan
    /// </summary>
    public TimeSpan CircuitBreakerDuration => TimeSpan.FromSeconds(CircuitBreakerDurationInSeconds);

    /// <summary>
    /// Gets the base retry delay as TimeSpan
    /// </summary>
    public TimeSpan RetryBaseDelay => TimeSpan.FromMilliseconds(RetryBaseDelayMs);
}
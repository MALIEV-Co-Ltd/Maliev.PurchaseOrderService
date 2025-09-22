namespace Maliev.PurchaseOrderService.Api.Configuration;

/// <summary>
/// Configuration options for health checks
/// </summary>
public class HealthCheckConfigOptions
{
    public const string SectionName = "HealthChecks";

    /// <summary>
    /// Enable detailed error information in health check responses
    /// </summary>
    public bool EnableDetailedErrors { get; set; } = false;

    /// <summary>
    /// Health check timeout in seconds
    /// </summary>
    public int TimeoutInSeconds { get; set; } = 30;

    /// <summary>
    /// Database health check timeout in seconds
    /// </summary>
    public int DatabaseTimeoutInSeconds { get; set; } = 15;

    /// <summary>
    /// External service health check timeout in seconds
    /// </summary>
    public int ExternalServiceTimeoutInSeconds { get; set; } = 10;

    /// <summary>
    /// Memory health check warning threshold in MB
    /// </summary>
    public long MemoryWarningThresholdMB { get; set; } = 1024;

    /// <summary>
    /// Memory health check critical threshold in MB
    /// </summary>
    public long MemoryCriticalThresholdMB { get; set; } = 2048;

    /// <summary>
    /// Disk space health check warning threshold in GB
    /// </summary>
    public long DiskSpaceWarningThresholdGB { get; set; } = 5;

    /// <summary>
    /// Disk space health check critical threshold in GB
    /// </summary>
    public long DiskSpaceCriticalThresholdGB { get; set; } = 1;

    /// <summary>
    /// Enable cache health checks
    /// </summary>
    public bool EnableCacheHealthChecks { get; set; } = true;

    /// <summary>
    /// Enable external service health checks
    /// </summary>
    public bool EnableExternalServiceHealthChecks { get; set; } = true;

    /// <summary>
    /// Enable database health checks
    /// </summary>
    public bool EnableDatabaseHealthChecks { get; set; } = true;

    /// <summary>
    /// Enable system resource health checks
    /// </summary>
    public bool EnableSystemResourceHealthChecks { get; set; } = true;

    /// <summary>
    /// Health check cache duration in seconds
    /// </summary>
    public int CacheDurationInSeconds { get; set; } = 30;

    /// <summary>
    /// Health check failure threshold before marking as unhealthy
    /// </summary>
    public int FailureThreshold { get; set; } = 3;

    /// <summary>
    /// Health check evaluation period in seconds
    /// </summary>
    public int EvaluationPeriodInSeconds { get; set; } = 60;

    /// <summary>
    /// Gets the timeout as TimeSpan
    /// </summary>
    public TimeSpan Timeout => TimeSpan.FromSeconds(TimeoutInSeconds);

    /// <summary>
    /// Gets the database timeout as TimeSpan
    /// </summary>
    public TimeSpan DatabaseTimeout => TimeSpan.FromSeconds(DatabaseTimeoutInSeconds);

    /// <summary>
    /// Gets the external service timeout as TimeSpan
    /// </summary>
    public TimeSpan ExternalServiceTimeout => TimeSpan.FromSeconds(ExternalServiceTimeoutInSeconds);

    /// <summary>
    /// Gets the cache duration as TimeSpan
    /// </summary>
    public TimeSpan CacheDuration => TimeSpan.FromSeconds(CacheDurationInSeconds);

    /// <summary>
    /// Gets the evaluation period as TimeSpan
    /// </summary>
    public TimeSpan EvaluationPeriod => TimeSpan.FromSeconds(EvaluationPeriodInSeconds);
}
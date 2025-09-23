using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Maliev.PurchaseOrderService.Api.Configuration;
using Maliev.PurchaseOrderService.Api.ExternalServices;

namespace Maliev.PurchaseOrderService.Api.Services;

/// <summary>
/// Health check for external service dependencies
/// Verifies connectivity to SupplierService, OrderService, CurrencyService, UploadService, and PdfService
/// </summary>
public class ExternalServicesHealthCheck : IHealthCheck
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ExternalServiceOptions _externalServiceOptions;
    private readonly ILogger<ExternalServicesHealthCheck> _logger;

    public ExternalServicesHealthCheck(
        IHttpClientFactory httpClientFactory,
        IOptions<ExternalServiceOptions> externalServiceOptions,
        ILogger<ExternalServicesHealthCheck> logger)
    {
        _httpClientFactory = httpClientFactory;
        _externalServiceOptions = externalServiceOptions.Value;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var results = new Dictionary<string, (bool isHealthy, string message, TimeSpan responseTime)>();

        // Check each external service
        await CheckServiceHealth("SupplierService", _externalServiceOptions.SupplierService, results, cancellationToken);
        await CheckServiceHealth("OrderService", _externalServiceOptions.OrderService, results, cancellationToken);
        await CheckServiceHealth("CurrencyService", _externalServiceOptions.CurrencyService, results, cancellationToken);
        await CheckServiceHealth("UploadService", _externalServiceOptions.UploadService, results, cancellationToken);
        await CheckServiceHealth("PdfService", _externalServiceOptions.PdfService, results, cancellationToken);

        // Determine overall health
        var healthyServices = results.Count(r => r.Value.isHealthy);
        var totalServices = results.Count;
        var healthyPercentage = (double)healthyServices / totalServices;

        var data = results.ToDictionary(
            kvp => kvp.Key,
            kvp => (object)new
            {
                IsHealthy = kvp.Value.isHealthy,
                Message = kvp.Value.message,
                ResponseTimeMs = kvp.Value.responseTime.TotalMilliseconds
            });

        // Add summary data
        data.Add("Summary", new
        {
            HealthyServices = healthyServices,
            TotalServices = totalServices,
            HealthyPercentage = Math.Round(healthyPercentage * 100, 2)
        });

        // Determine result based on healthy percentage
        if (healthyPercentage >= 1.0)
        {
            return new HealthCheckResult(HealthStatus.Healthy, $"All {totalServices} external services are healthy", data: data);
        }
        else if (healthyPercentage >= 0.5)
        {
            return new HealthCheckResult(HealthStatus.Degraded, $"{healthyServices}/{totalServices} external services are healthy", data: data);
        }
        else
        {
            return new HealthCheckResult(HealthStatus.Unhealthy, $"Only {healthyServices}/{totalServices} external services are healthy", data: data);
        }
    }

    private async Task CheckServiceHealth(
        string serviceName,
        ServiceEndpoint serviceConfig,
        Dictionary<string, (bool isHealthy, string message, TimeSpan responseTime)> results,
        CancellationToken cancellationToken)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            if (string.IsNullOrEmpty(serviceConfig.BaseUrl))
            {
                results[serviceName] = (false, "Base URL not configured", stopwatch.Elapsed);
                return;
            }

            using var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(5); // Short timeout for health checks

            // Try to call the health endpoint, fallback to base URL
            var healthUrl = $"{serviceConfig.BaseUrl.TrimEnd('/')}/health";
            var baseUrl = serviceConfig.BaseUrl.TrimEnd('/');

            HttpResponseMessage response;
            try
            {
                response = await httpClient.GetAsync(healthUrl, cancellationToken);
            }
            catch
            {
                // If health endpoint fails, try base URL
                response = await httpClient.GetAsync(baseUrl, cancellationToken);
            }

            stopwatch.Stop();

            if (response.IsSuccessStatusCode)
            {
                results[serviceName] = (true, $"HTTP {(int)response.StatusCode} {response.StatusCode}", stopwatch.Elapsed);
            }
            else
            {
                results[serviceName] = (false, $"HTTP {(int)response.StatusCode} {response.StatusCode}", stopwatch.Elapsed);
            }
        }
        catch (TaskCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();
            results[serviceName] = (false, "Health check cancelled", stopwatch.Elapsed);
        }
        catch (TaskCanceledException)
        {
            stopwatch.Stop();
            results[serviceName] = (false, "Request timeout", stopwatch.Elapsed);
        }
        catch (HttpRequestException ex)
        {
            stopwatch.Stop();
            results[serviceName] = (false, $"HTTP error: {ex.Message}", stopwatch.Elapsed);
            _logger.LogWarning(ex, "Health check failed for {ServiceName}", serviceName);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            results[serviceName] = (false, $"Error: {ex.Message}", stopwatch.Elapsed);
            _logger.LogError(ex, "Unexpected error during health check for {ServiceName}", serviceName);
        }
    }
}
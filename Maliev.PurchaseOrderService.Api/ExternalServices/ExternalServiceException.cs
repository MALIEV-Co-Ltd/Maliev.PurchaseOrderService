namespace Maliev.PurchaseOrderService.Api.ExternalServices;

/// <summary>
/// Exception thrown when external service communication fails
/// </summary>
public class ExternalServiceException : Exception
{
    public string ServiceName { get; }
    public string? ErrorCode { get; set; }
    public string? AdditionalContext { get; set; }

    public ExternalServiceException(string message) : base(message)
    {
        ServiceName = "Unknown";
    }

    public ExternalServiceException(string message, Exception innerException) : base(message, innerException)
    {
        ServiceName = "Unknown";
    }

    public ExternalServiceException(string serviceName, string message) : base(message)
    {
        ServiceName = serviceName;
    }

    public ExternalServiceException(string serviceName, string message, Exception innerException) : base(message, innerException)
    {
        ServiceName = serviceName;
    }

    public ExternalServiceException(string serviceName, string message, string? errorCode, string? additionalContext = null) : base(message)
    {
        ServiceName = serviceName;
        ErrorCode = errorCode;
        AdditionalContext = additionalContext;
    }

    public ExternalServiceException(string serviceName, string message, Exception innerException, string? errorCode, string? additionalContext = null) : base(message, innerException)
    {
        ServiceName = serviceName;
        ErrorCode = errorCode;
        AdditionalContext = additionalContext;
    }
}
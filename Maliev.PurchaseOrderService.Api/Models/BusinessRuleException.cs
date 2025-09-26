namespace Maliev.PurchaseOrderService.Api.Models;

/// <summary>
/// Exception thrown when business rules are violated
/// Maps to HTTP 409 Conflict for state transition issues
/// </summary>
public class BusinessRuleException : Exception
{
    public string ErrorCode { get; }
    public string? AdditionalContext { get; set; }

    public BusinessRuleException(string message, string errorCode) : base(message)
    {
        ErrorCode = errorCode;
    }

    public BusinessRuleException(string message, string errorCode, Exception innerException) : base(message, innerException)
    {
        ErrorCode = errorCode;
    }

    public BusinessRuleException(string message, string errorCode, string? additionalContext = null) : base(message)
    {
        ErrorCode = errorCode;
        AdditionalContext = additionalContext;
    }
}
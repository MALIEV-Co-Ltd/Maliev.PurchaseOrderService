namespace Maliev.PurchaseOrderService.Api.DTOs;

/// <summary>
/// Result of validation operation
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// Whether validation passed
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// List of validation errors
    /// </summary>
    public List<ValidationError> Errors { get; set; } = new();

    /// <summary>
    /// List of validation warnings
    /// </summary>
    public List<ValidationWarning> Warnings { get; set; } = new();

    /// <summary>
    /// Additional validation context
    /// </summary>
    public Dictionary<string, object>? Context { get; set; }

    /// <summary>
    /// Validation execution timestamp
    /// </summary>
    public DateTime ValidatedAt { get; set; }

    /// <summary>
    /// Time taken for validation
    /// </summary>
    public TimeSpan ValidationTime { get; set; }

    /// <summary>
    /// Creates a successful validation result
    /// </summary>
    /// <returns>Valid result</returns>
    public static ValidationResult Success()
    {
        return new ValidationResult
        {
            IsValid = true,
            ValidatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a failed validation result with errors
    /// </summary>
    /// <param name="errors">Validation errors</param>
    /// <returns>Invalid result</returns>
    public static ValidationResult Failure(params ValidationError[] errors)
    {
        return new ValidationResult
        {
            IsValid = false,
            Errors = errors.ToList(),
            ValidatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a failed validation result with error messages
    /// </summary>
    /// <param name="errorMessages">Error messages</param>
    /// <returns>Invalid result</returns>
    public static ValidationResult Failure(params string[] errorMessages)
    {
        var errors = errorMessages.Select(msg => new ValidationError
        {
            Field = "General",
            Message = msg,
            Code = "VALIDATION_ERROR"
        }).ToArray();

        return Failure(errors);
    }
}

/// <summary>
/// Validation error details
/// </summary>
public class ValidationError
{
    /// <summary>
    /// Field or property name that failed validation
    /// </summary>
    public string Field { get; set; } = string.Empty;

    /// <summary>
    /// Error message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Error code for programmatic handling
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Current value that failed validation
    /// </summary>
    public object? Value { get; set; }

    /// <summary>
    /// Additional error context
    /// </summary>
    public Dictionary<string, object>? Context { get; set; }
}

/// <summary>
/// Validation warning details
/// </summary>
public class ValidationWarning
{
    /// <summary>
    /// Field or property name for the warning
    /// </summary>
    public string Field { get; set; } = string.Empty;

    /// <summary>
    /// Warning message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Warning code for programmatic handling
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Current value that triggered the warning
    /// </summary>
    public object? Value { get; set; }

    /// <summary>
    /// Additional warning context
    /// </summary>
    public Dictionary<string, object>? Context { get; set; }
}
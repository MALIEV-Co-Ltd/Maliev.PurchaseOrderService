namespace Maliev.PurchaseOrderService.Api.Configuration;

/// <summary>
/// Configuration options for JWT authentication
/// </summary>
public class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>
    /// JWT security key
    /// </summary>
    public string SecurityKey { get; set; } = "test-signing-key-that-is-at-least-32-characters-long-for-testing-purposes";

    /// <summary>
    /// JWT issuer
    /// </summary>
    public string Issuer { get; set; } = "test-issuer";

    /// <summary>
    /// JWT audience
    /// </summary>
    public string Audience { get; set; } = "test-audience";

    /// <summary>
    /// JWT expiration time in minutes
    /// </summary>
    public int ExpirationMinutes { get; set; } = 60;

    /// <summary>
    /// Validate JWT issuer
    /// </summary>
    public bool ValidateIssuer { get; set; } = true;

    /// <summary>
    /// Validate JWT audience
    /// </summary>
    public bool ValidateAudience { get; set; } = true;

    /// <summary>
    /// Validate JWT lifetime
    /// </summary>
    public bool ValidateLifetime { get; set; } = true;

    /// <summary>
    /// Validate JWT signing key
    /// </summary>
    public bool ValidateIssuerSigningKey { get; set; } = true;

    /// <summary>
    /// Clock skew tolerance (format: HH:MM:SS)
    /// </summary>
    public string ClockSkew { get; set; } = "00:05:00";

    /// <summary>
    /// Require JWT expiration
    /// </summary>
    public bool RequireExpirationTime { get; set; } = true;

    /// <summary>
    /// Require signed tokens
    /// </summary>
    public bool RequireSignedTokens { get; set; } = true;

    /// <summary>
    /// Save tokens in authentication properties
    /// </summary>
    public bool SaveToken { get; set; } = false;

    /// <summary>
    /// Include error details in challenge responses
    /// </summary>
    public bool IncludeErrorDetails { get; set; } = false;

    /// <summary>
    /// JWT token type
    /// </summary>
    public string TokenType { get; set; } = "Bearer";

    /// <summary>
    /// Gets the expiration time as TimeSpan
    /// </summary>
    public TimeSpan ExpirationTimeSpan => TimeSpan.FromMinutes(ExpirationMinutes);

    /// <summary>
    /// Gets the clock skew as TimeSpan
    /// </summary>
    public TimeSpan ClockSkewTimeSpan
    {
        get
        {
            if (TimeSpan.TryParse(ClockSkew, out var timeSpan))
                return timeSpan;
            return TimeSpan.FromMinutes(5); // Default fallback
        }
    }

    /// <summary>
    /// Validates the configuration
    /// </summary>
    public bool IsValid()
    {
        return !string.IsNullOrEmpty(SecurityKey) &&
               !string.IsNullOrEmpty(Issuer) &&
               !string.IsNullOrEmpty(Audience) &&
               ExpirationMinutes > 0 &&
               SecurityKey.Length >= 32; // Minimum key length for security
    }

    /// <summary>
    /// Gets validation error messages
    /// </summary>
    public IEnumerable<string> GetValidationErrors()
    {
        var errors = new List<string>();

        if (string.IsNullOrEmpty(SecurityKey))
            errors.Add("JWT SecurityKey is required");
        else if (SecurityKey.Length < 32)
            errors.Add("JWT SecurityKey must be at least 32 characters long");

        if (string.IsNullOrEmpty(Issuer))
            errors.Add("JWT Issuer is required");

        if (string.IsNullOrEmpty(Audience))
            errors.Add("JWT Audience is required");

        if (ExpirationMinutes <= 0)
            errors.Add("JWT ExpirationMinutes must be greater than 0");

        if (!TimeSpan.TryParse(ClockSkew, out _))
            errors.Add("JWT ClockSkew must be in valid TimeSpan format (HH:MM:SS)");

        return errors;
    }
}
namespace Maliev.PurchaseOrderService.Api.Configuration;

/// <summary>
/// Configuration options for security settings
/// </summary>
public class SecurityOptions
{
    public const string SectionName = "Security";

    /// <summary>
    /// Require HTTPS for all requests
    /// </summary>
    public bool RequireHttps { get; set; } = true;

    /// <summary>
    /// JWT authentication settings
    /// </summary>
    public JwtSecuritySettings JwtSettings { get; set; } = new();

    /// <summary>
    /// CORS settings
    /// </summary>
    public CorsSettings Cors { get; set; } = new();

    /// <summary>
    /// API rate limiting settings
    /// </summary>
    public RateLimitingSettings RateLimiting { get; set; } = new();

    /// <summary>
    /// Content security policy settings
    /// </summary>
    public ContentSecurityPolicySettings ContentSecurityPolicy { get; set; } = new();

    /// <summary>
    /// Data protection settings
    /// </summary>
    public DataProtectionSettings DataProtection { get; set; } = new();
}

/// <summary>
/// JWT security configuration
/// </summary>
public class JwtSecuritySettings
{
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
    /// Clock skew tolerance in minutes
    /// </summary>
    public int ClockSkewMinutes { get; set; } = 5;

    /// <summary>
    /// Require JWT expiration
    /// </summary>
    public bool RequireExpirationTime { get; set; } = true;

    /// <summary>
    /// Require signed tokens
    /// </summary>
    public bool RequireSignedTokens { get; set; } = true;

    /// <summary>
    /// Gets the clock skew as TimeSpan
    /// </summary>
    public TimeSpan ClockSkew => TimeSpan.FromMinutes(ClockSkewMinutes);
}

/// <summary>
/// CORS security configuration
/// </summary>
public class CorsSettings
{
    /// <summary>
    /// Allowed origins for CORS
    /// </summary>
    public string[] AllowedOrigins { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Allowed HTTP methods
    /// </summary>
    public string[] AllowedMethods { get; set; } = new[] { "GET", "POST", "PUT", "DELETE", "PATCH", "OPTIONS" };

    /// <summary>
    /// Allowed headers
    /// </summary>
    public string[] AllowedHeaders { get; set; } = new[] { "Authorization", "Content-Type", "X-Api-Version" };

    /// <summary>
    /// Allow credentials in CORS requests
    /// </summary>
    public bool AllowCredentials { get; set; } = true;

    /// <summary>
    /// Preflight cache duration in seconds
    /// </summary>
    public int PreflightMaxAgeSeconds { get; set; } = 86400; // 24 hours
}

/// <summary>
/// Rate limiting security configuration
/// </summary>
public class RateLimitingSettings
{
    /// <summary>
    /// Global rate limit per minute
    /// </summary>
    public int GlobalLimitPerMinute { get; set; } = 1000;

    /// <summary>
    /// Per-user rate limit per minute
    /// </summary>
    public int UserLimitPerMinute { get; set; } = 100;

    /// <summary>
    /// Upload endpoint rate limit per minute
    /// </summary>
    public int UploadLimitPerMinute { get; set; } = 10;

    /// <summary>
    /// API endpoint rate limit per minute
    /// </summary>
    public int ApiLimitPerMinute { get; set; } = 300;

    /// <summary>
    /// Enable rate limiting
    /// </summary>
    public bool EnableRateLimiting { get; set; } = true;

    /// <summary>
    /// Rate limit window duration in minutes
    /// </summary>
    public int WindowDurationMinutes { get; set; } = 1;
}

/// <summary>
/// Content Security Policy configuration
/// </summary>
public class ContentSecurityPolicySettings
{
    /// <summary>
    /// Enable Content Security Policy
    /// </summary>
    public bool EnableCSP { get; set; } = true;

    /// <summary>
    /// CSP report-only mode
    /// </summary>
    public bool ReportOnlyMode { get; set; } = false;

    /// <summary>
    /// CSP report URI
    /// </summary>
    public string? ReportUri { get; set; }

    /// <summary>
    /// Default source policy
    /// </summary>
    public string DefaultSrc { get; set; } = "'self'";

    /// <summary>
    /// Script source policy
    /// </summary>
    public string ScriptSrc { get; set; } = "'self' 'unsafe-inline'";

    /// <summary>
    /// Style source policy
    /// </summary>
    public string StyleSrc { get; set; } = "'self' 'unsafe-inline'";

    /// <summary>
    /// Image source policy
    /// </summary>
    public string ImgSrc { get; set; } = "'self' data: https:";

    /// <summary>
    /// Font source policy
    /// </summary>
    public string FontSrc { get; set; } = "'self'";

    /// <summary>
    /// Connect source policy
    /// </summary>
    public string ConnectSrc { get; set; } = "'self'";
}

/// <summary>
/// Data protection configuration
/// </summary>
public class DataProtectionSettings
{
    /// <summary>
    /// Enable data protection
    /// </summary>
    public bool EnableDataProtection { get; set; } = true;

    /// <summary>
    /// Application name for data protection
    /// </summary>
    public string ApplicationName { get; set; } = "PurchaseOrderService";

    /// <summary>
    /// Key lifetime in days
    /// </summary>
    public int KeyLifetimeDays { get; set; } = 90;

    /// <summary>
    /// Enable automatic key rotation
    /// </summary>
    public bool EnableAutomaticKeyRotation { get; set; } = true;
}
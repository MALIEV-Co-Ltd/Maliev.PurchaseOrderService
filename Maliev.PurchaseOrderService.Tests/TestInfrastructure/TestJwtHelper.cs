using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Maliev.PurchaseOrderService.Api.Configuration;

namespace Maliev.PurchaseOrderService.Tests.TestInfrastructure;

/// <summary>
/// Helper class for generating JWT tokens in tests and configuring test authentication
/// </summary>
public static class TestJwtHelper
{
    private const string TestSigningKey = "test-signing-key-that-is-at-least-32-characters-long-for-testing-purposes";
    private const string TestIssuer = "test-issuer";
    private const string TestAudience = "test-audience";

    /// <summary>
    /// Generates a test JWT token with the specified claims
    /// </summary>
    /// <param name="userId">The user ID claim</param>
    /// <param name="role">The role claim (Employee, Manager, Procurement, Admin)</param>
    /// <param name="department">The department claim</param>
    /// <param name="additionalClaims">Additional claims to include</param>
    /// <returns>A valid JWT token string</returns>
    public static string GenerateTestToken(
        string userId,
        string role,
        string department = "test-department",
        Dictionary<string, string>? additionalClaims = null)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(TestSigningKey);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Name, userId),
            new(ClaimTypes.Role, role),
            new("department", department),
            new("sub", userId),
            new("jti", Guid.NewGuid().ToString())
        };

        // Add any additional claims
        if (additionalClaims != null)
        {
            foreach (var claim in additionalClaims)
            {
                claims.Add(new Claim(claim.Key, claim.Value));
            }
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(1),
            Issuer = TestIssuer,
            Audience = TestAudience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    /// <summary>
    /// Generates a test token for an Employee role
    /// </summary>
    public static string GenerateEmployeeToken(string userId = "emp123", string department = "department1")
    {
        return GenerateTestToken(userId, "Employee", department);
    }

    /// <summary>
    /// Generates a test token for a Manager role
    /// </summary>
    public static string GenerateManagerToken(string userId = "mgr123", string department = "department1")
    {
        return GenerateTestToken(userId, "Manager", department);
    }

    /// <summary>
    /// Generates a test token for a Procurement role
    /// </summary>
    public static string GenerateProcurementToken(string userId = "proc123", string department = "procurement")
    {
        return GenerateTestToken(userId, "Procurement", department);
    }

    /// <summary>
    /// Generates a test token for an Admin role
    /// </summary>
    public static string GenerateAdminToken(string userId = "admin123", string department = "admin")
    {
        return GenerateTestToken(userId, "Admin", department);
    }

    /// <summary>
    /// Generates an invalid/expired token for testing unauthorized scenarios
    /// </summary>
    public static string GenerateExpiredToken(string userId = "expired123", string role = "Employee")
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(TestSigningKey);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Name, userId),
            new(ClaimTypes.Role, role),
            new("sub", userId)
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(-1), // Expired token
            Issuer = TestIssuer,
            Audience = TestAudience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    /// <summary>
    /// Configures test authentication services for integration tests
    /// </summary>
    public static void ConfigureTestAuthentication(IServiceCollection services, IConfiguration configuration)
    {
        // Configure JWT options for testing
        services.Configure<JwtOptions>(options =>
        {
            options.SecurityKey = TestSigningKey;
            options.Issuer = TestIssuer;
            options.Audience = TestAudience;
            options.ValidateIssuer = true;
            options.ValidateAudience = true;
            options.ValidateLifetime = true;
            options.ValidateIssuerSigningKey = true;
            options.ExpirationMinutes = 60;
            options.ClockSkew = "00:05:00";
        });
    }

    /// <summary>
    /// Gets test JWT configuration for manual setup
    /// </summary>
    public static JwtOptions GetTestJwtOptions()
    {
        return new JwtOptions
        {
            SecurityKey = TestSigningKey,
            Issuer = TestIssuer,
            Audience = TestAudience,
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ExpirationMinutes = 60,
            ClockSkew = "00:05:00"
        };
    }
}
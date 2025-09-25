namespace Maliev.PurchaseOrderService.Api.Extensions;

/// <summary>
/// Extensions for authentication middleware
/// </summary>
public static class AuthenticationExtensions
{
    /// <summary>
    /// Middleware to add WWW-Authenticate header for 401 responses
    /// </summary>
    public static IApplicationBuilder UseAuthenticationHeaders(this IApplicationBuilder builder)
    {
        return builder.Use(async (context, next) =>
        {
            await next();

            // Add WWW-Authenticate header for 401 responses
            if (context.Response.StatusCode == 401)
            {
                if (!context.Response.Headers.ContainsKey("WWW-Authenticate"))
                {
                    context.Response.Headers["WWW-Authenticate"] = "Bearer";
                }
            }
        });
    }
}
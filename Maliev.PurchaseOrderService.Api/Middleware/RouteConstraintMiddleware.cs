using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Maliev.PurchaseOrderService.Api.Middleware;

/// <summary>
/// Middleware to convert route constraint failures to BadRequest responses
/// This handles cases where invalid parameter formats (like "invalid" for int parameters)
/// would normally return 404 but should return 400 according to API contracts
/// </summary>
public class RouteConstraintMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RouteConstraintMiddleware> _logger;

    public RouteConstraintMiddleware(RequestDelegate next, ILogger<RouteConstraintMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        await _next(context);

        // Check if we got a 404 that should be a 400 due to route constraint failure
        if (context.Response.StatusCode == 404 && ShouldConvertTo400(context)) {
        
            // Only convert if response hasn't started
            if (context.Response.HasStarted)
                return;
        
            _logger.LogInformation("Converting 404 to 400 for route constraint failure: {Path}", context.Request.Path);

            context.Response.Clear();
            context.Response.StatusCode = 400;
            context.Response.ContentType = "application/json";

            var problemDetails = new ValidationProblemDetails
            {
                Title = "Invalid request parameters",
                Status = 400,
                Detail = "One or more request parameters have invalid format",
                Instance = context.Request.Path
            };

            var json = JsonSerializer.Serialize(problemDetails, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(json);
        }
    }

    private static bool ShouldConvertTo400(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant();
        if (string.IsNullOrEmpty(path))
            return false;

        // Check if this is an API endpoint that likely failed due to route constraint
        // Look for patterns like /v1.0/purchase-orders/invalid/... where "invalid" should be an integer
        if (path.Contains("/purchase-orders/") &&
            (path.Contains("/orderitems") ||
             path.Contains("/purchaseorderfiles") ||
             path.Contains("/approve") ||
             path.Contains("/cancel") ||
             path.EndsWith("/purchase-orders/invalid") ||
             path.Contains("/purchase-orders/invalid-")))
        {
            return true;
        }

        return false;
    }
}

/// <summary>
/// Extension method to register the RouteConstraintMiddleware
/// </summary>
public static class RouteConstraintMiddlewareExtensions
{
    public static IApplicationBuilder UseRouteConstraintValidation(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RouteConstraintMiddleware>();
    }
}
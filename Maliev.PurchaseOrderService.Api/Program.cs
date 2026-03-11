using Maliev.PurchaseOrderService.Infrastructure.Persistence;
using Maliev.PurchaseOrderService.Domain.Entities;
using Maliev.Aspire.ServiceDefaults;
using Maliev.PurchaseOrderService.Api.Extensions;
using Maliev.PurchaseOrderService.Application;
using Maliev.PurchaseOrderService.Application.Interfaces;
using Maliev.PurchaseOrderService.Infrastructure;
using Maliev.PurchaseOrderService.Infrastructure.Services;
using Maliev.PurchaseOrderService.Api.ExternalServices;
using Microsoft.EntityFrameworkCore;
using MassTransit;
using ApiServices = Maliev.PurchaseOrderService.Api.Services;

// Initialize bootstrap logging
using var loggerFactory = LoggerFactory.Create(logBuilder => logBuilder.AddConsole());
var bootstrapLogger = loggerFactory.CreateLogger("Program");

try
{
    Log.StartingHost(bootstrapLogger, "Purchase Order Service");

    var builder = WebApplication.CreateBuilder(args);

    // --- Secrets & Configuration ---
    builder.AddGoogleSecretManagerVolume(); // Load secrets from /mnt/secrets if available

    // --- Infrastructure & Observability ---
    builder.AddServiceDefaults(); // OpenTelemetry, health checks, resilience
    builder.AddStandardMiddleware(options =>
    {
        options.EnableRequestLogging = true;
    });
    builder.AddServiceMeters("purchase-orders-meter"); // Register service meters

    // Add Redis Distributed Cache
    builder.AddStandardCache("purchase-order:");

    // Add MassTransit with RabbitMQ
    builder.AddMassTransitWithRabbitMq();

    // Add PostgreSQL DbContext
    builder.AddPostgresDbContext<PurchaseOrderContext>(connectionName: "PurchaseOrderDbContext", configureOptions: options =>
    {
        options.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        options.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.LazyLoadOnDisposedContextWarning));
    });

    // --- API Configuration ---
    builder.AddStandardCors(); // CORS with fail-fast validation
    builder.AddDefaultApiVersioning(); // API versioning with URL segment reader

    // JWT Authentication (tests override via PostConfigureAll with dynamic RSA keys)
    builder.AddJwtAuthentication();

    // --- Authorization & Permissions ---
    builder.Services.AddPermissionAuthorization();

    // --- Layer Registration ---
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    // Register application services
    builder.Services.AddScoped<IPurchaseOrderService, PurchaseOrderServiceImpl>();

    // Additional API Services
    builder.Services.AddScoped<IUserPermissionService, UserPermissionService>();
    builder.Services.AddScoped<Maliev.PurchaseOrderService.Application.Interfaces.IAuditLogService, Maliev.PurchaseOrderService.Api.Services.AuditLogService>();
    builder.Services.AddScoped<Maliev.PurchaseOrderService.Application.Interfaces.IWHTCalculationService, Maliev.PurchaseOrderService.Api.Services.WHTCalculationService>();

    // External Service Clients
    builder.Services.AddScoped<Maliev.PurchaseOrderService.Application.Interfaces.ISupplierServiceClient, Maliev.PurchaseOrderService.Api.ExternalServices.SupplierServiceClient>();
    builder.Services.AddScoped<Maliev.PurchaseOrderService.Application.Interfaces.IOrderServiceClient, Maliev.PurchaseOrderService.Api.ExternalServices.OrderServiceClient>();
    builder.Services.AddScoped<Maliev.PurchaseOrderService.Application.Interfaces.ICurrencyServiceClient, Maliev.PurchaseOrderService.Api.ExternalServices.CurrencyServiceClient>();

    // MassTransit IPublishEndpoint is auto-registered by MassTransit

    // Add OpenAPI (must be in Program.cs for XML comments to work via source generator)
    if (!builder.Environment.IsProduction())
    {
        builder.AddStandardOpenApi(
            title: "MALIEV Purchase Order Service API",
            description: "Purchase order management service.");
    }

    // IAM Registration
    builder.AddIAMServiceClient("purchase-order");
    builder.Services.AddIAMRegistration<ApiServices.PurchaseOrderIAMRegistrationService>("purchase-order");

    builder.Services.AddControllers();
    builder.Services.AddMemoryCache();

    builder.AddStandardRateLimiting();

    var app = builder.Build();
    var logger = app.Services.GetRequiredService<ILogger<Program>>();

    // Run database migrations on startup
    await app.MigrateDatabaseAsync<PurchaseOrderContext>();

    // Configure middleware pipeline
    app.UseStandardMiddleware();

    if (!app.Environment.IsDevelopment())
    {
        app.UseHttpsRedirection();
    }

    app.UseCors();
    app.UseAuthentication();
    app.UseAuthorization();

    // Map endpoints
    app.MapControllers();

    // Map Aspire default endpoints (/health, /alive, /metrics)
    app.MapDefaultEndpoints(servicePrefix: "purchase-order");

    // Map OpenAPI and Scalar documentation (dev/staging only)
    app.MapApiDocumentation(servicePrefix: "purchase-order");

    Log.ServiceStarted(logger, "Purchase Order Service");
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.HostTerminated(bootstrapLogger, ex, "Purchase Order Service");
    throw;
}
finally
{
    loggerFactory.Dispose();
}

/// <summary>
/// Main program class for the application
/// </summary>
public partial class Program
{
    internal static partial class Log
    {
        [LoggerMessage(Level = LogLevel.Information, Message = "Starting {ServiceName} host")]
        public static partial void StartingHost(ILogger logger, string serviceName);

        [LoggerMessage(Level = LogLevel.Critical, Message = "{ServiceName} host terminated unexpectedly during startup")]
        public static partial void HostTerminated(ILogger logger, Exception ex, string serviceName);

        [LoggerMessage(Level = LogLevel.Information, Message = "{ServiceName} started successfully")]
        public static partial void ServiceStarted(ILogger logger, string serviceName);
    }
}

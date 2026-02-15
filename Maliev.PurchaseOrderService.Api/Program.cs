using Maliev.PurchaseOrderService.Api.ExternalServices;
using Maliev.PurchaseOrderService.Api.Services;
using Maliev.PurchaseOrderService.Data;
using Microsoft.EntityFrameworkCore;
// Initialize bootstrap logging
using var loggerFactory = LoggerFactory.Create(logBuilder => logBuilder.AddConsole());
var bootstrapLogger = loggerFactory.CreateLogger("Program");

try
{
    Program.Log.StartingHost(bootstrapLogger, "Purchase Order Service");

    var builder = WebApplication.CreateBuilder(args);

    // --- Secrets & Configuration ---
    builder.AddGoogleSecretManagerVolume(); // Load secrets from /mnt/secrets if available

    // --- Infrastructure & Observability ---
    builder.AddServiceDefaults(); // OpenTelemetry, health checks, resilience
    builder.AddMassTransitWithRabbitMq(); // RabbitMQ messaging
    builder.AddServiceMeters("purchase-orders-meter"); // Register service meters for OpenTelemetry business metrics

    builder.AddStandardCache("purchase-order:"); // Redis + in-memory fallback, memory-optimized // Redis with in-memory fallback
    builder.AddPostgresDbContext<PurchaseOrderContext>(connectionName: "PurchaseOrderDbContext", configureOptions: options =>
    {
        options.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        options.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.LazyLoadOnDisposedContextWarning)); // Example of another one
    }); // PostgreSQL with retry logic

    // --- API Configuration ---
    builder.AddStandardCors(); // CORS with fail-fast validation
    builder.AddDefaultApiVersioning(); // API versioning with URL segment reader

    // JWT Authentication (tests override via PostConfigureAll with dynamic RSA keys)
    builder.AddJwtAuthentication();

    // Add OpenAPI (must be in Program.cs for XML comments to work via source generator)
    if (!builder.Environment.IsProduction())
    {
        builder.AddStandardOpenApi(
            title: "MALIEV Purchase Order Service API",
            description: "Purchase order management service. Supports PO creation with line items, approval workflows, status updates, search with pagination and filtering by supplier/status/date range, and cancellation with role-based access control.");
    }

    builder.Services.AddControllers();
    builder.Services.AddMemoryCache();

    // Rate Limiting
    builder.AddStandardRateLimiting(); // Memory-optimized for low-spec nodes
    // Configure HttpClients for external services
    builder.AddServiceClient("SupplierService");
    builder.AddServiceClient("OrderService");
    builder.AddServiceClient("CurrencyService");
    builder.AddServiceClient("UploadService");
    builder.AddServiceClient("PdfService");

    // Application Services
    builder.Services.AddScoped<IPurchaseOrderService, PurchaseOrderService>();
    builder.Services.AddScoped<IWHTCalculationService, WHTCalculationService>();
    builder.Services.AddScoped<IAuditLogService, AuditLogService>();
    builder.Services.AddScoped<IUserPermissionService, UserPermissionService>();

    // IAM Registration Service
    builder.AddIAMServiceClient("purchase-order");
    builder.Services.AddIAMRegistration<PurchaseOrderIAMRegistrationService>("purchase-order");

    // External Service Clients
    builder.Services.AddScoped<ISupplierServiceClient, SupplierServiceClient>();
    builder.Services.AddScoped<IOrderServiceClient, OrderServiceClient>();
    builder.Services.AddScoped<ICurrencyServiceClient, CurrencyServiceClient>();

    var app = builder.Build();
    var logger = app.Services.GetRequiredService<ILogger<Program>>();

    // --- Database Migrations ---
    await app.MigrateDatabaseAsync<PurchaseOrderContext>();

    // Middleware Pipeline
    if (!app.Environment.IsDevelopment())
    {
        app.UseHttpsRedirection();
    }
    app.UseRateLimiter();
    app.UseCors();

    app.UseAuthentication();
    app.UseAuthorization();

    // Map endpoints after middleware
    app.MapControllers();

    // Map Aspire default endpoints (/health, /alive, /metrics)
    app.MapDefaultEndpoints(servicePrefix: "purchase-order");

    // Map OpenAPI and Scalar documentation (dev/staging only)
    app.MapApiDocumentation(servicePrefix: "purchase-order");

    Program.Log.ServiceStarted(logger, "Purchase Order Service");
    await app.RunAsync();
}
catch (Exception ex)
{
    Program.Log.HostTerminated(bootstrapLogger, ex, "Purchase Order Service");
    throw;
}
finally
{
    loggerFactory.Dispose();
}

/// <summary>
/// Main program class for the Purchase Order Service API.
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

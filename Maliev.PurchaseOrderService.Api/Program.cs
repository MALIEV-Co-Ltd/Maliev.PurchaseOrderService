using Maliev.Aspire.ServiceDefaults;
using Microsoft.EntityFrameworkCore;
using Maliev.PurchaseOrderService.Api.ExternalServices;
using Maliev.PurchaseOrderService.Api.Services;
using Maliev.PurchaseOrderService.Data;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// --- Secrets & Configuration ---
builder.AddGoogleSecretManagerVolume(); // Load secrets from /mnt/secrets if available

// --- Infrastructure & Observability ---
builder.AddServiceDefaults(); // OpenTelemetry, health checks, resilience
builder.AddMassTransitWithRabbitMq(); // RabbitMQ messaging
builder.AddServiceMeters("purchase-orders-meter"); // Register service meters for OpenTelemetry business metrics

builder.AddRedisDistributedCache(instanceName: "purchase-order:"); // Redis with in-memory fallback
builder.AddPostgresDbContext<PurchaseOrderContext>(connectionName: "PurchaseOrderDbContext"); // PostgreSQL with retry logic

// --- API Configuration ---
builder.AddDefaultCors(); // CORS from CORS:AllowedOrigins config
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
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.User.Identity?.Name ?? context.Request.Headers.Host.ToString(),
            factory: partition => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 10
            }));
});

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

logger.LogInformation("PurchaseOrderService started successfully");
logger.LogInformation("Environment: {Environment}", builder.Environment.EnvironmentName);

await app.RunAsync();

/// <summary>
/// Main program class for the Purchase Order Service API.
/// </summary>
public partial class Program { }

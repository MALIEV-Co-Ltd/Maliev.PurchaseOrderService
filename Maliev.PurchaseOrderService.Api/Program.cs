using Maliev.PurchaseOrderService.Api.ExternalServices;
using Maliev.PurchaseOrderService.Api.Services;
using Maliev.PurchaseOrderService.Data;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// --- Secrets & Configuration ---
builder.AddGoogleSecretManagerVolume(); // Load secrets from /mnt/secrets if available

// --- Infrastructure & Observability ---
builder.AddServiceDefaults(); // OpenTelemetry, health checks, resilience
builder.AddServiceMeters("purchase-orders"); // Register service meters for OpenTelemetry business metrics

builder.AddRedisDistributedCache(instanceName: "PurchaseOrder:"); // Redis with in-memory fallback
builder.AddPostgresDbContext<PurchaseOrderContext>(connectionStringName: "PurchaseOrderDbContext"); // PostgreSQL with retry logic

// --- API Configuration ---
builder.AddDefaultCors(); // CORS from CORS:AllowedOrigins config
builder.AddDefaultApiVersioning(); // API versioning with URL segment reader

// JWT Authentication (tests override via PostConfigureAll with dynamic RSA keys)
builder.AddJwtAuthentication();

// Add OpenAPI (must be in Program.cs for XML comments to work via source generator)
if (!builder.Environment.IsProduction())
{
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddOpenApi("v1", options =>
    {
        options.AddDocumentTransformer((document, context, cancellationToken) =>
        {
            document.Info.Title = "MALIEV Purchase Order Service API";
            document.Info.Version = "v1";
            document.Info.Description = "Purchase order management service. Supports PO creation with line items, approval workflows, status updates, search with pagination and filtering by supplier/status/date range, and cancellation with role-based access control.";
            return Task.CompletedTask;
        });
    });
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

// Authorization Policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Employee", policy => policy.RequireRole("employee"));
    options.AddPolicy("Manager", policy => policy.RequireRole("manager", "procurement", "admin"));
    options.AddPolicy("Procurement", policy => policy.RequireRole("procurement", "admin"));
    options.AddPolicy("Admin", policy => policy.RequireRole("admin"));
});

// Configure HttpClients for external services
// Configure HttpClients for external services
builder.Services.AddHttpClient("SupplierService", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ExternalServices:SupplierService:BaseUrl"] ?? "http://localhost");
})
.AddStandardResilienceHandler();

builder.Services.AddHttpClient("OrderService", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ExternalServices:OrderService:BaseUrl"] ?? "http://localhost");
})
.AddStandardResilienceHandler();

builder.Services.AddHttpClient("CurrencyService", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ExternalServices:CurrencyService:BaseUrl"] ?? "http://localhost");
})
.AddStandardResilienceHandler();

builder.Services.AddHttpClient("UploadService", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ExternalServices:UploadService:BaseUrl"] ?? "http://localhost");
})
.AddStandardResilienceHandler();

builder.Services.AddHttpClient("PdfService", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ExternalServices:PdfService:BaseUrl"] ?? "http://localhost");
})
.AddStandardResilienceHandler();

// Application Services
builder.Services.AddScoped<IPurchaseOrderService, PurchaseOrderService>();
builder.Services.AddScoped<IWHTCalculationService, WHTCalculationService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();

// External Service Clients
builder.Services.AddScoped<ISupplierServiceClient, SupplierServiceClient>();
builder.Services.AddScoped<IOrderServiceClient, OrderServiceClient>();
builder.Services.AddScoped<ICurrencyServiceClient, CurrencyServiceClient>();

var app = builder.Build();
var logger = app.Services.GetRequiredService<ILogger<Program>>();

// Run database migrations on startup (skip in Testing environment)
if (!app.Environment.IsEnvironment("Testing"))
{
    try
    {
        await app.MigrateDatabaseAsync<PurchaseOrderContext>();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Database migration failed - application may not function correctly");
        // Don't throw - allow app to start for debugging
    }
}

// Middleware Pipeline
app.UseHttpsRedirection();
app.UseRateLimiter();
app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

// Map endpoints after middleware
app.MapControllers();

// Map Aspire default endpoints (/health, /alive, /metrics)
app.MapDefaultEndpoints(servicePrefix: "purchase-orders");

// Map OpenAPI and Scalar documentation (dev/staging only)
app.MapApiDocumentation(servicePrefix: "purchase-orders");

logger.LogInformation("PurchaseOrderService started successfully");
logger.LogInformation("Environment: {Environment}", builder.Environment.EnvironmentName);

await app.RunAsync();

/// <summary>
/// Main program class for the Purchase Order Service API.
/// </summary>
public partial class Program { }

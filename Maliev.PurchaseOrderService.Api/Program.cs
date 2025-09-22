using Asp.Versioning;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Reflection;
using System.Text;
using System.Threading.RateLimiting;
using Maliev.PurchaseOrderService.Data;
using Maliev.PurchaseOrderService.Api.Services;
using Maliev.PurchaseOrderService.Api.ExternalServices;
using Maliev.PurchaseOrderService.Api.MappingProfiles;
using Maliev.PurchaseOrderService.Api.Configuration;
using Polly;
using Polly.Extensions.Http;

var builder = WebApplication.CreateBuilder(args);

// Serilog Configuration (Console only)
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// Secrets from Google Secret Manager
var secretsPath = "/mnt/secrets";
if (Directory.Exists(secretsPath))
{
    builder.Configuration.AddKeyPerFile(directoryPath: secretsPath, optional: true);
}

// Configuration Options
builder.Services.Configure<ExternalServiceOptions>(
    builder.Configuration.GetSection(ExternalServiceOptions.SectionName));
builder.Services.Configure<ApplicationOptions>(
    builder.Configuration.GetSection(ApplicationOptions.SectionName));
builder.Services.Configure<CacheOptions>(
    builder.Configuration.GetSection(CacheOptions.SectionName));
builder.Services.Configure<SecurityOptions>(
    builder.Configuration.GetSection(SecurityOptions.SectionName));
builder.Services.Configure<HealthCheckConfigOptions>(
    builder.Configuration.GetSection(HealthCheckConfigOptions.SectionName));
builder.Services.Configure<JwtOptions>(
    builder.Configuration.GetSection(JwtOptions.SectionName));

// Add HTTP Context Accessor for JWT token forwarding
builder.Services.AddHttpContextAccessor();

// Database Configuration - Only register if not in Testing environment
if (!builder.Environment.IsEnvironment("Testing"))
{
    var connectionString = builder.Configuration.GetConnectionString("PurchaseOrderDbContext") ??
        builder.Configuration["ConnectionStrings__PurchaseOrderDbContext"] ??
        throw new InvalidOperationException("PurchaseOrderDbContext connection string is required");

    builder.Services.AddDbContext<PurchaseOrderContext>(options =>
    {
        options.UseNpgsql(connectionString, npgsqlOptions =>
        {
            npgsqlOptions.MigrationsAssembly("Maliev.PurchaseOrderService.Data");
            npgsqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorCodesToAdd: null);
        });

        if (builder.Environment.IsDevelopment())
        {
            options.EnableSensitiveDataLogging();
            options.EnableDetailedErrors();
        }
    });
}

// AutoMapper Configuration
builder.Services.AddAutoMapper(typeof(PurchaseOrderMappingProfile));

// Memory Cache (Simple configuration)
builder.Services.AddMemoryCache();

// HTTP Client Handler for JWT Authentication
builder.Services.AddTransient<AuthenticatedHttpClientHandler>();

// Helper methods for HTTP client policies with configuration support
static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(ServiceEndpoint serviceConfig)
{
    if (!serviceConfig.EnableRetryPolicy)
    {
        return Policy.NoOpAsync<HttpResponseMessage>();
    }

    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(
            retryCount: serviceConfig.RetryCount,
            sleepDurationProvider: retryAttempt =>
            {
                var baseDelay = serviceConfig.RetryBaseDelay;
                var jitter = TimeSpan.FromMilliseconds(Random.Shared.Next(0, serviceConfig.RetryJitterMs));
                return TimeSpan.FromMilliseconds(Math.Pow(2, retryAttempt)) + baseDelay + jitter;
            },
            onRetry: (outcome, timespan, retryCount, context) =>
            {
                Log.Warning("Retry {RetryCount} for {Operation} in {Delay}ms, Service: {Service}",
                    retryCount, context.OperationKey, timespan.TotalMilliseconds, context.GetValueOrDefault("ServiceName"));
            });
}

static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy(ServiceEndpoint serviceConfig, string serviceName)
{
    if (!serviceConfig.EnableCircuitBreaker)
    {
        return Policy.NoOpAsync<HttpResponseMessage>();
    }

    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .CircuitBreakerAsync(
            handledEventsAllowedBeforeBreaking: serviceConfig.CircuitBreakerThreshold,
            durationOfBreak: serviceConfig.CircuitBreakerDuration,
            onBreak: (result, duration) =>
            {
                Log.Warning("Circuit breaker opened for {Service} for {Duration}ms", serviceName, duration.TotalMilliseconds);
            },
            onReset: () =>
            {
                Log.Information("Circuit breaker reset for {Service}", serviceName);
            });
}

// Get external service options for configuring HTTP clients
var externalServiceOptions = builder.Configuration.GetSection(ExternalServiceOptions.SectionName).Get<ExternalServiceOptions>() ?? new ExternalServiceOptions();

// HTTP Clients for External Services with resilience patterns
builder.Services.AddHttpClient<ISupplierServiceClient, SupplierServiceClient>((serviceProvider, client) =>
{
    var serviceConfig = externalServiceOptions.SupplierService;
    if (string.IsNullOrEmpty(serviceConfig.BaseUrl))
        throw new InvalidOperationException("ExternalServices:SupplierService:BaseUrl configuration is required");

    client.BaseAddress = new Uri($"{serviceConfig.BaseUrl.TrimEnd('/')}/v1");
    client.DefaultRequestHeaders.Add("User-Agent", "PurchaseOrderService/1.0");
    client.Timeout = serviceConfig.Timeout;
})
.AddHttpMessageHandler<AuthenticatedHttpClientHandler>()
.AddPolicyHandler(request => GetRetryPolicy(externalServiceOptions.SupplierService))
.AddPolicyHandler(request => GetCircuitBreakerPolicy(externalServiceOptions.SupplierService, "SupplierService"));

builder.Services.AddHttpClient<IOrderServiceClient, OrderServiceClient>((serviceProvider, client) =>
{
    var serviceConfig = externalServiceOptions.OrderService;
    if (string.IsNullOrEmpty(serviceConfig.BaseUrl))
        throw new InvalidOperationException("ExternalServices:OrderService:BaseUrl configuration is required");

    client.BaseAddress = new Uri($"{serviceConfig.BaseUrl.TrimEnd('/')}/v1");
    client.DefaultRequestHeaders.Add("User-Agent", "PurchaseOrderService/1.0");
    client.Timeout = serviceConfig.Timeout;
})
.AddHttpMessageHandler<AuthenticatedHttpClientHandler>()
.AddPolicyHandler(request => GetRetryPolicy(externalServiceOptions.OrderService))
.AddPolicyHandler(request => GetCircuitBreakerPolicy(externalServiceOptions.OrderService, "OrderService"));

builder.Services.AddHttpClient<ICurrencyServiceClient, CurrencyServiceClient>((serviceProvider, client) =>
{
    var serviceConfig = externalServiceOptions.CurrencyService;
    if (string.IsNullOrEmpty(serviceConfig.BaseUrl))
        throw new InvalidOperationException("ExternalServices:CurrencyService:BaseUrl configuration is required");

    client.BaseAddress = new Uri($"{serviceConfig.BaseUrl.TrimEnd('/')}/v1");
    client.DefaultRequestHeaders.Add("User-Agent", "PurchaseOrderService/1.0");
    client.Timeout = serviceConfig.Timeout;
})
.AddPolicyHandler(request => GetRetryPolicy(externalServiceOptions.CurrencyService))
.AddPolicyHandler(request => GetCircuitBreakerPolicy(externalServiceOptions.CurrencyService, "CurrencyService"));

builder.Services.AddHttpClient<IUploadServiceClient, UploadServiceClient>((serviceProvider, client) =>
{
    var serviceConfig = externalServiceOptions.UploadService;
    if (string.IsNullOrEmpty(serviceConfig.BaseUrl))
        throw new InvalidOperationException("ExternalServices:UploadService:BaseUrl configuration is required");

    client.BaseAddress = new Uri($"{serviceConfig.BaseUrl.TrimEnd('/')}/v1");
    client.DefaultRequestHeaders.Add("User-Agent", "PurchaseOrderService/1.0");
    client.Timeout = serviceConfig.Timeout;
})
.AddPolicyHandler(request => GetRetryPolicy(externalServiceOptions.UploadService))
.AddPolicyHandler(request => GetCircuitBreakerPolicy(externalServiceOptions.UploadService, "UploadService"));

builder.Services.AddHttpClient<IPdfServiceClient, PdfServiceClient>((serviceProvider, client) =>
{
    var serviceConfig = externalServiceOptions.PdfService;
    if (string.IsNullOrEmpty(serviceConfig.BaseUrl))
        throw new InvalidOperationException("ExternalServices:PdfService:BaseUrl configuration is required");

    client.BaseAddress = new Uri($"{serviceConfig.BaseUrl.TrimEnd('/')}/v1");
    client.DefaultRequestHeaders.Add("User-Agent", "PurchaseOrderService/1.0");
    client.Timeout = serviceConfig.Timeout;
})
.AddPolicyHandler(request => GetRetryPolicy(externalServiceOptions.PdfService))
.AddPolicyHandler(request => GetCircuitBreakerPolicy(externalServiceOptions.PdfService, "PdfService"));

// Business Services
builder.Services.AddScoped<IWHTCalculationService, WHTCalculationService>();
builder.Services.AddScoped<IDocumentManagementService, DocumentManagementService>();
builder.Services.AddScoped<IPdfGenerationService, PdfGenerationService>();
builder.Services.AddScoped<IPurchaseOrderService, PurchaseOrderService>();
builder.Services.AddScoped<IDomainEventService, DomainEventService>();

// Controllers
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.SuppressMapClientErrors = true;
    });

// API Versioning
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ApiVersionReader = ApiVersionReader.Combine(
        new UrlSegmentApiVersionReader(),
        new HeaderApiVersionReader("X-Api-Version"),
        new QueryStringApiVersionReader("version")
    );
});

// MediatR - Comment out since package not referenced
// builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

// Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Purchase Order Service API",
        Version = "v1",
        Description = "Comprehensive microservice for managing purchase orders with external service integration, WHT calculations, and document management",
        Contact = new OpenApiContact
        {
            Name = "Maliev Development Team",
            Email = "dev@maliev.com"
        }
    });

    // Include XML documentation
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }

    // JWT Authentication in Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme (Example: 'Bearer 12345abcdef')",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Health Checks
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy())
    .AddDbContextCheck<PurchaseOrderContext>(tags: new[] { "readiness" });

// Authentication
if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            var jwtConfig = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();

            // Google Secret Manager and environment variables override appsettings
            var jwtKey = builder.Configuration["Jwt:SecurityKey"] ?? jwtConfig.SecurityKey;
            var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? jwtConfig.Issuer;
            var jwtAudience = builder.Configuration["Jwt:Audience"] ?? jwtConfig.Audience;

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = jwtConfig.ValidateIssuer,
                ValidateAudience = jwtConfig.ValidateAudience,
                ValidateLifetime = jwtConfig.ValidateLifetime,
                ValidateIssuerSigningKey = jwtConfig.ValidateIssuerSigningKey,
                ValidIssuer = jwtIssuer,
                ValidAudience = jwtAudience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                ClockSkew = jwtConfig.ClockSkewTimeSpan
            };

            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    Log.Warning("JWT authentication failed: {Error}", context.Exception.Message);
                    return Task.CompletedTask;
                },
                OnTokenValidated = context =>
                {
                    Log.Information("JWT token validated for user: {User}", context.Principal?.Identity?.Name);
                    return Task.CompletedTask;
                }
            };
        });
}

// Authorization
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Employee", policy => policy.RequireRole("Employee", "Manager", "Procurement", "Admin"));
    options.AddPolicy("Manager", policy => policy.RequireRole("Manager", "Procurement", "Admin"));
    options.AddPolicy("Procurement", policy => policy.RequireRole("Procurement", "Admin"));
    options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
});

// CORS Configuration
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var corsOriginsEnv = builder.Configuration["CORS_ALLOWED_ORIGINS"];
        var allowedOrigins = new List<string>();

        if (!string.IsNullOrEmpty(corsOriginsEnv))
        {
            allowedOrigins.AddRange(corsOriginsEnv.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(origin => origin.Trim()));
        }
        else
        {
            // Fallback for development and testing environments
            if (builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("Testing"))
            {
                allowedOrigins.AddRange(new[]
                {
                    "https://dev.intranet.maliev.com",
                    "https://dev.www.maliev.com",
                    "http://localhost:3000",
                    "http://localhost:4200"
                });
            }
            else
            {
                throw new InvalidOperationException("CORS_ALLOWED_ORIGINS environment variable is required");
            }
        }

        if (allowedOrigins.Count > 0)
        {
            policy.WithOrigins(allowedOrigins.ToArray())
                  .WithMethods("GET", "POST", "PUT", "DELETE", "PATCH", "OPTIONS")
                  .WithHeaders("Authorization", "Content-Type", "X-Api-Version")
                  .AllowCredentials();
        }
        else
        {
            policy.AllowAnyOrigin()
                  .WithMethods("GET", "POST", "PUT", "DELETE", "PATCH", "OPTIONS")
                  .WithHeaders("Authorization", "Content-Type", "X-Api-Version");
        }
    });
});

// Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.User?.Identity?.Name ?? httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1)
            }));

    options.AddPolicy("UploadPolicy", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.User?.Identity?.Name ?? httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1)
            }));

    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = 429;
        await context.HttpContext.Response.WriteAsync("Rate limit exceeded. Please try again later.", cancellationToken: token);
    };
});

var app = builder.Build();

// Middleware Pipeline (EXACT ORDER)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Purchase Order Service API v1");
        c.RoutePrefix = "purchaseorders/swagger";
    });
}

app.UseHttpsRedirection();
app.UseCors();

if (!app.Environment.IsEnvironment("Testing"))
{
    app.UseRateLimiter();
    app.UseAuthentication();
}

app.UseAuthorization();

// Health Checks
app.MapGet("/purchaseorders/liveness", () => "Healthy").AllowAnonymous();
app.MapHealthChecks("/purchaseorders/readiness", new HealthCheckOptions
{
    Predicate = healthCheck => healthCheck.Tags.Contains("readiness"),
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
}).AllowAnonymous();

// Apply rate limiting to upload endpoints
app.MapControllers().RequireRateLimiting("UploadPolicy").WithOpenApi();

// Database Migration (only for relational databases)
if (args.Contains("--migrate"))
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<PurchaseOrderContext>();

    // Only run migrations for relational databases (not InMemory)
    if (context.Database.ProviderName != "Microsoft.EntityFrameworkCore.InMemory")
    {
        await context.Database.MigrateAsync();
        Log.Information("Database migration completed");
    }
    else
    {
        Log.Information("Skipping migration for InMemory database");
    }
    return;
}

// Automatic Database Migration on Startup (Production)
if (!app.Environment.IsDevelopment() && !app.Environment.IsEnvironment("Testing"))
{
    try
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PurchaseOrderContext>();

        // Only run migrations for relational databases (not InMemory)
        if (context.Database.ProviderName != "Microsoft.EntityFrameworkCore.InMemory")
        {
            await context.Database.MigrateAsync();
            Log.Information("Automatic database migration completed on startup");
        }
        else
        {
            Log.Information("Skipping migration for InMemory database");
        }
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Database migration failed on startup");
        throw;
    }
}

// Background Services - Disabled in Testing environment
if (!app.Environment.IsEnvironment("Testing"))
{
    _ = Task.Run(async () =>
    {
        while (!app.Lifetime.ApplicationStopping.IsCancellationRequested)
        {
            try
            {
                using var scope = app.Services.CreateScope();
                var domainEventService = scope.ServiceProvider.GetRequiredService<IDomainEventService>();
                var pdfService = scope.ServiceProvider.GetRequiredService<IPdfGenerationService>();

                // Process domain events every 30 seconds
                await domainEventService.ProcessUnprocessedEventsAsync(50, app.Lifetime.ApplicationStopping);

                // Process pending PDF generation every 60 seconds
                await pdfService.ProcessPendingPdfGenerationAsync(10, app.Lifetime.ApplicationStopping);

                await Task.Delay(TimeSpan.FromSeconds(30), app.Lifetime.ApplicationStopping);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in background processing");
                await Task.Delay(TimeSpan.FromMinutes(1), app.Lifetime.ApplicationStopping);
            }
        }
    });
}

// Cleanup background service - Disabled in Testing environment
if (!app.Environment.IsEnvironment("Testing"))
{
    _ = Task.Run(async () =>
    {
        while (!app.Lifetime.ApplicationStopping.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromHours(6), app.Lifetime.ApplicationStopping); // Run every 6 hours

                using var scope = app.Services.CreateScope();
                var domainEventService = scope.ServiceProvider.GetRequiredService<IDomainEventService>();
                var documentService = scope.ServiceProvider.GetRequiredService<IDocumentManagementService>();

                // Cleanup old domain events (90 days)
                await domainEventService.CleanupOldEventsAsync(90, app.Lifetime.ApplicationStopping);

                // Archive old documents (7 years)
                await documentService.ArchiveOldDocumentsAsync(2555, app.Lifetime.ApplicationStopping);

                Log.Information("Cleanup tasks completed");
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in cleanup processing");
            }
        }
    });
}

Log.Information("Purchase Order Service starting up");
app.Run();

// HTTP Client Handler for JWT Authentication
public class AuthenticatedHttpClientHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuthenticatedHttpClientHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Get the Authorization token from the current HTTP context
        var authHeader = _httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();

        if (!string.IsNullOrEmpty(authHeader))
        {
            request.Headers.Add("Authorization", authHeader);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}

// Make Program class accessible for testing
public partial class Program { }
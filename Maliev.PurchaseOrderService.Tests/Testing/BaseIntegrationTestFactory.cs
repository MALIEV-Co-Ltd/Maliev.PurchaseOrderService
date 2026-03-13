using Maliev.PurchaseOrderService.Domain.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Diagnostics.CodeAnalysis;
using MassTransit;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Testcontainers.Redis;
using Xunit;

// Disable parallel execution to prevent race conditions on the shared singleton database
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Maliev.PurchaseOrderService.Tests.Testing;

/// <summary>
/// Base integration test factory for PurchaseOrderService.
/// Provides PostgreSQL, Redis, and RabbitMQ containers with parallel startup.
/// </summary>
/// <typeparam name="TProgram">The Program class of the service being tested</typeparam>
/// <typeparam name="TDbContext">The DbContext type for the service</typeparam>
public class BaseIntegrationTestFactory<TProgram, TDbContext> : WebApplicationFactory<TProgram>, IAsyncLifetime
    where TProgram : class
    where TDbContext : DbContext
{
    private static PostgreSqlContainer? _postgresContainer;
    private static RedisContainer? _redisContainer;
    private static RabbitMqContainer? _rabbitmqContainer;
    private static bool _containersStarted;
    private static readonly SemaphoreSlim _initLock = new(1, 1);

    private readonly RSA _testRsa;

    /// <summary>
    /// Override this property if your DbContext connection string has a different name.
    /// Defaults to the DbContext class name.
    /// </summary>
    protected virtual string DbConnectionStringName => "PurchaseOrderDbContext";

    public BaseIntegrationTestFactory()
    {
        _testRsa = RSA.Create(2048);

        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
    }

    public async Task InitializeAsync()
    {
        await _initLock.WaitAsync();
        try
        {
            if (!_containersStarted)
            {
                _postgresContainer = 
#pragma warning disable CS0618
        new PostgreSqlBuilder().WithImage("postgres:18-alpine")
                    .Build();

                _redisContainer = new RedisBuilder().WithImage("redis:8.4-alpine")
                    .Build();

                _rabbitmqContainer = new RabbitMqBuilder().WithImage("rabbitmq:4.2-alpine")
                    .Build();
#pragma warning restore CS0618

                await Task.WhenAll(
                    _postgresContainer.StartAsync(),
                    _redisContainer.StartAsync(),
                    _rabbitmqContainer.StartAsync()
                );

                // Ensure PostgreSQL is fully ready and accepting connections
                var postgresReady = false;
                var retryCount = 0;
                const int maxRetries = 60;
                while (!postgresReady && retryCount < maxRetries)
                {
                    try
                    {
                        await using var conn = new Npgsql.NpgsqlConnection(_postgresContainer.GetConnectionString());
                        await conn.OpenAsync();
                        await using var cmd = conn.CreateCommand();
                        cmd.CommandText = "SELECT 1";
                        await cmd.ExecuteScalarAsync();
                        postgresReady = true;
                    }
                    catch
                    {
                        retryCount++;
                        await Task.Delay(1000);
                    }
                }

                if (!postgresReady)
                {
                    throw new InvalidOperationException("PostgreSQL Testcontainer failed to become ready (Ping failed) after 60 seconds.");
                }

                using (var connection = await StackExchange.Redis.ConnectionMultiplexer.ConnectAsync(_redisContainer.GetConnectionString()))
                {
                    await connection.GetDatabase().PingAsync();
                }

                await ApplyMigrationsAsync();

                _containersStarted = true;
            }
        }
        finally
        {
            _initLock.Release();
        }

        // Explicitly set environment variables so they are available globally
        Environment.SetEnvironmentVariable($"ConnectionStrings__{DbConnectionStringName}", _postgresContainer!.GetConnectionString());
        Environment.SetEnvironmentVariable("ConnectionStrings__redis", _redisContainer!.GetConnectionString());
        Environment.SetEnvironmentVariable("ConnectionStrings__rabbitmq", _rabbitmqContainer!.GetConnectionString());

        // Set service URLs as environment variables to ensure they are picked up
        var additionalSettings = GetAdditionalConfiguration();
        if (additionalSettings != null)
        {
            foreach (var setting in additionalSettings)
            {
                var envKey = setting.Key.Replace(":", "__");
                Environment.SetEnvironmentVariable(envKey, setting.Value);
            }
        }
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync(); // Stop the Host
        // Static containers are NOT disposed here to allow reuse across tests
        _testRsa.Dispose();
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", null);
    }


    protected override IHost CreateHost(IHostBuilder builder)
    {
        if (!_containersStarted)
        {
            InitializeAsync().GetAwaiter().GetResult();
        }

        var rsaParams = _testRsa.ExportParameters(false);
        Environment.SetEnvironmentVariable("JWT_PUBLIC_KEY_MODULUS", Convert.ToBase64String(rsaParams.Modulus!));
        Environment.SetEnvironmentVariable("JWT_PUBLIC_KEY_EXPONENT", Convert.ToBase64String(rsaParams.Exponent!));

        ConfigureEnvironmentVariables();

        return base.CreateHost(builder);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("CORS:AllowedOrigins:0", "http://localhost:3000");
        builder.UseSetting("Features:FailOpenOnIAMError", "true");

        builder.ConfigureAppConfiguration((context, config) =>
        {
            if (!_containersStarted)
            {
                InitializeAsync().GetAwaiter().GetResult();
            }

            var settings = new Dictionary<string, string?>
            {
                [$"ConnectionStrings:{DbConnectionStringName}"] = _postgresContainer!.GetConnectionString(),
                [$"ConnectionStrings__{DbConnectionStringName}"] = _postgresContainer!.GetConnectionString(),
                ["ConnectionStrings:redis"] = _redisContainer!.GetConnectionString(),
                ["ConnectionStrings:rabbitmq"] = _rabbitmqContainer!.GetConnectionString(),
                ["Jwt:SecurityKey"] = "test-secret-key-at-least-32-characters-long"
            };

            var additionalSettings = GetAdditionalConfiguration();
            if (additionalSettings != null)
            {
                foreach (var setting in additionalSettings)
                {
                    settings[setting.Key] = setting.Value;
                }
            }

            config.AddInMemoryCollection(settings);
        });

        builder.ConfigureTestServices(services =>
        {
            // Override external service clients to point to WireMock
            var settings = GetAdditionalConfiguration();
            if (settings != null)
            {
                services.AddHttpClient("SupplierService", client =>
                    client.BaseAddress = new Uri(settings["Services:SupplierService:BaseUrl"] ?? "http://localhost"));
                services.AddHttpClient("OrderService", client =>
                    client.BaseAddress = new Uri(settings["Services:OrderService:BaseUrl"] ?? "http://localhost"));
                services.AddHttpClient("CurrencyService", client =>
                    client.BaseAddress = new Uri(settings["Services:CurrencyService:BaseUrl"] ?? "http://localhost"));
                services.AddHttpClient("UploadService", client =>
                    client.BaseAddress = new Uri(settings["Services:UploadService:BaseUrl"] ?? "http://localhost"));
                services.AddHttpClient("PdfService", client =>
                    client.BaseAddress = new Uri(settings["Services:PdfService:BaseUrl"] ?? "http://localhost"));
                services.AddHttpClient("IAMService", client =>
                    client.BaseAddress = new Uri(settings["Services:IAMService:BaseUrl"] ?? "http://localhost"));
            }

            services.PostConfigureAll<Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerOptions>(options =>
            {
                options.MapInboundClaims = false;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = "test-issuer",
                    ValidAudience = "test-audience",
                    IssuerSigningKey = new RsaSecurityKey(_testRsa),
                    ClockSkew = TimeSpan.Zero,
                    NameClaimType = JwtRegisteredClaimNames.Sub,
                    RoleClaimType = "role"
                };

                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = context =>
                    {
                        if (context.Principal?.Identity is ClaimsIdentity identity)
                        {
                            var subClaim = identity.FindFirst(JwtRegisteredClaimNames.Sub);
                            if (subClaim != null && !identity.HasClaim(c => c.Type == ClaimTypes.NameIdentifier))
                            {
                                identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, subClaim.Value));
                            }

                            var roleClaims = identity.FindAll("role").ToList();
                            foreach (var roleClaim in roleClaims)
                            {
                                if (!identity.HasClaim(c => c.Type == ClaimTypes.Role && c.Value == roleClaim.Value))
                                {
                                    identity.AddClaim(new Claim(ClaimTypes.Role, roleClaim.Value));
                                }
                            }
                        }
                        return Task.CompletedTask;
                    }
                };

                options.TokenValidationParameters.SignatureValidator = null;
            });

            // Add MassTransit test harness for testing message publishing/consuming
            services.AddMassTransitTestHarness();

            // Allow derived classes to add additional test services
            ConfigureAdditionalServices(services);
        });
    }

    protected virtual Dictionary<string, string?> GetAdditionalConfiguration()
    {
        return new Dictionary<string, string?>();
    }

    protected virtual void ConfigureEnvironmentVariables()
    {
    }

    protected virtual void ConfigureAdditionalServices(IServiceCollection services)
    {
    }

    public TDbContext GetDbContext()
    {
        var scope = Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<TDbContext>();
    }

    public TDbContext CreateDbContext()
    {
        var connectionString = _postgresContainer!.GetConnectionString();
        var optionsBuilder = new DbContextOptionsBuilder<TDbContext>();
        optionsBuilder.UseNpgsql(connectionString);
        optionsBuilder.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        return (TDbContext)Activator.CreateInstance(typeof(TDbContext), optionsBuilder.Options)!;
    }

    private async Task ApplyMigrationsAsync()
    {
        await using var context = CreateDbContext();
        try
        {
            await context.Database.MigrateAsync();
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("PendingModelChangesWarning"))
        {
            // Ignore ghost pending changes warning in tests
        }
    }

    [SuppressMessage("Security", "EF1002:Gaps in SQL queries", Justification = "Table names are retrieved from information_schema and are safe.")]
    public async Task CleanDatabaseAsync()
    {
        // Clear all pools to ensure no active connections interfere with TRUNCATE
        Npgsql.NpgsqlConnection.ClearAllPools();

        await using var context = CreateDbContext();

        // Get all table names from ALL schemas except system ones
        var tableNames = await context.Database
            .SqlQueryRaw<string>(
                @"SELECT '""' || table_schema || '"".""' || table_name || '""'
                  FROM information_schema.tables
                  WHERE table_schema NOT IN ('information_schema', 'pg_catalog')
                  AND table_type = 'BASE TABLE'
                  AND table_name != '__EFMigrationsHistory'
                  ORDER BY table_name")
            .ToListAsync();

        foreach (var tableNameWithSchema in tableNames)
        {
            try
            {
                await context.Database.ExecuteSqlRawAsync($"TRUNCATE TABLE {tableNameWithSchema} RESTART IDENTITY CASCADE");
            }
            catch (Npgsql.PostgresException ex) when (ex.SqlState == "42P01")
            {
            }
        }
    }

    public Task ResetDatabaseAsync() => CleanDatabaseAsync();

    public Task ClearDatabaseAsync() => CleanDatabaseAsync();

    public void ClearCache()
    {
        var memoryCache = Services.GetService<Microsoft.Extensions.Caching.Memory.IMemoryCache>();
        if (memoryCache is Microsoft.Extensions.Caching.Memory.MemoryCache cache)
        {
            cache.Compact(1.0);
        }
    }

    public SigningCredentials SigningCredentials => new SigningCredentials(new RsaSecurityKey(_testRsa), SecurityAlgorithms.RsaSha256);

    public string CreateTestJwtToken(
        string userId = "test-user",
        string[]? roles = null,
        string[]? permissions = null,
        Dictionary<string, string>? additionalClaims = null)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        if (roles != null)
        {
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }
        }

        if (permissions != null)
        {
            foreach (var permission in permissions)
            {
                claims.Add(new Claim("permissions", permission));
            }
        }

        if (additionalClaims != null)
        {
            foreach (var (key, value) in additionalClaims)
            {
                claims.Add(new Claim(key, value));
            }
        }

        var rsaSecurityKey = new RsaSecurityKey(_testRsa);
        var signingCredentials = new SigningCredentials(rsaSecurityKey, SecurityAlgorithms.RsaSha256);

        var token = new JwtSecurityToken(
            issuer: "test-issuer",
            audience: "test-audience",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: signingCredentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateTestToken(string userId = "test-user", string role = "admin")
    {
        return CreateTestJwtToken(userId, new[] { role });
    }

    public HttpClient CreateAuthenticatedClient(string userId = "test-user", string[]? roles = null, string[]? permissions = null)
    {
        var token = CreateTestJwtToken(userId, roles, permissions);
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
        return client;
    }

    public HttpClient CreatePermissionAuthenticatedClient(string userId = "test-user", string[]? permissions = null)
    {
        var token = CreateTestJwtToken(userId, null, permissions);
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
        return client;
    }
}





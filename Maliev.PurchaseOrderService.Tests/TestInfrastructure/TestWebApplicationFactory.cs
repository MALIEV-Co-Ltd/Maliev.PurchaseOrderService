using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Maliev.PurchaseOrderService.Data;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using WireMock.Server;
using Testcontainers.PostgreSql;
using Microsoft.EntityFrameworkCore;

namespace Maliev.PurchaseOrderService.Tests.TestInfrastructure;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;
    private readonly RSA _testRsa;
    private const string TestIssuer = "test-issuer";
    private const string TestAudience = "test-audience";
    
    // WireMock servers are passed in to configure URLs
    private readonly string _supplierServiceUrl;
    private readonly string _orderServiceUrl;
    private readonly string _currencyServiceUrl;
    private readonly string _uploadServiceUrl;
    private readonly string _pdfServiceUrl;

    public TestWebApplicationFactory(
        string connectionString, 
        RSA testRsa,
        string supplierServiceUrl,
        string orderServiceUrl,
        string currencyServiceUrl,
        string uploadServiceUrl,
        string pdfServiceUrl)
    {
        _connectionString = connectionString;
        _testRsa = testRsa;
        _supplierServiceUrl = supplierServiceUrl;
        _orderServiceUrl = orderServiceUrl;
        _currencyServiceUrl = currencyServiceUrl;
        _uploadServiceUrl = uploadServiceUrl;
        _pdfServiceUrl = pdfServiceUrl;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:PurchaseOrderDbContext"] = _connectionString,
                ["Jwt:Issuer"] = TestIssuer,
                ["Jwt:Audience"] = TestAudience,
                // Provide valid dummy key to bypass AddJwtAuthentication startup checks
                // The actual key used for validation is overridden by PostConfigureAll below
                ["Jwt:PublicKey"] = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(RSA.Create().ExportSubjectPublicKeyInfoPem())),
                ["ExternalServices:SupplierService:BaseUrl"] = _supplierServiceUrl,
                ["ExternalServices:OrderService:BaseUrl"] = _orderServiceUrl,
                ["ExternalServices:CurrencyService:BaseUrl"] = _currencyServiceUrl,
                ["ExternalServices:UploadService:BaseUrl"] = _uploadServiceUrl,
                ["ExternalServices:PdfService:BaseUrl"] = _pdfServiceUrl,
                ["CORS:AllowedOrigins:0"] = "http://localhost:3000",
                ["Redis:Enabled"] = "false"
            });
        });

        builder.ConfigureTestServices(services =>
        {
            // Remove existing DbContext registrations
            services.RemoveAll(typeof(DbContextOptions<PurchaseOrderContext>));
            services.RemoveAll(typeof(PurchaseOrderContext));

            // Add test database context
            services.AddDbContext<PurchaseOrderContext>(options =>
            {
                options.UseNpgsql(_connectionString);
            });

            // PostConfigure JWT Bearer options to use our test RSA key
            services.PostConfigureAll<JwtBearerOptions>(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = TestIssuer,
                    ValidAudience = TestAudience,
                    IssuerSigningKey = new RsaSecurityKey(_testRsa),
                    ClockSkew = TimeSpan.Zero // No clock skew for tests
                };
            });

            // Configure HttpClients to use WireMock URLs with /v1/ suffix to match test expectations
            services.AddHttpClient("SupplierService", client => client.BaseAddress = new Uri(_supplierServiceUrl + "/v1/"));
            services.AddHttpClient("OrderService", client => client.BaseAddress = new Uri(_orderServiceUrl + "/v1/"));
            services.AddHttpClient("CurrencyService", client => client.BaseAddress = new Uri(_currencyServiceUrl + "/v1/"));
            services.AddHttpClient("UploadService", client => client.BaseAddress = new Uri(_uploadServiceUrl + "/v1/"));
            services.AddHttpClient("PdfService", client => client.BaseAddress = new Uri(_pdfServiceUrl + "/v1/"));
        });
    }
}

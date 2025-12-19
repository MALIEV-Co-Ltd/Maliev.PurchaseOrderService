using Maliev.PurchaseOrderService.Data;
using Maliev.PurchaseOrderService.Tests.Testing;
using WireMock.Server;

namespace Maliev.PurchaseOrderService.Tests.TestInfrastructure;

public class TestWebApplicationFactory : BaseIntegrationTestFactory<Program, PurchaseOrderContext>
{
    // WireMock servers for external service dependencies
    public WireMockServer SupplierServiceMock { get; private set; } = null!;
    public WireMockServer OrderServiceMock { get; private set; } = null!;
    public WireMockServer CurrencyServiceMock { get; private set; } = null!;
    public WireMockServer UploadServiceMock { get; private set; } = null!;
    public WireMockServer PdfServiceMock { get; private set; } = null!;

    public TestWebApplicationFactory()
    {
        // Start WireMock servers for external services during construction
        SupplierServiceMock = WireMockServer.Start();
        OrderServiceMock = WireMockServer.Start();
        CurrencyServiceMock = WireMockServer.Start();
        UploadServiceMock = WireMockServer.Start();
        PdfServiceMock = WireMockServer.Start();
    }

    protected override string DbConnectionStringName => "PurchaseOrderDbContext";

    protected override void ConfigureEnvironmentVariables()
    {
        base.ConfigureEnvironmentVariables();

        // Configure WireMock URLs as environment variables for external services
        // Include /v1/ path prefix with trailing slash to match test mock configurations
        // The trailing slash is critical - without it, HttpClient treats "1" as replacing "/v1" instead of appending
        Environment.SetEnvironmentVariable("ExternalServices:SupplierService:BaseUrl", $"{SupplierServiceMock.Urls[0]}/v1/");
        Environment.SetEnvironmentVariable("ExternalServices:OrderService:BaseUrl", $"{OrderServiceMock.Urls[0]}/v1/");
        Environment.SetEnvironmentVariable("ExternalServices:CurrencyService:BaseUrl", $"{CurrencyServiceMock.Urls[0]}/v1/");
        Environment.SetEnvironmentVariable("ExternalServices:UploadService:BaseUrl", $"{UploadServiceMock.Urls[0]}/v1/");
        Environment.SetEnvironmentVariable("ExternalServices:PdfService:BaseUrl", $"{PdfServiceMock.Urls[0]}/v1/");
    }
}

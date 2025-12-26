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
    public WireMockServer IAMServiceMock { get; private set; } = null!;

    public TestWebApplicationFactory()
    {
        // Start WireMock servers for external services during construction
        SupplierServiceMock = WireMockServer.Start();
        OrderServiceMock = WireMockServer.Start();
        CurrencyServiceMock = WireMockServer.Start();
        UploadServiceMock = WireMockServer.Start();
        PdfServiceMock = WireMockServer.Start();
        IAMServiceMock = WireMockServer.Start();
    }

    protected override string DbConnectionStringName => "PurchaseOrderDbContext";

    protected override Dictionary<string, string?> GetAdditionalConfiguration()
    {
        var config = base.GetAdditionalConfiguration();

        // Configure WireMock URLs for external services
        // The trailing slash is critical for HttpClient base address
        config["SupplierService:BaseUrl"] = $"{SupplierServiceMock.Urls[0]}/v1/suppliers/";
        config["OrderService:BaseUrl"] = $"{OrderServiceMock.Urls[0]}/v1/orders/";
        config["CurrencyService:BaseUrl"] = $"{CurrencyServiceMock.Urls[0]}/v1/currencies/";
        config["UploadService:BaseUrl"] = $"{UploadServiceMock.Urls[0]}/v1/uploads/";
        config["PdfService:BaseUrl"] = $"{PdfServiceMock.Urls[0]}/v1/pdfs/"; config["IAMService:BaseUrl"] = $"{IAMServiceMock.Urls[0]}/iam/v1/";

        return config;
    }
}
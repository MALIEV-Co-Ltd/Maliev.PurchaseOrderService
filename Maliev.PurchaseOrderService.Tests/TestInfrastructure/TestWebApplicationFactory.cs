using Maliev.PurchaseOrderService.Infrastructure.Persistence;
using Maliev.PurchaseOrderService.Domain.Entities;
using Maliev.PurchaseOrderService.Infrastructure;
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
        // The trailing slash is critical for HttpClient base address when using relative paths
        var supplierUrl = $"{SupplierServiceMock.Urls[0]}/v1/suppliers/";
        var orderUrl = $"{OrderServiceMock.Urls[0]}/v1/orders/";
        var currencyUrl = $"{CurrencyServiceMock.Urls[0]}/v1/currencies/";
        var uploadUrl = $"{UploadServiceMock.Urls[0]}/v1/uploads/";
        var pdfUrl = $"{PdfServiceMock.Urls[0]}/v1/pdfs/";
        var iamUrl = $"{IAMServiceMock.Urls[0]}/";

        config["Services:SupplierService:BaseUrl"] = supplierUrl;
        config["Services__SupplierService__BaseUrl"] = supplierUrl;
        config["Services:Supplier:BaseUrl"] = supplierUrl;
        config["Services__Supplier__BaseUrl"] = supplierUrl;

        config["Services:OrderService:BaseUrl"] = orderUrl;
        config["Services__OrderService__BaseUrl"] = orderUrl;
        config["Services:Order:BaseUrl"] = orderUrl;
        config["Services__Order__BaseUrl"] = orderUrl;

        config["Services:CurrencyService:BaseUrl"] = currencyUrl;
        config["Services__CurrencyService__BaseUrl"] = currencyUrl;
        config["Services:Currency:BaseUrl"] = currencyUrl;
        config["Services__Currency__BaseUrl"] = currencyUrl;

        config["Services:UploadService:BaseUrl"] = uploadUrl;
        config["Services__UploadService__BaseUrl"] = uploadUrl;

        config["Services:PdfService:BaseUrl"] = pdfUrl;
        config["Services__PdfService__BaseUrl"] = pdfUrl;

        config["Services:IAMService:BaseUrl"] = iamUrl;
        config["Services__IAMService__BaseUrl"] = iamUrl;

        // Reduce IAM registration delay for tests
        config["IAM:RegistrationDelaySeconds"] = "0";

        return config;
    }
}

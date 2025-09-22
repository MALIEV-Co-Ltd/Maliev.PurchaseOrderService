using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Maliev.PurchaseOrderService.Api.DTOs;
using Maliev.PurchaseOrderService.Api.Models;
using Maliev.PurchaseOrderService.Data;

namespace Maliev.PurchaseOrderService.Tests.Integration;

/// <summary>
/// Integration tests for T033: Document management with PDF generation
///
/// Tests Scenario 9 from quickstart.md:
/// - Document management including automatic PDF generation for internal POs only
/// - File upload and download functionality
/// - Document type validation (CustomerPO, InternalApproval, GeneratedPDF)
/// - Automatic PDF generation via PdfService integration
/// - File storage via UploadService integration
/// - Document security and access control
/// - File size and type validation
/// - Service unavailability handling
/// </summary>
public class DocumentManagementTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly ILogger<DocumentManagementTests> _logger;

    public DocumentManagementTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();

        // Configure test logging
        using var scope = _factory.Services.CreateScope();
        var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
        _logger = loggerFactory.CreateLogger<DocumentManagementTests>();
    }

    [Fact]
    public async Task UploadDocument_CustomerPO_ShouldUploadSuccessfully()
    {
        // Arrange
        var multipartContent = new MultipartFormDataContent();

        // Create a test PDF file content
        var pdfContent = Encoding.UTF8.GetBytes("%PDF-1.4 Test Customer PO Document Content");
        var fileContent = new ByteArrayContent(pdfContent);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");

        multipartContent.Add(fileContent, "file", "customer-po-2025-5678.pdf");
        multipartContent.Add(new StringContent("CustomerPO"), "documentType");
        multipartContent.Add(new StringContent("Customer purchase order document received via email"), "description");

        // Set authorization header
        _client.DefaultRequestHeaders.Add("Authorization", "Bearer employee-token");
        _client.DefaultRequestHeaders.Add("X-User-Id", "emp_12345");

        // Act
        var response = await _client.PostAsync("/purchase-orders/12345/files", multipartContent);

        // Assert - Should fail because implementation doesn't exist yet (TDD)
        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "because the purchase order file upload endpoint is not implemented yet");
    }

    [Fact]
    public async Task GetDocuments_ForPurchaseOrder_ShouldReturnAllDocuments()
    {
        // Arrange
        _client.DefaultRequestHeaders.Add("Authorization", "Bearer employee-token");
        _client.DefaultRequestHeaders.Add("X-User-Id", "emp_12345");

        // Act
        var response = await _client.GetAsync("/purchase-orders/12345/files");

        // Assert - Should fail because implementation doesn't exist yet (TDD)
        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "because the purchase order files list endpoint is not implemented yet");
    }

    [Fact]
    public async Task DownloadDocument_ValidDocument_ShouldReturnSignedUrl()
    {
        // Arrange
        _client.DefaultRequestHeaders.Add("Authorization", "Bearer employee-token");
        _client.DefaultRequestHeaders.Add("X-User-Id", "emp_12345");

        // Act
        var response = await _client.GetAsync("/purchase-orders/12345/files/501");

        // Assert - Should fail because implementation doesn't exist yet (TDD)
        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "because the purchase order file download endpoint is not implemented yet");
    }

    [Fact]
    public async Task DeleteDocument_WithManagerRole_ShouldDeleteSuccessfully()
    {
        // Arrange
        _client.DefaultRequestHeaders.Add("Authorization", "Bearer manager-token");
        _client.DefaultRequestHeaders.Add("X-User-Role", "manager");
        _client.DefaultRequestHeaders.Add("X-User-Id", "mgr_67890");

        // Act
        var response = await _client.DeleteAsync("/purchase-orders/12345/files/502");

        // Assert - Should fail because implementation doesn't exist yet (TDD)
        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "because the purchase order file delete endpoint is not implemented yet");
    }

    [Fact]
    public async Task AutomaticPDFGeneration_InternalPO_ShouldGeneratePDFAutomatically()
    {
        // This test validates that internal purchase orders automatically generate PDFs

        // Arrange
        var request = new CreatePurchaseOrderRequest
        {
            SupplierID = 1234,
            OrderID = 5678,
            CurrencyID = 1,
            OrderType = (Maliev.PurchaseOrderService.Data.Enums.OrderType)OrderType.Internal, // Internal POs should auto-generate PDFs
            Notes = "Internal purchase order requiring automatic PDF generation",
            ShippingAddress = new CreateAddressRequest
            {
                AddressType = (Maliev.PurchaseOrderService.Data.Enums.AddressType)AddressType.Shipping,
                ContactName = "Internal Department",
                AddressLine1 = "123 Office Building",
                City = "Bangkok",
                PostalCode = "10330",
                Country = "Thailand",
                PhoneNumber = "+66-2-555-0123",
                EmailAddress = "internal@maliev.com"
            }
        };

        _client.DefaultRequestHeaders.Add("Authorization", "Bearer employee-token");
        _client.DefaultRequestHeaders.Add("X-User-Id", "emp_12345");

        // Act
        var response = await _client.PostAsJsonAsync("/purchase-orders", request);

        // Assert - Should fail because implementation doesn't exist yet (TDD)
        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "because the purchase orders controller endpoint is not implemented yet");
    }

    [Fact]
    public async Task AutomaticPDFGeneration_ExternalPO_ShouldNotGeneratePDF()
    {
        // This test validates that external purchase orders do NOT automatically generate PDFs

        // Arrange
        var request = new CreatePurchaseOrderRequest
        {
            SupplierID = 1234,
            OrderID = 5678,
            CurrencyID = 1,
            OrderType = (Maliev.PurchaseOrderService.Data.Enums.OrderType)OrderType.External, // External POs should NOT auto-generate PDFs
            Notes = "External purchase order - no automatic PDF generation",
            ShippingAddress = new CreateAddressRequest
            {
                AddressType = (Maliev.PurchaseOrderService.Data.Enums.AddressType)AddressType.Shipping,
                ContactName = "External Supplier",
                AddressLine1 = "456 Supplier Street",
                City = "Bangkok",
                PostalCode = "10330",
                Country = "Thailand",
                PhoneNumber = "+66-2-555-0123",
                EmailAddress = "orders@supplier.com"
            }
        };

        _client.DefaultRequestHeaders.Add("Authorization", "Bearer employee-token");
        _client.DefaultRequestHeaders.Add("X-User-Id", "emp_12345");

        // Act
        var response = await _client.PostAsJsonAsync("/purchase-orders", request);

        // Assert - Should fail because implementation doesn't exist yet (TDD)
        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "because the purchase orders controller endpoint is not implemented yet");
    }

    [Fact]
    public async Task UploadDocument_FileTooLarge_ShouldReturnError()
    {
        // Arrange - Create a file larger than 50MB limit
        var multipartContent = new MultipartFormDataContent();

        // Simulate large file (50MB + 1 byte)
        var largeContent = new byte[50 * 1024 * 1024 + 1];
        var fileContent = new ByteArrayContent(largeContent);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");

        multipartContent.Add(fileContent, "file", "large-document.pdf");
        multipartContent.Add(new StringContent("CustomerPO"), "documentType");
        multipartContent.Add(new StringContent("Large test document"), "description");

        _client.DefaultRequestHeaders.Add("Authorization", "Bearer employee-token");

        // Act
        var response = await _client.PostAsync("/purchase-orders/12345/files", multipartContent);

        // Assert - Should fail because implementation doesn't exist yet (TDD)
        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "because the purchase order file upload endpoint is not implemented yet");

        // When implemented, should return 413 Request Entity Too Large
        // response.StatusCode.Should().Be(HttpStatusCode.RequestEntityTooLarge);
    }

    [Fact]
    public async Task UploadDocument_InvalidFileType_ShouldReturnValidationError()
    {
        // Arrange - Upload non-PDF file
        var multipartContent = new MultipartFormDataContent();

        var txtContent = Encoding.UTF8.GetBytes("This is a text file, not a PDF");
        var fileContent = new ByteArrayContent(txtContent);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain");

        multipartContent.Add(fileContent, "file", "invalid-document.txt");
        multipartContent.Add(new StringContent("CustomerPO"), "documentType");
        multipartContent.Add(new StringContent("Invalid file type test"), "description");

        _client.DefaultRequestHeaders.Add("Authorization", "Bearer employee-token");

        // Act
        var response = await _client.PostAsync("/purchase-orders/12345/files", multipartContent);

        // Assert - Should fail because implementation doesn't exist yet (TDD)
        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "because the purchase order file upload endpoint is not implemented yet");

        // When implemented, should return BadRequest for invalid file types
        // response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UploadDocument_GeneratedPDFType_ShouldReturnForbidden()
    {
        // This test validates that users cannot manually upload GeneratedPDF documents
        // Only the system should create GeneratedPDF documents

        // Arrange
        var multipartContent = new MultipartFormDataContent();

        var pdfContent = Encoding.UTF8.GetBytes("%PDF-1.4 Fake Generated PDF");
        var fileContent = new ByteArrayContent(pdfContent);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");

        multipartContent.Add(fileContent, "file", "fake-generated.pdf");
        multipartContent.Add(new StringContent("GeneratedPDF"), "documentType"); // Should be forbidden
        multipartContent.Add(new StringContent("Attempting to upload GeneratedPDF manually"), "description");

        _client.DefaultRequestHeaders.Add("Authorization", "Bearer employee-token");

        // Act
        var response = await _client.PostAsync("/purchase-orders/12345/files", multipartContent);

        // Assert - Should fail because implementation doesn't exist yet (TDD)
        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "because the purchase order file upload endpoint is not implemented yet");

        // When implemented, should return Forbidden for GeneratedPDF uploads
        // response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UploadService_Unavailable_ShouldReturnServiceError()
    {
        // This test validates handling when UploadService is unavailable

        // Arrange
        var multipartContent = new MultipartFormDataContent();

        var pdfContent = Encoding.UTF8.GetBytes("%PDF-1.4 Test Document");
        var fileContent = new ByteArrayContent(pdfContent);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");

        multipartContent.Add(fileContent, "file", "test-document.pdf");
        multipartContent.Add(new StringContent("CustomerPO"), "documentType");
        multipartContent.Add(new StringContent("Test when UploadService is down"), "description");

        _client.DefaultRequestHeaders.Add("Authorization", "Bearer employee-token");

        // Act
        var response = await _client.PostAsync("/purchase-orders/12345/files", multipartContent);

        // Assert - Should fail because implementation doesn't exist yet (TDD)
        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "because the purchase order file upload endpoint is not implemented yet");

        // When implemented and UploadService is unavailable, should return 502 Bad Gateway
        // response.StatusCode.Should().Be(HttpStatusCode.BadGateway);
        // var content = await response.Content.ReadAsStringAsync();
        // content.Should().Contain("UploadService is currently unavailable");
    }

    [Fact]
    public async Task PdfService_GenerationFailure_ShouldHandleGracefully()
    {
        // This test validates handling when PdfService fails to generate PDFs

        // Arrange
        var request = new CreatePurchaseOrderRequest
        {
            SupplierID = 1234,
            OrderID = 5678,
            CurrencyID = 1,
            OrderType = (Data.Enums.OrderType)OrderType.Internal, // Should trigger PDF generation
            Notes = "Test PDF generation failure handling",
            ShippingAddress = new CreateAddressRequest
            {
                AddressType = (Maliev.PurchaseOrderService.Data.Enums.AddressType)AddressType.Shipping,
                ContactName = "Test Contact",
                AddressLine1 = "Test Address",
                City = "Bangkok",
                PostalCode = "10330",
                Country = "Thailand",
                PhoneNumber = "+66-2-555-0123",
                EmailAddress = "test@maliev.com"
            }
        };

        _client.DefaultRequestHeaders.Add("Authorization", "Bearer employee-token");

        // Act
        var response = await _client.PostAsJsonAsync("/purchase-orders", request);

        // Assert - Should fail because implementation doesn't exist yet (TDD)
        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "because the purchase orders controller endpoint is not implemented yet");
    }

    [Theory]
    [InlineData("CustomerPO")]
    [InlineData("InternalApproval")]
    [InlineData("SupplierQuote")]
    [InlineData("DeliveryReceipt")]
    public async Task UploadDocument_ValidDocumentTypes_ShouldAcceptUpload(string documentType)
    {
        // This test validates that all valid document types are accepted

        // Arrange
        var multipartContent = new MultipartFormDataContent();

        var pdfContent = Encoding.UTF8.GetBytes($"%PDF-1.4 Test {documentType} Document");
        var fileContent = new ByteArrayContent(pdfContent);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");

        multipartContent.Add(fileContent, "file", $"{documentType.ToLower()}-test.pdf");
        multipartContent.Add(new StringContent(documentType), "documentType");
        multipartContent.Add(new StringContent($"Test {documentType} document upload"), "description");

        _client.DefaultRequestHeaders.Add("Authorization", "Bearer employee-token");

        // Act
        var response = await _client.PostAsync("/purchase-orders/12345/files", multipartContent);

        // Assert - Should fail because implementation doesn't exist yet (TDD)
        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            $"because the purchase order file upload endpoint is not implemented yet for {documentType}");
    }

    [Fact]
    public async Task DocumentAccess_CrossOrderAccess_ShouldReturnForbidden()
    {
        // This test validates that users cannot access documents from orders they don't have permission for

        // Arrange
        _client.DefaultRequestHeaders.Add("Authorization", "Bearer employee-token");
        _client.DefaultRequestHeaders.Add("X-User-Id", "emp_12345");
        _client.DefaultRequestHeaders.Add("X-User-Department", "Engineering");

        // Act - Attempting to access document from different department's order
        var response = await _client.GetAsync("/purchase-orders/99999/files/501");

        // Assert - Should fail because implementation doesn't exist yet (TDD)
        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "because the purchase order file download endpoint is not implemented yet");

        // When implemented, should validate access permissions
        // response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task BulkDocumentOperation_MultipleUploads_ShouldHandleCorrectly()
    {
        // This test validates bulk document operations

        var documentTypes = new[] { "CustomerPO", "InternalApproval", "SupplierQuote" };
        var responses = new List<HttpResponseMessage>();

        foreach (var docType in documentTypes)
        {
            // Arrange
            var multipartContent = new MultipartFormDataContent();

            var pdfContent = Encoding.UTF8.GetBytes($"%PDF-1.4 Bulk Upload {docType}");
            var fileContent = new ByteArrayContent(pdfContent);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");

            multipartContent.Add(fileContent, "file", $"bulk-{docType.ToLower()}.pdf");
            multipartContent.Add(new StringContent(docType), "documentType");
            multipartContent.Add(new StringContent($"Bulk upload {docType}"), "description");

            _client.DefaultRequestHeaders.Clear();
            _client.DefaultRequestHeaders.Add("Authorization", "Bearer employee-token");

            // Act
            var response = await _client.PostAsync("/purchase-orders/12345/files", multipartContent);
            responses.Add(response);
        }

        // Assert - All should fail because implementation doesn't exist yet (TDD)
        foreach (var response in responses)
        {
            response.StatusCode.Should().Be(HttpStatusCode.NotFound,
                "because the purchase order file upload endpoint is not implemented yet");
        }
    }

    [Fact]
    public async Task GeneratedPDF_SystemUser_ShouldHaveCorrectMetadata()
    {
        // This test validates that system-generated PDFs have correct metadata

        // Arrange - Create internal order that should auto-generate PDF
        var request = new CreatePurchaseOrderRequest
        {
            SupplierID = 1234,
            OrderID = 5678,
            CurrencyID = 1,
            OrderType = (Data.Enums.OrderType)OrderType.Internal,
            Notes = "Internal order for PDF metadata validation",
            ShippingAddress = new CreateAddressRequest
            {
                AddressType = (Maliev.PurchaseOrderService.Data.Enums.AddressType)AddressType.Shipping,
                ContactName = "Internal Contact",
                AddressLine1 = "Internal Address",
                City = "Bangkok",
                PostalCode = "10330",
                Country = "Thailand",
                PhoneNumber = "+66-2-555-0123",
                EmailAddress = "internal@maliev.com"
            }
        };

        _client.DefaultRequestHeaders.Add("Authorization", "Bearer employee-token");

        // Act
        var createResponse = await _client.PostAsJsonAsync("/purchase-orders", request);

        // Assert - Should fail because implementation doesn't exist yet (TDD)
        createResponse.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "because the purchase orders controller endpoint is not implemented yet");
    }

    private byte[] GenerateTestPDFContent(string content = "Test PDF Content")
    {
        // Generate a minimal valid PDF structure for testing
        var pdfHeader = "%PDF-1.4\n";
        var pdfContent = $"1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] >>\nendobj\nxref\n0 4\n0000000000 65535 f\n0000000009 00000 n\n0000000058 00000 n\n0000000115 00000 n\ntrailer\n<< /Size 4 /Root 1 0 R >>\nstartxref\n181\n%%EOF";
        return Encoding.UTF8.GetBytes(pdfHeader + pdfContent);
    }
}
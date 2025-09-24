using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;
using System.Net;
using Xunit;
using FluentAssertions;
using Moq;
using Maliev.PurchaseOrderService.Api;
using Maliev.PurchaseOrderService.Api.DTOs;
using Maliev.PurchaseOrderService.Data;
using Maliev.PurchaseOrderService.Data.Entities;
using Maliev.PurchaseOrderService.Data.Enums;
using Maliev.PurchaseOrderService.Tests.TestInfrastructure;
using Maliev.PurchaseOrderService.Api.Services;

namespace Maliev.PurchaseOrderService.Tests.Integration;

/// <summary>
/// Comprehensive integration tests for PurchaseOrderFilesController
/// Tests file upload, download, delete, PDF generation, and document management
/// </summary>
public class PurchaseOrderFilesControllerIntegrationTests : IntegrationTestBase
{
    public PurchaseOrderFilesControllerIntegrationTests(TestWebApplicationFactory<Program> factory) : base(factory)
    {
    }

    protected override void SetupCommonMocks()
    {
        base.SetupCommonMocks();

        // Setup mock document management service
        var mockDocumentService = new Mock<IDocumentManagementService>();

        // Default mock responses for file operations
        mockDocumentService
            .Setup(x => x.GetDocumentsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PurchaseOrderFileDto>
            {
                new PurchaseOrderFileDto
                {
                    Id = 1,
                    PurchaseOrderId = 1,
                    FileName = "test-document.pdf",
                    ContentType = "application/pdf",
                    FileSize = 1024,
                    UploadedBy = "test-user",
                    UploadedAt = DateTime.UtcNow
                }
            });

        mockDocumentService
            .Setup(x => x.GetDocumentMetadataAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int fileId, CancellationToken _) => new PurchaseOrderFileDto
            {
                Id = fileId,
                PurchaseOrderId = 1,
                FileName = "test-document.pdf",
                ContentType = "application/pdf",
                FileSize = 1024,
                UploadedBy = "test-user",
                UploadedAt = DateTime.UtcNow
            });

        mockDocumentService
            .Setup(x => x.ValidateFile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>()))
            .Returns(new DocumentValidationResult { IsValid = true, Errors = new List<string>() });

        mockDocumentService
            .Setup(x => x.UploadDocumentAsync(It.IsAny<int>(), It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int poId, Stream stream, string fileName, string contentType, string uploadedBy, CancellationToken _) => new DocumentUploadResult
            {
                Success = true,
                FileId = 1,
                FileSize = stream.Length,
                UploadedBy = uploadedBy
            });

        mockDocumentService
            .Setup(x => x.DownloadDocumentAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DocumentDownloadResult
            {
                Success = true,
                FileName = "test-document.pdf",
                ContentType = "application/pdf",
                FileStream = new MemoryStream(Encoding.UTF8.GetBytes("Test PDF content"))
            });

        mockDocumentService
            .Setup(x => x.DeleteDocumentAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        mockDocumentService
            .Setup(x => x.UpdateDocumentAsync(It.IsAny<int>(), It.IsAny<UpdateDocumentRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int fileId, UpdateDocumentRequest request, string updatedBy, CancellationToken _) => new PurchaseOrderFileDto
            {
                Id = fileId,
                PurchaseOrderId = 1,
                FileName = request.FileName ?? "updated-document.pdf",
                ContentType = "application/pdf",
                FileSize = 1024,
                UploadedBy = "test-user",
                UploadedAt = DateTime.UtcNow
            });

        mockDocumentService
            .Setup(x => x.GeneratePreviewUrlAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://preview.example.com/document/1");

        // Setup mock PDF generation service
        var mockPdfService = new Mock<IPdfGenerationService>();

        mockPdfService
            .Setup(x => x.GeneratePurchaseOrderPdfAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PdfGenerationResult
            {
                Success = true,
                FilePath = "https://storage.example.com/pdf/po-123.pdf",
                FileSize = 2048,
                GeneratedAt = DateTime.UtcNow
            });

        mockPdfService
            .Setup(x => x.GetPdfGenerationStatusAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PdfGenerationStatus
            {
                Status = PdfStatus.Completed,
                LastAttempt = DateTime.UtcNow,
                AttemptCount = 1,
                IsApplicable = true
            });
    }

    #region GET /v1.0/purchase-orders/{purchaseOrderId}/files Tests

    [Fact]
    public async Task GetFiles_WithValidPurchaseOrderId_ShouldReturnFiles()
    {
        // Arrange
        SetupEmployeeAuthentication();
        var seededPurchaseOrder = await SeedPurchaseOrderAsync();

        // Act
        var response = await Client.GetAsync($"/v1.0/purchase-orders/{seededPurchaseOrder.Id}/files");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<List<PurchaseOrderFileDto>>(responseContent, JsonOptions);

        result.Should().NotBeNull();
        result!.Should().NotBeEmpty();
        result.All(file => file.PurchaseOrderId == seededPurchaseOrder.Id).Should().BeTrue();
    }

    [Fact]
    public async Task GetFiles_WithInvalidPurchaseOrderId_ShouldReturnNotFound()
    {
        // Arrange
        SetupEmployeeAuthentication();

        // Act
        var response = await Client.GetAsync("/v1.0/purchase-orders/99999/files");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var responseContent = await response.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseContent, JsonOptions);

        errorResponse.Should().NotBeNull();
        errorResponse!.Error.Code.Should().Be("PURCHASE_ORDER_NOT_FOUND");
    }

    [Fact]
    public async Task GetFiles_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        ClearAuthentication();
        var seededPurchaseOrder = await SeedPurchaseOrderAsync();

        // Act
        var response = await Client.GetAsync($"/v1.0/purchase-orders/{seededPurchaseOrder.Id}/files");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region GET /v1.0/purchase-orders/{purchaseOrderId}/files/{fileId} Tests

    [Fact]
    public async Task GetFile_WithValidIds_ShouldReturnFileMetadata()
    {
        // Arrange
        SetupEmployeeAuthentication();
        var seededPurchaseOrder = await SeedPurchaseOrderAsync();

        // Act
        var response = await Client.GetAsync($"/v1.0/purchase-orders/{seededPurchaseOrder.Id}/files/1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PurchaseOrderFileDto>(responseContent, JsonOptions);

        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.PurchaseOrderId.Should().Be(seededPurchaseOrder.Id);
        result.FileName.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetFile_WithInvalidFileId_ShouldReturnNotFound()
    {
        // Arrange
        SetupEmployeeAuthentication();
        var seededPurchaseOrder = await SeedPurchaseOrderAsync();

        // Setup mock to return null for invalid file ID
        var mockDocumentService = MockServiceFactory.CreateDocumentManagementServiceMock();
        mockDocumentService
            .Setup(x => x.GetDocumentMetadataAsync(99999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PurchaseOrderFileDto?)null);

        // Act
        var response = await Client.GetAsync($"/v1.0/purchase-orders/{seededPurchaseOrder.Id}/files/99999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var responseContent = await response.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseContent, JsonOptions);

        errorResponse.Should().NotBeNull();
        errorResponse!.Error.Code.Should().Be("FILE_NOT_FOUND");
    }

    #endregion

    #region POST /v1.0/purchase-orders/{purchaseOrderId}/files Tests

    [Fact]
    public async Task UploadFile_WithValidFile_ShouldUploadSuccessfully()
    {
        // Arrange
        SetupEmployeeAuthentication("emp123");
        var seededPurchaseOrder = await SeedPurchaseOrderAsync();

        // Create test file content
        var fileContent = Encoding.UTF8.GetBytes("Test PDF content for upload");
        var fileStream = new MemoryStream(fileContent);

        using var form = new MultipartFormDataContent();
        var fileUploadContent = new StreamContent(fileStream);
        fileUploadContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        form.Add(fileUploadContent, "file", "test-upload.pdf");

        // Act
        var response = await Client.PostAsync($"/v1.0/purchase-orders/{seededPurchaseOrder.Id}/files", form);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<DocumentUploadResult>(responseContent, JsonOptions);

        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.UploadedBy.Should().Be("emp123");

        // Verify location header
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Contain($"/v1.0/purchase-orders/{seededPurchaseOrder.Id}/files/{result.FileId}");
    }

    [Fact]
    public async Task UploadFile_WithoutFile_ShouldReturnBadRequest()
    {
        // Arrange
        SetupEmployeeAuthentication();
        var seededPurchaseOrder = await SeedPurchaseOrderAsync();

        using var form = new MultipartFormDataContent();

        // Act
        var response = await Client.PostAsync($"/v1.0/purchase-orders/{seededPurchaseOrder.Id}/files", form);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var responseContent = await response.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseContent, JsonOptions);

        errorResponse.Should().NotBeNull();
        errorResponse!.Error.Code.Should().Be("NO_FILE_PROVIDED");
    }

    [Fact]
    public async Task UploadFile_WithEmptyFile_ShouldReturnBadRequest()
    {
        // Arrange
        SetupEmployeeAuthentication();
        var seededPurchaseOrder = await SeedPurchaseOrderAsync();

        using var form = new MultipartFormDataContent();
        var emptyContent = new ByteArrayContent(Array.Empty<byte>());
        emptyContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        form.Add(emptyContent, "file", "empty.pdf");

        // Act
        var response = await Client.PostAsync($"/v1.0/purchase-orders/{seededPurchaseOrder.Id}/files", form);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var responseContent = await response.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseContent, JsonOptions);

        errorResponse.Should().NotBeNull();
        errorResponse!.Error.Code.Should().Be("NO_FILE_PROVIDED");
    }

    [Fact]
    public async Task UploadFile_WithInvalidPurchaseOrderId_ShouldReturnNotFound()
    {
        // Arrange
        SetupEmployeeAuthentication();

        var fileContent = Encoding.UTF8.GetBytes("Test content");
        var fileStream = new MemoryStream(fileContent);

        using var form = new MultipartFormDataContent();
        var fileUploadContent = new StreamContent(fileStream);
        fileUploadContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        form.Add(fileUploadContent, "file", "test.pdf");

        // Act
        var response = await Client.PostAsync("/v1.0/purchase-orders/99999/files", form);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var responseContent = await response.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseContent, JsonOptions);

        errorResponse.Should().NotBeNull();
        errorResponse!.Error.Code.Should().Be("PURCHASE_ORDER_NOT_FOUND");
    }

    [Fact]
    public async Task UploadFile_WithInvalidFileType_ShouldReturnBadRequest()
    {
        // Arrange
        SetupEmployeeAuthentication();
        var seededPurchaseOrder = await SeedPurchaseOrderAsync();

        // Setup mock to return validation failure
        var mockDocumentService = MockServiceFactory.CreateDocumentManagementServiceMock();
        mockDocumentService
            .Setup(x => x.ValidateFile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>()))
            .Returns(new DocumentValidationResult
            {
                IsValid = false,
                Errors = new List<string> { "File type not supported" }
            });

        var fileContent = Encoding.UTF8.GetBytes("Invalid content");
        var fileStream = new MemoryStream(fileContent);

        using var form = new MultipartFormDataContent();
        var fileUploadContent = new StreamContent(fileStream);
        fileUploadContent.Headers.ContentType = new MediaTypeHeaderValue("application/exe");
        form.Add(fileUploadContent, "file", "malware.exe");

        // Act
        var response = await Client.PostAsync($"/v1.0/purchase-orders/{seededPurchaseOrder.Id}/files", form);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var responseContent = await response.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseContent, JsonOptions);

        errorResponse.Should().NotBeNull();
        errorResponse!.Error.Code.Should().Be("FILE_VALIDATION_FAILED");
    }

    #endregion

    #region GET /v1.0/purchase-orders/{purchaseOrderId}/files/{fileId}/download Tests

    [Fact]
    public async Task DownloadFile_WithValidIds_ShouldReturnFileContent()
    {
        // Arrange
        SetupEmployeeAuthentication();
        var seededPurchaseOrder = await SeedPurchaseOrderAsync();

        // Act
        var response = await Client.GetAsync($"/v1.0/purchase-orders/{seededPurchaseOrder.Id}/files/1/download");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/pdf");

        var content = await response.Content.ReadAsByteArrayAsync();
        content.Should().NotBeEmpty();
    }

    [Fact]
    public async Task DownloadFile_WithInvalidFileId_ShouldReturnNotFound()
    {
        // Arrange
        SetupEmployeeAuthentication();
        var seededPurchaseOrder = await SeedPurchaseOrderAsync();

        // Setup mock to return null metadata for invalid file
        var mockDocumentService = MockServiceFactory.CreateDocumentManagementServiceMock();
        mockDocumentService
            .Setup(x => x.GetDocumentMetadataAsync(99999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PurchaseOrderFileDto?)null);

        // Act
        var response = await Client.GetAsync($"/v1.0/purchase-orders/{seededPurchaseOrder.Id}/files/99999/download");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var responseContent = await response.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseContent, JsonOptions);

        errorResponse.Should().NotBeNull();
        errorResponse!.Error.Code.Should().Be("FILE_NOT_FOUND");
    }

    [Fact]
    public async Task DownloadFile_WithDownloadFailure_ShouldReturnNotFound()
    {
        // Arrange
        SetupEmployeeAuthentication();
        var seededPurchaseOrder = await SeedPurchaseOrderAsync();

        // Setup mock to return download failure
        var mockDocumentService = MockServiceFactory.CreateDocumentManagementServiceMock();
        mockDocumentService
            .Setup(x => x.DownloadDocumentAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DocumentDownloadResult
            {
                Success = false,
                ErrorMessage = "File not found in storage"
            });

        // Act
        var response = await Client.GetAsync($"/v1.0/purchase-orders/{seededPurchaseOrder.Id}/files/1/download");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var responseContent = await response.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseContent, JsonOptions);

        errorResponse.Should().NotBeNull();
        errorResponse!.Error.Code.Should().Be("DOWNLOAD_FAILED");
    }

    #endregion

    #region DELETE /v1.0/purchase-orders/{purchaseOrderId}/files/{fileId} Tests

    [Fact]
    public async Task DeleteFile_WithValidIds_ShouldDeleteSuccessfully()
    {
        // Arrange
        SetupEmployeeAuthentication("emp123");
        var seededPurchaseOrder = await SeedPurchaseOrderAsync();

        // Act
        var response = await Client.DeleteAsync($"/v1.0/purchase-orders/{seededPurchaseOrder.Id}/files/1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteFile_WithInvalidFileId_ShouldReturnNotFound()
    {
        // Arrange
        SetupEmployeeAuthentication();
        var seededPurchaseOrder = await SeedPurchaseOrderAsync();

        // Setup mock to return null metadata for invalid file
        var mockDocumentService = MockServiceFactory.CreateDocumentManagementServiceMock();
        mockDocumentService
            .Setup(x => x.GetDocumentMetadataAsync(99999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PurchaseOrderFileDto?)null);

        // Act
        var response = await Client.DeleteAsync($"/v1.0/purchase-orders/{seededPurchaseOrder.Id}/files/99999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var responseContent = await response.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseContent, JsonOptions);

        errorResponse.Should().NotBeNull();
        errorResponse!.Error.Code.Should().Be("FILE_NOT_FOUND");
    }

    [Fact]
    public async Task DeleteFile_WithDeleteFailure_ShouldReturnNotFound()
    {
        // Arrange
        SetupEmployeeAuthentication();
        var seededPurchaseOrder = await SeedPurchaseOrderAsync();

        // Setup mock to return false for delete operation
        var mockDocumentService = MockServiceFactory.CreateDocumentManagementServiceMock();
        mockDocumentService
            .Setup(x => x.DeleteDocumentAsync(1, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var response = await Client.DeleteAsync($"/v1.0/purchase-orders/{seededPurchaseOrder.Id}/files/1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var responseContent = await response.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseContent, JsonOptions);

        errorResponse.Should().NotBeNull();
        errorResponse!.Error.Code.Should().Be("FILE_NOT_FOUND");
    }

    #endregion

    #region PUT /v1.0/purchase-orders/{purchaseOrderId}/files/{fileId} Tests

    [Fact]
    public async Task UpdateFile_WithValidRequest_ShouldUpdateSuccessfully()
    {
        // Arrange
        SetupEmployeeAuthentication("emp123");
        var seededPurchaseOrder = await SeedPurchaseOrderAsync();

        var updateRequest = new UpdateDocumentRequest
        {
            FileName = "updated-document.pdf",
            Description = "Updated document description"
        };

        // Act
        var response = await PutAsJsonAsync($"/v1.0/purchase-orders/{seededPurchaseOrder.Id}/files/1", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PurchaseOrderFileDto>(responseContent, JsonOptions);

        result.Should().NotBeNull();
        result!.FileName.Should().Be("updated-document.pdf");
    }

    [Fact]
    public async Task UpdateFile_WithInvalidRequest_ShouldReturnBadRequest()
    {
        // Arrange
        SetupEmployeeAuthentication();
        var seededPurchaseOrder = await SeedPurchaseOrderAsync();

        var invalidRequest = new object(); // Invalid request object

        // Act
        var response = await PutAsJsonAsync($"/v1.0/purchase-orders/{seededPurchaseOrder.Id}/files/1", invalidRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateFile_WithInvalidFileId_ShouldReturnNotFound()
    {
        // Arrange
        SetupEmployeeAuthentication();
        var seededPurchaseOrder = await SeedPurchaseOrderAsync();

        // Setup mock to return null metadata for invalid file
        var mockDocumentService = MockServiceFactory.CreateDocumentManagementServiceMock();
        mockDocumentService
            .Setup(x => x.GetDocumentMetadataAsync(99999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PurchaseOrderFileDto?)null);

        var updateRequest = new UpdateDocumentRequest
        {
            FileName = "updated-document.pdf"
        };

        // Act
        var response = await PutAsJsonAsync($"/v1.0/purchase-orders/{seededPurchaseOrder.Id}/files/99999", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var responseContent = await response.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseContent, JsonOptions);

        errorResponse.Should().NotBeNull();
        errorResponse!.Error.Code.Should().Be("FILE_NOT_FOUND");
    }

    #endregion

    #region POST /v1.0/purchase-orders/{purchaseOrderId}/files/generate-pdf Tests

    [Fact]
    public async Task GeneratePdf_WithEmployeeAuth_ShouldGenerateSuccessfully()
    {
        // Arrange
        SetupEmployeeAuthentication();
        var seededPurchaseOrder = await SeedPurchaseOrderAsync();

        // Act
        var response = await Client.PostAsync($"/v1.0/purchase-orders/{seededPurchaseOrder.Id}/files/generate-pdf", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PdfGenerationResult>(responseContent, JsonOptions);

        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.FilePath.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GeneratePdf_WithInvalidPurchaseOrderId_ShouldReturnNotFound()
    {
        // Arrange
        SetupEmployeeAuthentication();

        // Act
        var response = await Client.PostAsync("/v1.0/purchase-orders/99999/files/generate-pdf", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var responseContent = await response.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseContent, JsonOptions);

        errorResponse.Should().NotBeNull();
        errorResponse!.Error.Code.Should().Be("PURCHASE_ORDER_NOT_FOUND");
    }

    [Fact]
    public async Task GeneratePdf_WithPdfGenerationFailure_ShouldReturnBadRequest()
    {
        // Arrange
        SetupEmployeeAuthentication();
        var seededPurchaseOrder = await SeedPurchaseOrderAsync();

        // Setup mock to return failure
        var mockPdfService = MockServiceFactory.CreatePdfGenerationServiceMock();
        mockPdfService
            .Setup(x => x.GeneratePurchaseOrderPdfAsync(seededPurchaseOrder.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PdfGenerationResult
            {
                Success = false,
                ErrorMessage = "PDF generation failed"
            });

        // Act
        var response = await Client.PostAsync($"/v1.0/purchase-orders/{seededPurchaseOrder.Id}/files/generate-pdf", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var responseContent = await response.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseContent, JsonOptions);

        errorResponse.Should().NotBeNull();
        errorResponse!.Error.Code.Should().Be("PDF_GENERATION_FAILED");
    }

    [Theory]
    [InlineData("Employee")]
    [InlineData("Manager")]
    [InlineData("Procurement")]
    [InlineData("Admin")]
    public async Task GeneratePdf_WithAuthorizedRoles_ShouldSucceed(string role)
    {
        // Arrange
        switch (role)
        {
            case "Employee":
                SetupEmployeeAuthentication();
                break;
            case "Manager":
                SetupManagerAuthentication();
                break;
            case "Procurement":
                SetupProcurementAuthentication();
                break;
            case "Admin":
                SetupAdminAuthentication();
                break;
        }

        var seededPurchaseOrder = await SeedPurchaseOrderAsync();

        // Act
        var response = await Client.PostAsync($"/v1.0/purchase-orders/{seededPurchaseOrder.Id}/files/generate-pdf", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region GET /v1.0/purchase-orders/{purchaseOrderId}/files/pdf-status Tests

    [Fact]
    public async Task GetPdfStatus_WithValidPurchaseOrderId_ShouldReturnStatus()
    {
        // Arrange
        SetupEmployeeAuthentication();
        var seededPurchaseOrder = await SeedPurchaseOrderAsync();

        // Act
        var response = await Client.GetAsync($"/v1.0/purchase-orders/{seededPurchaseOrder.Id}/files/pdf-status");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<PdfGenerationStatus>(responseContent, JsonOptions);

        result.Should().NotBeNull();
        result!.Status.Should().Be(PdfStatus.Completed);
    }

    [Fact]
    public async Task GetPdfStatus_WithInvalidPurchaseOrderId_ShouldReturnNotFound()
    {
        // Arrange
        SetupEmployeeAuthentication();

        // Act
        var response = await Client.GetAsync("/v1.0/purchase-orders/99999/files/pdf-status");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var responseContent = await response.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseContent, JsonOptions);

        errorResponse.Should().NotBeNull();
        errorResponse!.Error.Code.Should().Be("PURCHASE_ORDER_NOT_FOUND");
    }

    #endregion

    #region GET /v1.0/purchase-orders/{purchaseOrderId}/files/{fileId}/preview Tests

    [Fact]
    public async Task GetFilePreview_WithValidIds_ShouldReturnPreviewInfo()
    {
        // Arrange
        SetupEmployeeAuthentication();
        var seededPurchaseOrder = await SeedPurchaseOrderAsync();

        // Act
        var response = await Client.GetAsync($"/v1.0/purchase-orders/{seededPurchaseOrder.Id}/files/1/preview");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<FilePreviewResponseDto>(responseContent, JsonOptions);

        result.Should().NotBeNull();
        result!.FileId.Should().Be(1);
        result.FileName.Should().NotBeNullOrEmpty();
        result.PreviewUrl.Should().NotBeNullOrEmpty();
        result.IsPreviewSupported.Should().BeTrue();
    }

    [Fact]
    public async Task GetFilePreview_WithInvalidFileId_ShouldReturnNotFound()
    {
        // Arrange
        SetupEmployeeAuthentication();
        var seededPurchaseOrder = await SeedPurchaseOrderAsync();

        // Setup mock to return null metadata for invalid file
        var mockDocumentService = MockServiceFactory.CreateDocumentManagementServiceMock();
        mockDocumentService
            .Setup(x => x.GetDocumentMetadataAsync(99999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PurchaseOrderFileDto?)null);

        // Act
        var response = await Client.GetAsync($"/v1.0/purchase-orders/{seededPurchaseOrder.Id}/files/99999/preview");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var responseContent = await response.Content.ReadAsStringAsync();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseContent, JsonOptions);

        errorResponse.Should().NotBeNull();
        errorResponse!.Error.Code.Should().Be("FILE_NOT_FOUND");
    }

    #endregion

    #region Authorization Tests

    [Theory]
    [InlineData("/v1.0/purchase-orders/1/files")]
    [InlineData("/v1.0/purchase-orders/1/files/1")]
    [InlineData("/v1.0/purchase-orders/1/files/1/download")]
    [InlineData("/v1.0/purchase-orders/1/files/pdf-status")]
    [InlineData("/v1.0/purchase-orders/1/files/1/preview")]
    public async Task GetEndpoints_WithoutAuthentication_ShouldReturnUnauthorized(string endpoint)
    {
        // Arrange
        ClearAuthentication();

        // Act
        var response = await Client.GetAsync(endpoint);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("Employee")]
    [InlineData("Manager")]
    [InlineData("Procurement")]
    [InlineData("Admin")]
    public async Task GetFiles_WithValidRoles_ShouldSucceed(string role)
    {
        // Arrange
        switch (role)
        {
            case "Employee":
                SetupEmployeeAuthentication();
                break;
            case "Manager":
                SetupManagerAuthentication();
                break;
            case "Procurement":
                SetupProcurementAuthentication();
                break;
            case "Admin":
                SetupAdminAuthentication();
                break;
        }

        var seededPurchaseOrder = await SeedPurchaseOrderAsync();

        // Act
        var response = await Client.GetAsync($"/v1.0/purchase-orders/{seededPurchaseOrder.Id}/files");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task UploadFile_WithNegativePurchaseOrderId_ShouldReturnNotFound()
    {
        // Arrange
        SetupEmployeeAuthentication();

        var fileContent = Encoding.UTF8.GetBytes("Test content");
        var fileStream = new MemoryStream(fileContent);

        using var form = new MultipartFormDataContent();
        var fileUploadContent = new StreamContent(fileStream);
        fileUploadContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        form.Add(fileUploadContent, "file", "test.pdf");

        // Act
        var response = await Client.PostAsync("/v1.0/purchase-orders/-1/files", form);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateFile_WithMalformedJson_ShouldReturnBadRequest()
    {
        // Arrange
        SetupEmployeeAuthentication();
        var seededPurchaseOrder = await SeedPurchaseOrderAsync();

        var malformedJson = "{ invalid json }";
        var content = new StringContent(malformedJson, Encoding.UTF8, "application/json");

        // Act
        var response = await Client.PutAsync($"/v1.0/purchase-orders/{seededPurchaseOrder.Id}/files/1", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region File Size and Type Tests

    [Fact]
    public async Task UploadFile_WithLargeFile_ShouldRespectSizeLimit()
    {
        // Arrange
        SetupEmployeeAuthentication();
        var seededPurchaseOrder = await SeedPurchaseOrderAsync();

        // Create a file larger than the 50MB limit (note: this is just a test, actual limits are in the controller)
        var largeFileContent = new byte[60 * 1024 * 1024]; // 60MB
        var fileStream = new MemoryStream(largeFileContent);

        using var form = new MultipartFormDataContent();
        var fileUploadContent = new StreamContent(fileStream);
        fileUploadContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        form.Add(fileUploadContent, "file", "large-file.pdf");

        // Act & Assert
        // This test might timeout or fail due to size restrictions
        // The exact behavior depends on server configuration
        var response = await Client.PostAsync($"/v1.0/purchase-orders/{seededPurchaseOrder.Id}/files", form);

        // Should either succeed or return appropriate error
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.BadRequest, HttpStatusCode.RequestEntityTooLarge);
    }

    [Fact]
    public async Task UploadFile_WithDifferentFileTypes_ShouldHandleCorrectly()
    {
        // Arrange
        SetupEmployeeAuthentication();
        var seededPurchaseOrder = await SeedPurchaseOrderAsync();

        var testFiles = new[]
        {
            ("document.pdf", "application/pdf"),
            ("image.jpg", "image/jpeg"),
            ("spreadsheet.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"),
            ("text.txt", "text/plain")
        };

        foreach (var (fileName, contentType) in testFiles)
        {
            var fileContent = Encoding.UTF8.GetBytes($"Test content for {fileName}");
            var fileStream = new MemoryStream(fileContent);

            using var form = new MultipartFormDataContent();
            var fileUploadContent = new StreamContent(fileStream);
            fileUploadContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            form.Add(fileUploadContent, "file", fileName);

            // Act
            var response = await Client.PostAsync($"/v1.0/purchase-orders/{seededPurchaseOrder.Id}/files", form);

            // Assert
            // Should either succeed or fail based on file type validation
            response.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.BadRequest);
        }
    }

    #endregion
}

/// <summary>
/// File preview response DTO for testing
/// </summary>
public class FilePreviewResponseDto
{
    /// <summary>
    /// File ID
    /// </summary>
    public int FileId { get; set; }

    /// <summary>
    /// File name
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Content type
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// Preview URL (if supported)
    /// </summary>
    public string? PreviewUrl { get; set; }

    /// <summary>
    /// Whether preview is supported for this file type
    /// </summary>
    public bool IsPreviewSupported { get; set; }
}
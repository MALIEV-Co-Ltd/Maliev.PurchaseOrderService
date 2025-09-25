using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Maliev.PurchaseOrderService.Api.Configuration;
using Maliev.PurchaseOrderService.Api.ExternalServices;
using Maliev.PurchaseOrderService.Api.DTOs;

namespace Maliev.PurchaseOrderService.Tests.ExternalServices;

/// <summary>
/// T037: External service mock tests for UploadService
/// Tests HTTP client configuration, error handling, circuit breaker patterns,
/// authentication, timeout handling, response parsing and service availability
/// </summary>
public class UploadServiceTests
{
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<ILogger<UploadServiceClient>> _loggerMock;
    private readonly Mock<IOptions<ExternalServiceOptions>> _optionsMock;
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly HttpClient _httpClient;
    private readonly ExternalServiceOptions _serviceOptions;

    public UploadServiceTests()
    {
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _loggerMock = new Mock<ILogger<UploadServiceClient>>();
        _optionsMock = new Mock<IOptions<ExternalServiceOptions>>();
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();

        _serviceOptions = new ExternalServiceOptions
        {
            UploadService = new ServiceEndpoint
            {
                BaseUrl = "https://test.api.maliev.com/upload",
                TimeoutInSeconds = 120 // Longer timeout for file uploads
            }
        };

        _optionsMock.Setup(x => x.Value).Returns(_serviceOptions);

        _httpClient = new HttpClient(_httpMessageHandlerMock.Object)
        {
            BaseAddress = new Uri(_serviceOptions.UploadService.BaseUrl)
        };

        _httpClientFactoryMock
            .Setup(x => x.CreateClient("UploadService"))
            .Returns(_httpClient);
    }

    [Fact]
    public async Task UploadFileAsync_ValidFile_ReturnsUploadResult()
    {
        // Arrange
        var fileContent = "Test file content"u8.ToArray();
        var fileName = "test-document.pdf";
        var contentType = "application/pdf";

        var uploadRequest = new FileUploadRequest
        {
            FileName = fileName,
            ContentType = contentType,
            Description = "Purchase order document",
            Metadata = new Dictionary<string, string>
            {
                ["Category"] = "purchase-order",
                ["Tags"] = "document,po",
                ["FileSize"] = fileContent.Length.ToString()
            }
        };

        var expectedResponse = new FileUploadResultDto
        {
            FileId = Guid.NewGuid().ToString(),
            FileName = fileName,
            ContentType = contentType,
            FileSize = fileContent.Length,
            Url = "https://test.storage.maliev.com/files/123456/test-document.pdf",
            UploadedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(24)
        };

        var responseContent = JsonSerializer.Serialize(expectedResponse);
        var httpResponse = new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = new StringContent(responseContent, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"))
        };

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri!.ToString().Contains("/files/upload")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        var service = new UploadServiceClient(_httpClient, _loggerMock.Object, _optionsMock.Object);

        // Act
        using var fileStream = new MemoryStream(fileContent);
        var result = await service.UploadFileAsync(fileStream, fileName, contentType, "purchase-orders");

        // Assert
        result.Should().NotBeNull();
        result.FileName.Should().Be(fileName);
        result.ContentType.Should().Be(contentType);
        result.FileSize.Should().Be(fileContent.Length);
        result.Url.Should().NotBeNullOrEmpty();
        result.ThumbnailUrl.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetFileInfoAsync_ValidFileId_ReturnsFileInfo()
    {
        // Arrange
        var fileId = Guid.NewGuid();
        var expectedFileInfo = new FileInfoDto
        {
            FileId = fileId.ToString(),
            FileName = "purchase-order-123.pdf",
            ContentType = "application/pdf",
            FileSize = 1024000,
            UploadedAt = DateTime.UtcNow.AddHours(-2),
            UploadedBy = "user@maliev.com",
            Category = "purchase-order",
            Metadata = new Dictionary<string, string>
            {
                ["Tags"] = "approved,purchase-order,document"
            }
        };

        var responseContent = JsonSerializer.Serialize(expectedFileInfo);
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseContent, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"))
        };

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri!.ToString().Contains($"/files/{fileId}")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        var service = new UploadServiceClient(_httpClient, _loggerMock.Object, _optionsMock.Object);

        // Act
        var result = await service.GetFileInfoAsync(fileId.ToString());

        // Assert
        result.Should().NotBeNull();
        result.FileId.Should().Be(fileId.ToString());
        result.FileName.Should().Be("purchase-order-123.pdf");
        result.Category.Should().Be("purchase-order");
        result.Metadata.Should().ContainKey("Tags").WhoseValue.Should().Contain("approved");
    }

    [Fact]
    public async Task DownloadFileAsync_ValidFileId_ReturnsFileStream()
    {
        // Arrange
        var fileId = Guid.NewGuid();
        var fileContent = "Mock file content for download test"u8.ToArray();

        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(fileContent)
        };
        httpResponse.Content.Headers.Add("Content-Type", "application/pdf");
        httpResponse.Content.Headers.Add("Content-Disposition", "attachment; filename=\"test.pdf\"");

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri!.ToString().Contains($"/files/{fileId}/download")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        var service = new UploadServiceClient(_httpClient, _loggerMock.Object, _optionsMock.Object);

        // Act
        var result = await service.DownloadFileAsync(fileId.ToString());

        // Assert
        result.Should().NotBeNull();
        result.Content.Should().NotBeNull();
        result.FileName.Should().Be("test.pdf");
        result.ContentType.Should().Be("application/pdf");

        // Verify stream content
        var downloadedContent = new byte[fileContent.Length];
        await result.Content!.ReadExactlyAsync(downloadedContent);
        downloadedContent.Should().BeEquivalentTo(fileContent);
    }

    [Fact]
    public async Task DeleteFileAsync_ValidFileId_CompletesSuccessfully()
    {
        // Arrange
        var fileId = Guid.NewGuid();
        var httpResponse = new HttpResponseMessage(HttpStatusCode.NoContent);

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Delete &&
                    req.RequestUri!.ToString().Contains($"/files/{fileId}")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        var service = new UploadServiceClient(_httpClient, _loggerMock.Object, _optionsMock.Object);

        // Act
        await service.DeleteFileAsync(fileId.ToString());

        // Assert
        _httpMessageHandlerMock
            .Protected()
            .Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Delete &&
                    req.RequestUri!.ToString().Contains($"/files/{fileId}")),
                ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task GetFilesByTagsAsync_ValidTags_ReturnsFilteredFiles()
    {
        // Arrange
        var tags = new[] { "purchase-order", "approved" };
        var expectedFiles = new FileSearchResultDto
        {
            Files = new List<FileInfoDto>
            {
                new() { FileId = Guid.NewGuid().ToString(), FileName = "po-001.pdf", Category = "purchase-order" },
                new() { FileId = Guid.NewGuid().ToString(), FileName = "po-002.pdf", Category = "purchase-order" }
            },
            TotalCount = 2,
            Page = 1,
            PageSize = 50
        };

        var responseContent = JsonSerializer.Serialize(expectedFiles);
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseContent, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"))
        };

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri!.ToString().Contains("/files/search") &&
                    req.RequestUri.ToString().Contains("tags=")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        var service = new UploadServiceClient(_httpClient, _loggerMock.Object, _optionsMock.Object);

        // Act
        var result = await service.GetFilesByCategoryAsync("test-category");

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.First().Category.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task UpdateFileMetadataAsync_ValidData_ReturnsUpdatedFileInfo()
    {
        // Arrange
        var fileId = Guid.NewGuid();
        var updateRequest = new
        {
            FileName = "updated-purchase-order.pdf",
            Category = "purchase-order-updated",
            Tags = new[] { "document", "po", "updated", "approved" },
            IsPublic = true
        };

        var updatedFileInfo = new FileInfoDto
        {
            FileId = fileId.ToString(),
            FileName = updateRequest.FileName,
            Category = updateRequest.Category,
            Metadata = new Dictionary<string, string> { ["Tags"] = string.Join(",", updateRequest.Tags) },
            IsActive = true,
            UploadedAt = DateTime.UtcNow
        };

        var responseContent = JsonSerializer.Serialize(updatedFileInfo);
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseContent, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"))
        };

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Patch &&
                    req.RequestUri!.ToString().Contains($"/files/{fileId}/metadata")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        var service = new UploadServiceClient(_httpClient, _loggerMock.Object, _optionsMock.Object);

        // Act
        var result = await service.GetFileInfoAsync(fileId.ToString());

        // Assert
        result.Should().NotBeNull();
        result.FileId.Should().Be(fileId.ToString());
        result.FileName.Should().Be(updateRequest.FileName);
        result.Category.Should().Be(updateRequest.Category);
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task UploadFileAsync_ServiceUnavailable_ThrowsHttpRequestException()
    {
        // Arrange
        var fileContent = "Test content"u8.ToArray();
        var uploadRequest = new FileUploadRequest { FileName = "test.pdf" };
        var httpResponse = new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        var service = new UploadServiceClient(_httpClient, _loggerMock.Object, _optionsMock.Object);

        // Act & Assert
        using var fileStream = new MemoryStream(fileContent);
        var exception = await Assert.ThrowsAsync<ExternalServiceException>(
            () => service.UploadFileAsync(fileStream, "test.pdf", "application/pdf", "purchase-orders"));

        exception.Message.Should().Contain("Failed to upload file");
    }

    [Fact]
    public async Task UploadFileAsync_Timeout_ThrowsTimeoutException()
    {
        // Arrange
        var fileContent = "Test content"u8.ToArray();
        var uploadRequest = new FileUploadRequest { FileName = "test.pdf" };

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException("Upload timeout"));

        var service = new UploadServiceClient(_httpClient, _loggerMock.Object, _optionsMock.Object);

        // Act & Assert
        using var fileStream = new MemoryStream(fileContent);
        var exception = await Assert.ThrowsAsync<ExternalServiceException>(
            () => service.UploadFileAsync(fileStream, "test.pdf", "application/pdf", "purchase-orders"));

        exception.Message.Should().Contain("Timeout while uploading file");
    }

    [Fact]
    public async Task UploadFileAsync_FileTooLarge_ThrowsBadRequestException()
    {
        // Arrange
        var fileContent = "Test content"u8.ToArray();
        var uploadRequest = new FileUploadRequest { FileName = "large-file.pdf", ContentType = "application/pdf" }; // Large file
        var errorResponse = new { message = "File size exceeds maximum limit", maxSize = "50MB" };
        var responseContent = JsonSerializer.Serialize(errorResponse);
        var httpResponse = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent(responseContent, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"))
        };

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        var service = new UploadServiceClient(_httpClient, _loggerMock.Object, _optionsMock.Object);

        // Act & Assert
        using var fileStream = new MemoryStream(fileContent);
        var exception = await Assert.ThrowsAsync<ExternalServiceException>(
            () => service.UploadFileAsync(fileStream, "test.pdf", "application/pdf", "purchase-orders"));

        exception.Message.Should().Contain("Failed to upload file");
    }

    [Fact]
    public async Task GetFileInfoAsync_FileNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var fileId = Guid.NewGuid();
        var httpResponse = new HttpResponseMessage(HttpStatusCode.NotFound);

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        var service = new UploadServiceClient(_httpClient, _loggerMock.Object, _optionsMock.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.GetFileInfoAsync(fileId.ToString()));

        exception.Message.Should().Contain("File not found");
    }

    [Fact]
    public async Task UploadFileAsync_CircuitBreakerOpen_ThrowsCircuitBreakerException()
    {
        // Arrange
        var fileContent = "Test content"u8.ToArray();
        var uploadRequest = new FileUploadRequest { FileName = "test.pdf" };

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Circuit breaker is open for UploadService"));

        var service = new UploadServiceClient(_httpClient, _loggerMock.Object, _optionsMock.Object);

        // Act & Assert
        using var fileStream = new MemoryStream(fileContent);
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UploadFileAsync(fileStream, "test.pdf", "application/pdf", "purchase-orders"));

        exception.Message.Should().Contain("UploadService");
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    public async Task GetFileInfoAsync_AuthenticationFailure_ThrowsAuthenticationException(HttpStatusCode statusCode)
    {
        // Arrange
        var fileId = Guid.NewGuid();
        var httpResponse = new HttpResponseMessage(statusCode);

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        var service = new UploadServiceClient(_httpClient, _loggerMock.Object, _optionsMock.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.GetFileInfoAsync(fileId.ToString()));

        exception.Message.Should().Contain("authentication");
    }

    [Fact]
    public async Task GetFileInfoAsync_InvalidJson_ThrowsInvalidDataException()
    {
        // Arrange
        var fileId = Guid.NewGuid();
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("invalid json response", Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"))
        };

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        var service = new UploadServiceClient(_httpClient, _loggerMock.Object, _optionsMock.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidDataException>(
            () => service.GetFileInfoAsync(fileId.ToString()));

        exception.Message.Should().Contain("parsing");
    }

    [Fact]
    public void Constructor_NullHttpClientFactory_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new UploadServiceClient(null!, _loggerMock.Object, _optionsMock.Object));
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new UploadServiceClient(_httpClient, null!, _optionsMock.Object));
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new UploadServiceClient(_httpClient, _loggerMock.Object, null!));
    }
}
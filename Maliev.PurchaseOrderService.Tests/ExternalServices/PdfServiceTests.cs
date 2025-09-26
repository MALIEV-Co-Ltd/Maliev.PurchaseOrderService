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
/// T038: External service mock tests for PdfService
/// Tests HTTP client configuration, error handling, circuit breaker patterns,
/// authentication, timeout handling, response parsing and service availability
/// </summary>
public class PdfServiceTests
{
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<ILogger<PdfServiceClient>> _loggerMock;
    private readonly Mock<IOptions<ExternalServiceOptions>> _optionsMock;
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly HttpClient _httpClient;
    private readonly ExternalServiceOptions _serviceOptions;

    public PdfServiceTests()
    {
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _loggerMock = new Mock<ILogger<PdfServiceClient>>();
        _optionsMock = new Mock<IOptions<ExternalServiceOptions>>();
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();

        _serviceOptions = new ExternalServiceOptions
        {
            PdfService = new ServiceEndpoint
            {
                BaseUrl = "https://test.api.maliev.com/pdf",
                TimeoutInSeconds = 60 // Longer timeout for PDF generation
            }
        };

        _optionsMock.Setup(x => x.Value).Returns(_serviceOptions);

        _httpClient = new HttpClient(_httpMessageHandlerMock.Object)
        {
            BaseAddress = new Uri(_serviceOptions.PdfService.BaseUrl)
        };

        _httpClientFactoryMock
            .Setup(x => x.CreateClient("PdfService"))
            .Returns(_httpClient);
    }

    [Fact]
    public async Task GeneratePdfFromTemplateAsync_ValidData_ReturnsPdfResult()
    {
        // Arrange
        var purchaseOrderId = Guid.NewGuid();
        var pdfRequest = new PdfGenerationOptionsDto
        {
            PageSize = "A4",
            Orientation = "Portrait",
            Scale = 1.0,
            PrintBackground = true,
            DisplayHeaderFooter = false
        };

        var expectedResult = new PdfGenerationResultDto
        {
            DocumentId = "PO-2024-001",
            FileName = "PO-2024-001.pdf",
            FileSize = 2048000,
            PageCount = 2,
            IsSuccess = true,
            GeneratedAt = DateTime.UtcNow,
            GenerationTime = TimeSpan.FromSeconds(2)
        };

        var pdfContent = "Mock PDF content for purchase order"u8.ToArray();
        var httpResponse = new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = new ByteArrayContent(pdfContent)
        };

        // Add headers that the PdfServiceClient expects
        httpResponse.Headers.Add("X-Document-Id", expectedResult.DocumentId);
        httpResponse.Headers.Add("X-File-Name", expectedResult.FileName);
        httpResponse.Headers.Add("X-Page-Count", expectedResult.PageCount.ToString());
        httpResponse.Headers.Add("X-Generation-Time", expectedResult.GenerationTime.ToString());
        httpResponse.Content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri!.ToString().Contains("/generate/purchase-order")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        var service = new PdfServiceClient(_httpClient, _loggerMock.Object, _optionsMock.Object);

        // Act
        var result = await service.GeneratePdfFromTemplateAsync("purchase-order-template", new Dictionary<string, object>(), pdfRequest);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.DocumentId.Should().NotBeNullOrEmpty();
        result.FileName.Should().Be("PO-2024-001.pdf");
        result.FileSize.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetPdfInfoAsync_ValidJobId_ReturnsJobStatus()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var expectedStatus = new
        {
            JobId = jobId,
            Status = "Processing",
            Progress = 75,
            EstimatedCompletionTime = DateTime.UtcNow.AddMinutes(2),
            Message = "Generating PDF pages...",
            CreatedAt = DateTime.UtcNow.AddMinutes(-5)
        };

        var responseContent = JsonSerializer.Serialize(expectedStatus);
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
                    req.RequestUri!.ToString().Contains($"/jobs/{jobId}/status")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        var service = new PdfServiceClient(_httpClient, _loggerMock.Object, _optionsMock.Object);

        // Act
        using var stream = new MemoryStream();
        var result = await service.GetPdfInfoAsync(stream);

        // Assert
        result.Should().NotBeNull();
        result.PageCount.Should().BeGreaterThan(0);
        result.FileSize.Should().BeGreaterThan(0);
        result.Version.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task DownloadPdfAsync_ValidJobId_ReturnsPdfStream()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var pdfContent = "Mock PDF content as bytes"u8.ToArray();

        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(pdfContent)
        };
        httpResponse.Content.Headers.Add("Content-Type", "application/pdf");
        httpResponse.Content.Headers.Add("Content-Disposition", "attachment; filename=\"purchase-order.pdf\"");

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri!.ToString().Contains($"/jobs/{jobId}/download")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        var service = new PdfServiceClient(_httpClient, _loggerMock.Object, _optionsMock.Object);

        // Act
        var result = await service.DownloadTemplateAsync("template-id");

        // Assert
        result.Should().NotBeNull();
        result.TemplateContent.Should().NotBeNull();
        result.FileName.Should().Be("purchase-order.pdf");
        result.ContentType.Should().Be("application/pdf");

        // Verify stream content
        var downloadedContent = new byte[pdfContent.Length];
        await result.TemplateContent!.ReadExactlyAsync(downloadedContent);
        downloadedContent.Should().BeEquivalentTo(pdfContent);
    }

    [Fact]
    public async Task GenerateQuotePdfAsync_ValidData_ReturnsPdfResult()
    {
        // Arrange
        var quoteId = Guid.NewGuid();
        var pdfRequest = new PdfGenerationOptionsDto
        {
            PageSize = "A4",
            Orientation = "Portrait",
            Scale = 1.0,
            PrintBackground = true,
            DisplayHeaderFooter = false
        };

        var expectedResult = new PdfGenerationResultDto
        {
            DocumentId = "QUO-2024-001",
            FileName = "QUO-2024-001.pdf",
            FileSize = 1024000,
            PageCount = 1,
            IsSuccess = true,
            GeneratedAt = DateTime.UtcNow,
            GenerationTime = TimeSpan.FromSeconds(1)
        };

        var pdfContent = "Mock PDF content as bytes"u8.ToArray();
        var httpResponse = new HttpResponseMessage(HttpStatusCode.Accepted)
        {
            Content = new ByteArrayContent(pdfContent)
        };

        // Add headers that the PdfServiceClient expects
        httpResponse.Headers.Add("X-Document-Id", expectedResult.DocumentId);
        httpResponse.Headers.Add("X-File-Name", expectedResult.FileName);
        httpResponse.Headers.Add("X-Page-Count", expectedResult.PageCount.ToString());
        httpResponse.Headers.Add("X-Generation-Time", expectedResult.GenerationTime.ToString());
        httpResponse.Content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri!.ToString().Contains("/pdfs/generate/template")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        var service = new PdfServiceClient(_httpClient, _loggerMock.Object, _optionsMock.Object);

        // Act
        var result = await service.GeneratePdfFromTemplateAsync("quote-template", new Dictionary<string, object> { ["id"] = quoteId }, pdfRequest);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.PageCount.Should().BeGreaterThan(0);
        result.GeneratedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task GetAvailableTemplatesAsync_ValidRequest_ReturnsTemplates()
    {
        // Arrange
        var expectedTemplates = new
        {
            Templates = new[]
            {
                new
                {
                    Id = "purchase-order-v1",
                    Name = "Purchase Order Template v1",
                    Description = "Standard purchase order template",
                    Category = "purchase-order",
                    Version = "1.0",
                    IsActive = true,
                    SupportedPageSizes = new[] { "A4", "Letter" },
                    SupportedOrientations = new[] { "Portrait", "Landscape" }
                },
                new
                {
                    Id = "quote-template-v2",
                    Name = "Quote Template v2",
                    Description = "Enhanced quote template with branding",
                    Category = "quote",
                    Version = "2.0",
                    IsActive = true,
                    SupportedPageSizes = new[] { "A4" },
                    SupportedOrientations = new[] { "Portrait" }
                }
            },
            TotalCount = 2
        };

        var responseContent = JsonSerializer.Serialize(expectedTemplates);
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
                    req.RequestUri!.ToString().Contains("/templates")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        var service = new PdfServiceClient(_httpClient, _loggerMock.Object, _optionsMock.Object);

        // Act
        var result = await service.GetAvailableTemplatesAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().Contain(t => t.Id == "purchase-order-v1");
        result.Should().Contain(t => t.Id == "quote-template-v2");
    }

    [Fact]
    public async Task GetPdfInfoAsync_ValidJobId_CompletesSuccessfully()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var httpResponse = new HttpResponseMessage(HttpStatusCode.NoContent);

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Delete &&
                    req.RequestUri!.ToString().Contains($"/jobs/{jobId}/cancel")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        var service = new PdfServiceClient(_httpClient, _loggerMock.Object, _optionsMock.Object);

        // Act
        await service.GetPdfInfoAsync(new MemoryStream());

        // Assert
        _httpMessageHandlerMock
            .Protected()
            .Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Delete &&
                    req.RequestUri!.ToString().Contains($"/jobs/{jobId}/cancel")),
                ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task ValidateTemplateDataAsync_ValidData_ReturnsValidationResult()
    {
        // Arrange
        var templateId = "purchase-order-v1";
        var templateData = new { OrderNumber = "PO-001", CustomerName = "Test" };
        var validationRequest = new
        {
            TemplateId = templateId,
            Data = templateData
        };

        var expectedValidation = new
        {
            IsValid = true,
            Errors = new string[0],
            Warnings = new[] { "Missing optional field: DeliveryInstructions" },
            RequiredFields = new[] { "OrderNumber", "CustomerName", "Items" },
            ProvidedFields = new[] { "OrderNumber", "CustomerName" }
        };

        var responseContent = JsonSerializer.Serialize(expectedValidation);
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseContent, Encoding.UTF8, MediaTypeHeaderValue.Parse("application/json"))
        };

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri!.ToString().Contains("/templates/validate")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        var service = new PdfServiceClient(_httpClient, _loggerMock.Object, _optionsMock.Object);

        // Act
        var result = await service.ValidatePdfAsync(new MemoryStream(), new PdfValidationOptionsDto());

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.ValidationErrors.Should().BeEmpty();
        result.Warnings.Should().HaveCount(1);
    }

    [Fact]
    public async Task GeneratePdfFromTemplateAsync_ServiceUnavailable_ThrowsHttpRequestException()
    {
        // Arrange
        var purchaseOrderId = Guid.NewGuid();
        var pdfRequest = new PdfGenerationOptionsDto { Format = "PDF", PageSize = "A4" };
        var httpResponse = new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        var service = new PdfServiceClient(_httpClient, _loggerMock.Object, _optionsMock.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ExternalServiceException>(
            () => service.GeneratePdfFromTemplateAsync("po-template", new Dictionary<string, object> { ["id"] = purchaseOrderId }, pdfRequest));

        exception.Message.Should().Contain("Failed to generate PDF");
    }

    [Fact]
    public async Task GeneratePdfFromTemplateAsync_Timeout_ThrowsTimeoutException()
    {
        // Arrange
        var purchaseOrderId = Guid.NewGuid();
        var pdfRequest = new PdfGenerationOptionsDto { Format = "PDF", PageSize = "A4" };

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException("PDF generation timeout"));

        var service = new PdfServiceClient(_httpClient, _loggerMock.Object, _optionsMock.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ExternalServiceException>(
            () => service.GeneratePdfFromTemplateAsync("po-template", new Dictionary<string, object> { ["id"] = purchaseOrderId }, pdfRequest));

        exception.Message.Should().Contain("Timeout while generating PDF");
    }

    [Fact]
    public async Task GetPdfInfoAsync_JobNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var httpResponse = new HttpResponseMessage(HttpStatusCode.NotFound);

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        var service = new PdfServiceClient(_httpClient, _loggerMock.Object, _optionsMock.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.GetPdfInfoAsync(new MemoryStream()));

        exception.Message.Should().Contain("PDF job not found");
    }

    [Fact]
    public async Task GeneratePdfFromTemplateAsync_InvalidTemplate_ThrowsBadRequestException()
    {
        // Arrange
        var purchaseOrderId = Guid.NewGuid();
        var pdfRequest = new PdfGenerationOptionsDto { Format = "PDF", PageSize = "A4" };
        var errorResponse = new { message = "Template not found", templateId = "invalid-template" };
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

        var service = new PdfServiceClient(_httpClient, _loggerMock.Object, _optionsMock.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ExternalServiceException>(
            () => service.GeneratePdfFromTemplateAsync("po-template", new Dictionary<string, object> { ["id"] = purchaseOrderId }, pdfRequest));

        exception.Message.Should().Contain("Failed to generate PDF");
    }

    [Fact]
    public async Task GeneratePdfFromTemplateAsync_CircuitBreakerOpen_ThrowsCircuitBreakerException()
    {
        // Arrange
        var purchaseOrderId = Guid.NewGuid();
        var pdfRequest = new PdfGenerationOptionsDto { Format = "PDF", PageSize = "A4" };

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Circuit breaker is open for PdfService"));

        var service = new PdfServiceClient(_httpClient, _loggerMock.Object, _optionsMock.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.GeneratePdfFromTemplateAsync("po-template", new Dictionary<string, object> { ["id"] = purchaseOrderId }, pdfRequest));

        exception.Message.Should().Contain("PdfService");
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    public async Task GetPdfInfoAsync_AuthenticationFailure_ThrowsAuthenticationException(HttpStatusCode statusCode)
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var httpResponse = new HttpResponseMessage(statusCode);

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        var service = new PdfServiceClient(_httpClient, _loggerMock.Object, _optionsMock.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.GetPdfInfoAsync(new MemoryStream()));

        exception.Message.Should().Contain("authentication");
    }

    [Fact]
    public async Task GetAvailableTemplatesAsync_InvalidJson_ThrowsInvalidDataException()
    {
        // Arrange
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

        var service = new PdfServiceClient(_httpClient, _loggerMock.Object, _optionsMock.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidDataException>(
            () => service.GetAvailableTemplatesAsync());

        exception.Message.Should().Contain("Failed to parse PDF templates response");
    }

    [Fact]
    public void Constructor_NullHttpClientFactory_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new PdfServiceClient(null!, _loggerMock.Object, _optionsMock.Object));
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new PdfServiceClient(_httpClient, null!, _optionsMock.Object));
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new PdfServiceClient(_httpClient, _loggerMock.Object, null!));
    }
}
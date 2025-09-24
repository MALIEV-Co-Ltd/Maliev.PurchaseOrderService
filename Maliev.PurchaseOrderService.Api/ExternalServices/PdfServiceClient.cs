using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Maliev.PurchaseOrderService.Api.Configuration;
using Maliev.PurchaseOrderService.Api.DTOs;

namespace Maliev.PurchaseOrderService.Api.ExternalServices;

/// <summary>
/// HTTP client implementation for PDF Service integration
/// Handles PDF generation for internal POs and event-driven processing
/// </summary>
public class PdfServiceClient : IPdfServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PdfServiceClient> _logger;
    private readonly ExternalServiceOptions _options;
    private readonly JsonSerializerOptions _jsonOptions;

    public PdfServiceClient(
        HttpClient httpClient,
        ILogger<PdfServiceClient> logger,
        IOptions<ExternalServiceOptions> options)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
    }

    /// <inheritdoc />
    public async Task<PdfGenerationResultDto?> GeneratePdfFromHtmlAsync(
        string htmlContent,
        PdfGenerationOptionsDto options,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(htmlContent))
            {
                throw new ArgumentException("HTML content cannot be null or empty", nameof(htmlContent));
            }

            _logger.LogInformation("Generating PDF from HTML content, size: {ContentSize} characters", htmlContent.Length);

            var request = new
            {
                HtmlContent = htmlContent,
                Options = options
            };

            var jsonContent = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/pdfs/generate/html", content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = new PdfGenerationResultDto
            {
                DocumentId = GetHeaderValue(response.Headers, "X-Document-Id") ?? Guid.NewGuid().ToString(),
                FileName = GetHeaderValue(response.Headers, "X-File-Name") ?? "generated.pdf",
                PdfContent = await response.Content.ReadAsStreamAsync(cancellationToken),
                FileSize = response.Content.Headers.ContentLength ?? 0,
                IsSuccess = true,
                GeneratedAt = DateTime.UtcNow
            };

            // Try to get additional metadata from headers
            if (int.TryParse(GetHeaderValue(response.Headers, "X-Page-Count"), out var pageCount))
            {
                result.PageCount = pageCount;
            }

            if (TimeSpan.TryParse(GetHeaderValue(response.Headers, "X-Generation-Time"), out var generationTime))
            {
                result.GenerationTime = generationTime;
            }

            _logger.LogInformation("Successfully generated PDF from HTML, Document ID: {DocumentId}, Pages: {PageCount}",
                result.DocumentId, result.PageCount);
            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error occurred while generating PDF from HTML");
            throw new ExternalServiceException($"Failed to generate PDF from HTML: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogError(ex, "Timeout occurred while generating PDF from HTML");
            throw new ExternalServiceException("Timeout while generating PDF from HTML", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON serialization error while generating PDF from HTML");
            throw new ExternalServiceException("Invalid request format while generating PDF from HTML", ex);
        }
    }

    /// <inheritdoc />
    public async Task<PdfGenerationResultDto?> GeneratePdfFromTemplateAsync(
        string templateId,
        Dictionary<string, object> templateData,
        PdfGenerationOptionsDto options,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(templateId))
            {
                throw new ArgumentException("Template ID cannot be null or empty", nameof(templateId));
            }

            _logger.LogInformation("Generating PDF from template: {TemplateId}", templateId);

            var request = new
            {
                TemplateId = templateId,
                Data = templateData,
                Options = options
            };

            var jsonContent = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/pdfs/generate/template", content, cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Template not found: {TemplateId}", templateId);
                return null;
            }

            response.EnsureSuccessStatusCode();

            var result = new PdfGenerationResultDto
            {
                DocumentId = GetHeaderValue(response.Headers, "X-Document-Id") ?? Guid.NewGuid().ToString(),
                FileName = GetHeaderValue(response.Headers, "X-File-Name") ?? $"template-{templateId}.pdf",
                PdfContent = await response.Content.ReadAsStreamAsync(cancellationToken),
                FileSize = response.Content.Headers.ContentLength ?? 0,
                IsSuccess = true,
                GeneratedAt = DateTime.UtcNow
            };

            _logger.LogInformation("Successfully generated PDF from template: {TemplateId}, Document ID: {DocumentId}",
                templateId, result.DocumentId);
            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error occurred while generating PDF from template {TemplateId}", templateId);
            throw new ExternalServiceException($"Failed to generate PDF from template {templateId}: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogError(ex, "Timeout occurred while generating PDF from template {TemplateId}", templateId);
            throw new ExternalServiceException($"Timeout while generating PDF from template {templateId}", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON serialization error while generating PDF from template {TemplateId}", templateId);
            throw new ExternalServiceException($"Invalid request format while generating PDF from template {templateId}", ex);
        }
    }

    /// <inheritdoc />
    public async Task<PdfMergeResultDto?> MergePdfsAsync(
        IEnumerable<PdfFileInput> pdfFiles,
        PdfMergeOptionsDto options,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var fileList = pdfFiles.ToList();
            if (!fileList.Any())
            {
                throw new ArgumentException("PDF files list cannot be empty", nameof(pdfFiles));
            }

            _logger.LogInformation("Merging {FileCount} PDF files", fileList.Count);

            using var multipartContent = new MultipartFormDataContent();

            // Add options
            var optionsJson = JsonSerializer.Serialize(options, _jsonOptions);
            multipartContent.Add(new StringContent(optionsJson, Encoding.UTF8, "application/json"), "options");

            // Add each PDF file
            for (int i = 0; i < fileList.Count; i++)
            {
                var pdfFile = fileList[i];
                var fileContent = new StreamContent(pdfFile.Content);
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");
                multipartContent.Add(fileContent, $"files", pdfFile.FileName);

                if (!string.IsNullOrEmpty(pdfFile.Password))
                {
                    multipartContent.Add(new StringContent(pdfFile.Password), $"password_{i}");
                }
            }

            var response = await _httpClient.PostAsync("/pdfs/merge", multipartContent, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = new PdfMergeResultDto
            {
                DocumentId = GetHeaderValue(response.Headers, "X-Document-Id") ?? Guid.NewGuid().ToString(),
                FileName = options.OutputFileName,
                MergedPdfContent = await response.Content.ReadAsStreamAsync(cancellationToken),
                FileSize = response.Content.Headers.ContentLength ?? 0,
                InputFileCount = fileList.Count,
                IsSuccess = true,
                MergedAt = DateTime.UtcNow
            };

            _logger.LogInformation("Successfully merged {FileCount} PDF files, Document ID: {DocumentId}",
                fileList.Count, result.DocumentId);
            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error occurred while merging PDF files");
            throw new ExternalServiceException($"Failed to merge PDF files: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogError(ex, "Timeout occurred while merging PDF files");
            throw new ExternalServiceException("Timeout while merging PDF files", ex);
        }
    }

    /// <inheritdoc />
    public async Task<PdfToImageResultDto?> ConvertPdfToImagesAsync(
        Stream pdfStream,
        PdfToImageOptionsDto options,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (pdfStream == null || !pdfStream.CanRead)
            {
                throw new ArgumentException("PDF stream must be readable", nameof(pdfStream));
            }

            _logger.LogInformation("Converting PDF to images with format: {ImageFormat}, DPI: {DPI}",
                options.ImageFormat, options.DPI);

            using var multipartContent = new MultipartFormDataContent();

            var pdfContent = new StreamContent(pdfStream);
            pdfContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");
            multipartContent.Add(pdfContent, "pdf", "document.pdf");

            var optionsJson = JsonSerializer.Serialize(options, _jsonOptions);
            multipartContent.Add(new StringContent(optionsJson, Encoding.UTF8, "application/json"), "options");

            var response = await _httpClient.PostAsync("/pdfs/convert/images", multipartContent, cancellationToken);
            response.EnsureSuccessStatusCode();

            // For simplicity, returning a basic result. In practice, you might parse a JSON response
            // containing multiple image streams or URLs
            var result = new PdfToImageResultDto
            {
                ConversionId = GetHeaderValue(response.Headers, "X-Conversion-Id") ?? Guid.NewGuid().ToString(),
                IsSuccess = true,
                ConvertedAt = DateTime.UtcNow
            };

            _logger.LogInformation("Successfully converted PDF to images, Conversion ID: {ConversionId}",
                result.ConversionId);
            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error occurred while converting PDF to images");
            throw new ExternalServiceException($"Failed to convert PDF to images: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogError(ex, "Timeout occurred while converting PDF to images");
            throw new ExternalServiceException("Timeout while converting PDF to images", ex);
        }
    }

    /// <inheritdoc />
    public async Task<PdfTextExtractionResultDto?> ExtractTextFromPdfAsync(
        Stream pdfStream,
        PdfTextExtractionOptionsDto options,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (pdfStream == null || !pdfStream.CanRead)
            {
                throw new ArgumentException("PDF stream must be readable", nameof(pdfStream));
            }

            _logger.LogInformation("Extracting text from PDF with options: PreserveLayout={PreserveLayout}",
                options.PreserveLayout);

            using var multipartContent = new MultipartFormDataContent();

            var pdfContent = new StreamContent(pdfStream);
            pdfContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");
            multipartContent.Add(pdfContent, "pdf", "document.pdf");

            var optionsJson = JsonSerializer.Serialize(options, _jsonOptions);
            multipartContent.Add(new StringContent(optionsJson, Encoding.UTF8, "application/json"), "options");

            var response = await _httpClient.PostAsync("/pdfs/extract-text", multipartContent, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<PdfTextExtractionResultDto>(responseContent, _jsonOptions);

            if (result != null)
            {
                result.ExtractedAt = DateTime.UtcNow;
                _logger.LogInformation("Successfully extracted text from PDF, Document ID: {DocumentId}, Pages: {PageCount}",
                    result.DocumentId, result.TotalPages);
            }

            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error occurred while extracting text from PDF");
            throw new ExternalServiceException($"Failed to extract text from PDF: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogError(ex, "Timeout occurred while extracting text from PDF");
            throw new ExternalServiceException("Timeout while extracting text from PDF", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON deserialization error while extracting text from PDF");
            throw new ExternalServiceException("Invalid response format while extracting text from PDF", ex);
        }
    }

    /// <inheritdoc />
    public async Task<PdfWatermarkResultDto?> AddWatermarkToPdfAsync(
        Stream pdfStream,
        PdfWatermarkOptionsDto watermarkOptions,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (pdfStream == null || !pdfStream.CanRead)
            {
                throw new ArgumentException("PDF stream must be readable", nameof(pdfStream));
            }

            _logger.LogInformation("Adding watermark to PDF with text: {WatermarkText}", watermarkOptions.Text);

            using var multipartContent = new MultipartFormDataContent();

            var pdfContent = new StreamContent(pdfStream);
            pdfContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");
            multipartContent.Add(pdfContent, "pdf", "document.pdf");

            // Add watermark image if provided
            if (watermarkOptions.ImageStream != null && watermarkOptions.ImageStream.CanRead)
            {
                var imageContent = new StreamContent(watermarkOptions.ImageStream);
                multipartContent.Add(imageContent, "watermarkImage", "watermark.png");
            }

            var optionsJson = JsonSerializer.Serialize(watermarkOptions, _jsonOptions);
            multipartContent.Add(new StringContent(optionsJson, Encoding.UTF8, "application/json"), "options");

            var response = await _httpClient.PostAsync("/pdfs/watermark", multipartContent, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = new PdfWatermarkResultDto
            {
                DocumentId = GetHeaderValue(response.Headers, "X-Document-Id") ?? Guid.NewGuid().ToString(),
                FileName = GetHeaderValue(response.Headers, "X-File-Name") ?? "watermarked.pdf",
                WatermarkedPdfContent = await response.Content.ReadAsStreamAsync(cancellationToken),
                FileSize = response.Content.Headers.ContentLength ?? 0,
                IsSuccess = true,
                ProcessedAt = DateTime.UtcNow
            };

            _logger.LogInformation("Successfully added watermark to PDF, Document ID: {DocumentId}", result.DocumentId);
            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error occurred while adding watermark to PDF");
            throw new ExternalServiceException($"Failed to add watermark to PDF: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogError(ex, "Timeout occurred while adding watermark to PDF");
            throw new ExternalServiceException("Timeout while adding watermark to PDF", ex);
        }
    }

    /// <inheritdoc />
    public async Task<PdfInfoDto?> GetPdfInfoAsync(Stream pdfStream, CancellationToken cancellationToken = default)
    {
        try
        {
            if (pdfStream == null || !pdfStream.CanRead)
            {
                throw new ArgumentException("PDF stream must be readable", nameof(pdfStream));
            }

            _logger.LogInformation("Getting PDF information");

            using var multipartContent = new MultipartFormDataContent();

            var pdfContent = new StreamContent(pdfStream);
            pdfContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");
            multipartContent.Add(pdfContent, "pdf", "document.pdf");

            var response = await _httpClient.PostAsync("/pdfs/info", multipartContent, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<PdfInfoDto>(responseContent, _jsonOptions);

            _logger.LogInformation("Successfully retrieved PDF information, Pages: {PageCount}", result?.PageCount);
            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error occurred while getting PDF information");
            throw new ExternalServiceException($"Failed to get PDF information: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogError(ex, "Timeout occurred while getting PDF information");
            throw new ExternalServiceException("Timeout while getting PDF information", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON deserialization error while getting PDF information");
            throw new ExternalServiceException("Invalid response format while getting PDF information", ex);
        }
    }

    /// <inheritdoc />
    public async Task<PdfValidationResultDto> ValidatePdfAsync(
        Stream pdfStream,
        PdfValidationOptionsDto validationOptions,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (pdfStream == null || !pdfStream.CanRead)
            {
                throw new ArgumentException("PDF stream must be readable", nameof(pdfStream));
            }

            _logger.LogInformation("Validating PDF with options: CheckStructure={CheckStructure}",
                validationOptions.CheckStructure);

            using var multipartContent = new MultipartFormDataContent();

            var pdfContent = new StreamContent(pdfStream);
            pdfContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");
            multipartContent.Add(pdfContent, "pdf", "document.pdf");

            var optionsJson = JsonSerializer.Serialize(validationOptions, _jsonOptions);
            multipartContent.Add(new StringContent(optionsJson, Encoding.UTF8, "application/json"), "options");

            var response = await _httpClient.PostAsync("/pdfs/validate", multipartContent, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<PdfValidationResultDto>(responseContent, _jsonOptions) ??
                        new PdfValidationResultDto
                        {
                            IsValid = false,
                            ValidationErrors = new List<string> { "Invalid response" },
                            ValidatedAt = DateTime.UtcNow
                        };

            result.ValidatedAt = DateTime.UtcNow;
            _logger.LogInformation("PDF validation completed, IsValid: {IsValid}", result.IsValid);
            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error occurred while validating PDF");
            throw new ExternalServiceException($"Failed to validate PDF: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogError(ex, "Timeout occurred while validating PDF");
            throw new ExternalServiceException("Timeout while validating PDF", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON deserialization error while validating PDF");
            throw new ExternalServiceException("Invalid response format while validating PDF", ex);
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<PdfTemplateDto>> GetAvailableTemplatesAsync(
        string? category = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting available PDF templates for category: {Category}", category ?? "All");

            var url = "/pdfs/templates";
            if (!string.IsNullOrWhiteSpace(category))
            {
                url += $"?category={Uri.EscapeDataString(category)}";
            }

            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var templateResponse = JsonSerializer.Deserialize<PdfTemplateListDto>(content, _jsonOptions);

            var templates = templateResponse?.Templates ?? Enumerable.Empty<PdfTemplateDto>();

            _logger.LogInformation("Successfully retrieved {TemplateCount} PDF templates", templates.Count());
            return templates;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error occurred while getting PDF templates");
            throw new ExternalServiceException($"Failed to get PDF templates: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogError(ex, "Timeout occurred while getting PDF templates");
            throw new ExternalServiceException("Timeout while getting PDF templates", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON deserialization error while getting PDF templates");
            throw new ExternalServiceException("Invalid response format while getting PDF templates", ex);
        }
    }

    /// <inheritdoc />
    public async Task<PdfTemplateDownloadDto?> DownloadTemplateAsync(string templateId, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(templateId))
            {
                throw new ArgumentException("Template ID cannot be null or empty", nameof(templateId));
            }

            _logger.LogInformation("Downloading PDF template: {TemplateId}", templateId);

            var response = await _httpClient.GetAsync($"/pdfs/templates/{templateId}/download", cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("PDF template not found: {TemplateId}", templateId);
                return null;
            }

            response.EnsureSuccessStatusCode();

            var result = new PdfTemplateDownloadDto
            {
                TemplateId = templateId,
                FileName = GetHeaderValue(response.Headers, "X-File-Name") ?? $"template-{templateId}.html",
                TemplateContent = await response.Content.ReadAsStreamAsync(cancellationToken),
                ContentType = response.Content.Headers.ContentType?.MediaType ?? "text/html",
                FileSize = response.Content.Headers.ContentLength ?? 0,
                DownloadedAt = DateTime.UtcNow
            };

            _logger.LogInformation("Successfully downloaded PDF template: {TemplateId}", templateId);
            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error occurred while downloading PDF template {TemplateId}", templateId);
            throw new ExternalServiceException($"Failed to download PDF template {templateId}: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogError(ex, "Timeout occurred while downloading PDF template {TemplateId}", templateId);
            throw new ExternalServiceException($"Timeout while downloading PDF template {templateId}", ex);
        }
    }

    private static string? GetHeaderValue(System.Net.Http.Headers.HttpResponseHeaders headers, string headerName)
    {
        return headers.TryGetValues(headerName, out var values) ? values.FirstOrDefault() : null;
    }
}
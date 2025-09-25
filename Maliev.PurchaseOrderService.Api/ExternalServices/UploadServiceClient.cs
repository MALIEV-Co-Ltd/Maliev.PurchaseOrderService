using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Maliev.PurchaseOrderService.Api.Configuration;
using Maliev.PurchaseOrderService.Api.DTOs;

namespace Maliev.PurchaseOrderService.Api.ExternalServices;

/// <summary>
/// HTTP client implementation for Upload Service integration
/// Handles document upload/download and virus scanning
/// </summary>
public class UploadServiceClient : IUploadServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<UploadServiceClient> _logger;
    private readonly ExternalServiceOptions _options;
    private readonly JsonSerializerOptions _jsonOptions;

    public UploadServiceClient(
        HttpClient httpClient,
        ILogger<UploadServiceClient> logger,
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
    public async Task<FileUploadResultDto?> UploadFileAsync(
        Stream file,
        string fileName,
        string contentType,
        string category,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (file == null || !file.CanRead)
            {
                throw new ArgumentException("File stream must be readable", nameof(file));
            }

            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException("File name cannot be null or empty", nameof(fileName));
            }

            _logger.LogInformation("Uploading file: {FileName} to category: {Category}", fileName, category);

            using var multipartContent = new MultipartFormDataContent();

            // Add file content
            var fileContent = new StreamContent(file);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
            multipartContent.Add(fileContent, "file", fileName);

            // Add metadata
            multipartContent.Add(new StringContent(category), "category");
            multipartContent.Add(new StringContent(fileName), "fileName");
            multipartContent.Add(new StringContent(contentType), "contentType");

            var response = await _httpClient.PostAsync("/files/upload", multipartContent, cancellationToken);

            if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden)
            {
                _logger.LogError("Authentication failure while uploading file: {FileName}", fileName);
                throw new UnauthorizedAccessException($"Upload service authentication failed while uploading file {fileName}");
            }

            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<FileUploadResultDto>(responseContent, _jsonOptions);

            _logger.LogInformation("Successfully uploaded file: {FileName} with ID: {FileId}",
                fileName, result?.FileId);
            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error occurred while uploading file: {FileName}", fileName);
            throw new ExternalServiceException($"Failed to upload file {fileName}: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout occurred while uploading file: {FileName}", fileName);
            throw new ExternalServiceException($"Timeout while uploading file {fileName}", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON deserialization error while uploading file: {FileName}", fileName);
            throw new ExternalServiceException($"Invalid response format while uploading file {fileName}", ex);
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<FileUploadResultDto>> UploadMultipleFilesAsync(
        IEnumerable<FileUploadRequest> files,
        string category,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var fileList = files.ToList();
            if (!fileList.Any())
            {
                return Enumerable.Empty<FileUploadResultDto>();
            }

            _logger.LogInformation("Uploading {FileCount} files to category: {Category}", fileList.Count, category);

            using var multipartContent = new MultipartFormDataContent();

            // Add category
            multipartContent.Add(new StringContent(category), "category");

            // Add each file
            for (int i = 0; i < fileList.Count; i++)
            {
                var fileRequest = fileList[i];
                var fileContent = new StreamContent(fileRequest.Content);
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(fileRequest.ContentType);
                multipartContent.Add(fileContent, $"files", fileRequest.FileName);

                // Add file metadata
                if (fileRequest.Metadata.Any())
                {
                    var metadataJson = JsonSerializer.Serialize(fileRequest.Metadata, _jsonOptions);
                    multipartContent.Add(new StringContent(metadataJson), $"metadata_{i}");
                }
            }

            var response = await _httpClient.PostAsync("/files/upload-multiple", multipartContent, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var results = JsonSerializer.Deserialize<IEnumerable<FileUploadResultDto>>(responseContent, _jsonOptions) ??
                         Enumerable.Empty<FileUploadResultDto>();

            _logger.LogInformation("Successfully uploaded {FileCount} files", results.Count());
            return results;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error occurred while uploading multiple files");
            throw new ExternalServiceException($"Failed to upload multiple files: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout occurred while uploading multiple files");
            throw new ExternalServiceException("Timeout while uploading multiple files", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON deserialization error while uploading multiple files");
            throw new ExternalServiceException("Invalid response format while uploading multiple files", ex);
        }
    }

    /// <inheritdoc />
    public async Task<FileInfoDto?> GetFileInfoAsync(string fileId, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(fileId))
            {
                throw new ArgumentException("File ID cannot be null or empty", nameof(fileId));
            }

            _logger.LogInformation("Getting file information for ID: {FileId}", fileId);

            var response = await _httpClient.GetAsync($"/files/{fileId}/info", cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("File information not found for ID: {FileId}", fileId);
                return null;
            }

            if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden)
            {
                _logger.LogError("Authentication failure while getting file info {FileId}", fileId);
                throw new UnauthorizedAccessException($"Upload service authentication failed while getting file info {fileId}");
            }

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var fileInfo = JsonSerializer.Deserialize<FileInfoDto>(content, _jsonOptions);

            _logger.LogInformation("Successfully retrieved file information for ID: {FileId}", fileId);
            return fileInfo;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error occurred while getting file info {FileId}", fileId);
            throw new ExternalServiceException($"Failed to get file info {fileId}: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout occurred while getting file info {FileId}", fileId);
            throw new ExternalServiceException($"Timeout while getting file info {fileId}", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON deserialization error while getting file info {FileId}", fileId);
            throw new ExternalServiceException($"Invalid response format while getting file info {fileId}", ex);
        }
    }

    /// <inheritdoc />
    public async Task<FileDownloadResultDto?> DownloadFileAsync(string fileId, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(fileId))
            {
                throw new ArgumentException("File ID cannot be null or empty", nameof(fileId));
            }

            _logger.LogInformation("Downloading file with ID: {FileId}", fileId);

            var response = await _httpClient.GetAsync($"/files/{fileId}/download", cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("File not found for download, ID: {FileId}", fileId);
                return null;
            }

            response.EnsureSuccessStatusCode();

            var result = new FileDownloadResultDto
            {
                FileId = fileId,
                FileName = GetFileNameFromResponse(response) ?? "unknown",
                ContentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream",
                FileSize = response.Content.Headers.ContentLength ?? 0,
                Content = await response.Content.ReadAsStreamAsync(cancellationToken),
                LastModified = response.Content.Headers.LastModified?.DateTime ?? DateTime.UtcNow,
                ETag = response.Headers.ETag?.Tag ?? string.Empty
            };

            // Copy response headers
            foreach (var header in response.Headers)
            {
                result.Headers[header.Key] = string.Join(", ", header.Value);
            }

            _logger.LogInformation("Successfully downloaded file with ID: {FileId}", fileId);
            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error occurred while downloading file {FileId}", fileId);
            throw new ExternalServiceException($"Failed to download file {fileId}: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout occurred while downloading file {FileId}", fileId);
            throw new ExternalServiceException($"Timeout while downloading file {fileId}", ex);
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeleteFileAsync(string fileId, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(fileId))
            {
                throw new ArgumentException("File ID cannot be null or empty", nameof(fileId));
            }

            _logger.LogInformation("Deleting file with ID: {FileId}", fileId);

            var response = await _httpClient.DeleteAsync($"/files/{fileId}", cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("File not found for deletion, ID: {FileId}", fileId);
                return false;
            }

            response.EnsureSuccessStatusCode();

            _logger.LogInformation("Successfully deleted file with ID: {FileId}", fileId);
            return true;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error occurred while deleting file {FileId}", fileId);
            throw new ExternalServiceException($"Failed to delete file {fileId}: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout occurred while deleting file {FileId}", fileId);
            throw new ExternalServiceException($"Timeout while deleting file {fileId}", ex);
        }
    }

    /// <inheritdoc />
    public async Task<FileDownloadUrlDto?> GetDownloadUrlAsync(
        string fileId,
        int expirationMinutes = 60,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(fileId))
            {
                throw new ArgumentException("File ID cannot be null or empty", nameof(fileId));
            }

            _logger.LogInformation("Getting download URL for file ID: {FileId} with expiration: {ExpirationMinutes} minutes",
                fileId, expirationMinutes);

            var response = await _httpClient.GetAsync(
                $"/files/{fileId}/download-url?expirationMinutes={expirationMinutes}",
                cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("File not found for download URL, ID: {FileId}", fileId);
                return null;
            }

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var downloadUrl = JsonSerializer.Deserialize<FileDownloadUrlDto>(content, _jsonOptions);

            _logger.LogInformation("Successfully generated download URL for file ID: {FileId}", fileId);
            return downloadUrl;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error occurred while getting download URL for file {FileId}", fileId);
            throw new ExternalServiceException($"Failed to get download URL for file {fileId}: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout occurred while getting download URL for file {FileId}", fileId);
            throw new ExternalServiceException($"Timeout while getting download URL for file {fileId}", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON deserialization error while getting download URL for file {FileId}", fileId);
            throw new ExternalServiceException($"Invalid response format while getting download URL for file {fileId}", ex);
        }
    }

    /// <inheritdoc />
    public async Task<FileValidationResultDto> ValidateFileAsync(
        string fileName,
        long fileSize,
        string contentType,
        string category,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Validating file: {FileName}, Size: {FileSize}, Type: {ContentType}, Category: {Category}",
                fileName, fileSize, contentType, category);

            var validationRequest = new
            {
                FileName = fileName,
                FileSize = fileSize,
                ContentType = contentType,
                Category = category
            };

            var jsonContent = JsonSerializer.Serialize(validationRequest, _jsonOptions);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/files/validate", content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<FileValidationResultDto>(responseContent, _jsonOptions) ??
                        new FileValidationResultDto { IsValid = false, ValidationErrors = new List<string> { "Invalid response" } };

            _logger.LogInformation("File validation result for {FileName}: {IsValid}", fileName, result.IsValid);
            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error occurred while validating file {FileName}", fileName);
            throw new ExternalServiceException($"Failed to validate file {fileName}: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout occurred while validating file {FileName}", fileName);
            throw new ExternalServiceException($"Timeout while validating file {fileName}", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON deserialization error while validating file {FileName}", fileName);
            throw new ExternalServiceException($"Invalid response format while validating file {fileName}", ex);
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<FileInfoDto>> GetFilesByCategoryAsync(
        string category,
        Dictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(category))
            {
                throw new ArgumentException("Category cannot be null or empty", nameof(category));
            }

            _logger.LogInformation("Getting files by category: {Category}", category);

            var queryParams = new List<string> { $"category={Uri.EscapeDataString(category)}" };

            if (metadata != null && metadata.Any())
            {
                foreach (var kvp in metadata)
                {
                    queryParams.Add($"metadata.{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}");
                }
            }

            var queryString = string.Join("&", queryParams);
            var response = await _httpClient.GetAsync($"/files/search?{queryString}", cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var files = JsonSerializer.Deserialize<IEnumerable<FileInfoDto>>(content, _jsonOptions) ??
                       Enumerable.Empty<FileInfoDto>();

            _logger.LogInformation("Successfully retrieved {FileCount} files for category: {Category}",
                files.Count(), category);
            return files;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error occurred while getting files by category {Category}", category);
            throw new ExternalServiceException($"Failed to get files by category {category}: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout occurred while getting files by category {Category}", category);
            throw new ExternalServiceException($"Timeout while getting files by category {category}", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON deserialization error while getting files by category {Category}", category);
            throw new ExternalServiceException($"Invalid response format while getting files by category {category}", ex);
        }
    }

    /// <inheritdoc />
    public async Task<UploadDto> GenerateUploadUrlAsync(string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Generating upload URL for file: {FileName}", fileName);

            var request = new { FileName = fileName, ContentType = contentType };
            var jsonContent = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/files/upload-url", content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var uploadInfo = JsonSerializer.Deserialize<UploadDto>(responseContent, _jsonOptions);

            _logger.LogInformation("Successfully generated upload URL for file: {FileName}", fileName);
            return uploadInfo!;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error occurred while generating upload URL for {FileName}", fileName);
            throw new ExternalServiceException($"Failed to generate upload URL for {fileName}: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout occurred while generating upload URL for {FileName}", fileName);
            throw new ExternalServiceException($"Timeout while generating upload URL for {fileName}", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON deserialization error while generating upload URL for {FileName}", fileName);
            throw new ExternalServiceException($"Invalid response format while generating upload URL for {fileName}", ex);
        }
    }

    /// <inheritdoc />
    public async Task<string> GenerateDownloadUrlAsync(string fileId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Generating download URL for file: {FileId}", fileId);

            var response = await _httpClient.GetAsync($"/files/{fileId}/download-url", cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var urlInfo = JsonSerializer.Deserialize<Dictionary<string, string>>(responseContent, _jsonOptions);

            var downloadUrl = urlInfo?.GetValueOrDefault("downloadUrl") ?? string.Empty;
            _logger.LogInformation("Successfully generated download URL for file: {FileId}", fileId);
            return downloadUrl;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error occurred while generating download URL for {FileId}", fileId);
            throw new ExternalServiceException($"Failed to generate download URL for {fileId}: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout occurred while generating download URL for {FileId}", fileId);
            throw new ExternalServiceException($"Timeout while generating download URL for {fileId}", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON deserialization error while generating download URL for {FileId}", fileId);
            throw new ExternalServiceException($"Invalid response format while generating download URL for {fileId}", ex);
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<FileInfoDto>> GetFilesByTagsAsync(
        string[] tags,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (tags == null || tags.Length == 0)
            {
                throw new ArgumentException("Tags array cannot be null or empty", nameof(tags));
            }

            _logger.LogInformation("Getting files by tags: {Tags}", string.Join(", ", tags));

            var tagsParam = string.Join(",", tags.Select(Uri.EscapeDataString));
            var response = await _httpClient.GetAsync($"/files/search?tags={tagsParam}", cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var searchResult = JsonSerializer.Deserialize<FileSearchResultDto>(content, _jsonOptions);
            var files = searchResult?.Files ?? Enumerable.Empty<FileInfoDto>();

            _logger.LogInformation("Successfully retrieved {FileCount} files for tags: {Tags}",
                files.Count(), string.Join(", ", tags));
            return files;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error occurred while getting files by tags {Tags}", string.Join(", ", tags));
            throw new ExternalServiceException($"Failed to get files by tags {string.Join(", ", tags)}: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout occurred while getting files by tags {Tags}", string.Join(", ", tags));
            throw new ExternalServiceException($"Timeout while getting files by tags {string.Join(", ", tags)}", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON deserialization error while getting files by tags {Tags}", string.Join(", ", tags));
            throw new ExternalServiceException($"Invalid response format while getting files by tags {string.Join(", ", tags)}", ex);
        }
    }

    /// <inheritdoc />
    public async Task<FileInfoDto?> UpdateFileMetadataAsync(
        string fileId,
        Dictionary<string, string> metadata,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(fileId))
            {
                throw new ArgumentException("File ID cannot be null or empty", nameof(fileId));
            }

            if (metadata == null || !metadata.Any())
            {
                throw new ArgumentException("Metadata cannot be null or empty", nameof(metadata));
            }

            _logger.LogInformation("Updating metadata for file ID: {FileId}", fileId);

            var requestBody = new { Metadata = metadata };
            var jsonContent = JsonSerializer.Serialize(requestBody, _jsonOptions);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await _httpClient.PatchAsync($"/files/{fileId}/metadata", content, cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning("File not found for metadata update, ID: {FileId}", fileId);
                throw new InvalidOperationException($"File not found for metadata update, ID: {fileId}");
            }

            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var updatedFileInfo = JsonSerializer.Deserialize<FileInfoDto>(responseContent, _jsonOptions);

            _logger.LogInformation("Successfully updated metadata for file ID: {FileId}", fileId);
            return updatedFileInfo;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error occurred while updating metadata for file {FileId}", fileId);
            throw new ExternalServiceException($"Failed to update metadata for file {fileId}: {ex.Message}", ex);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout occurred while updating metadata for file {FileId}", fileId);
            throw new ExternalServiceException($"Timeout while updating metadata for file {fileId}", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON error while updating metadata for file {FileId}", fileId);
            throw new ExternalServiceException($"Invalid response format while updating metadata for file {fileId}", ex);
        }
    }

    private static string? GetHeaderValue(System.Net.Http.Headers.HttpResponseHeaders headers, string headerName)
    {
        return headers.TryGetValues(headerName, out var values) ? values.FirstOrDefault() : null;
    }

    private static string? GetFileNameFromResponse(HttpResponseMessage response)
    {
        // First try X-File-Name header
        if (response.Headers.TryGetValues("X-File-Name", out var fileNameValues))
        {
            return fileNameValues.FirstOrDefault();
        }

        // Then try Content-Disposition header
        if (response.Content.Headers.ContentDisposition?.FileName != null)
        {
            var fileName = response.Content.Headers.ContentDisposition.FileName;
            // Remove quotes if present
            return fileName.Trim('"');
        }

        return null;
    }
}
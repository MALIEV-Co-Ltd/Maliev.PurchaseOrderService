namespace Maliev.PurchaseOrderService.Api.DTOs;

/// <summary>
/// Data Transfer Object for File Upload Result
/// </summary>
public class FileUploadResultDto
{
    public string FileId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public Dictionary<string, string> Metadata { get; set; } = new();
    public bool IsSuccess { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string Hash { get; set; } = string.Empty;
}

/// <summary>
/// Data Transfer Object for File Information
/// </summary>
public class FileInfoDto
{
    public string FileId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public Dictionary<string, string> Metadata { get; set; } = new();
    public DateTime UploadedAt { get; set; }
    public DateTime? LastAccessedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string UploadedBy { get; set; } = string.Empty;
    public string Hash { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public long DownloadCount { get; set; }
}

/// <summary>
/// Data Transfer Object for File Download Result
/// </summary>
public class FileDownloadResultDto : IDisposable
{
    public string FileId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public Stream? Content { get; set; }
    public Dictionary<string, string> Headers { get; set; } = new();
    public DateTime LastModified { get; set; }
    public string ETag { get; set; } = string.Empty;

    public void Dispose()
    {
        Content?.Dispose();
    }
}

/// <summary>
/// Data Transfer Object for File Download URL
/// </summary>
public class FileDownloadUrlDto
{
    public string FileId { get; set; } = string.Empty;
    public string DownloadUrl { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public int ExpirationMinutes { get; set; }
    public bool IsTemporary { get; set; }
    public Dictionary<string, string> Headers { get; set; } = new();
}

/// <summary>
/// Data Transfer Object for File Validation Result
/// </summary>
public class FileValidationResultDto
{
    public bool IsValid { get; set; }
    public List<string> ValidationErrors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public long MaxFileSizeBytes { get; set; }
    public List<string> AllowedContentTypes { get; set; } = new();
    public List<string> AllowedExtensions { get; set; } = new();
    public Dictionary<string, object> ValidationDetails { get; set; } = new();
}

/// <summary>
/// Request object for file upload
/// </summary>
public class FileUploadRequest
{
    public Stream Content { get; set; } = Stream.Null;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public Dictionary<string, string> Metadata { get; set; } = new();
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Data Transfer Object for File Search Request
/// </summary>
public class FileSearchRequest
{
    public string? Category { get; set; }
    public string? FileName { get; set; }
    public string? ContentType { get; set; }
    public DateTime? UploadedAfter { get; set; }
    public DateTime? UploadedBefore { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public string SortBy { get; set; } = "UploadedAt";
    public bool SortDescending { get; set; } = true;
}

/// <summary>
/// Data Transfer Object for File Search Result
/// </summary>
public class FileSearchResultDto
{
    public List<FileInfoDto> Files { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
}
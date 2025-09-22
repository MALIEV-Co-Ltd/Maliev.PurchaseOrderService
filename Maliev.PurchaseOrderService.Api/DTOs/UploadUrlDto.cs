namespace Maliev.PurchaseOrderService.Api.DTOs;

/// <summary>
/// Upload URL information for file uploads
/// </summary>
public class UploadDto
{
    /// <summary>
    /// Upload URL
    /// </summary>
    public string UploadUrl { get; set; } = string.Empty;

    /// <summary>
    /// File identifier
    /// </summary>
    public string FileId { get; set; } = string.Empty;

    /// <summary>
    /// URL expiration timestamp
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Generated timestamp
    /// </summary>
    public DateTime GeneratedAt { get; set; }

    /// <summary>
    /// Additional headers required for upload
    /// </summary>
    public Dictionary<string, string>? Headers { get; set; }
}
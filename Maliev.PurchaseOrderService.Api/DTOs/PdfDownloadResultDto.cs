namespace Maliev.PurchaseOrderService.Api.DTOs;

/// <summary>
/// Result of PDF download by job ID
/// </summary>
public class PdfDownloadResultDto
{
    /// <summary>
    /// The job ID for the PDF generation
    /// </summary>
    public Guid JobId { get; set; }

    /// <summary>
    /// PDF file name
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// PDF content type
    /// </summary>
    public string ContentType { get; set; } = "application/pdf";

    /// <summary>
    /// PDF content stream
    /// </summary>
    public Stream? PdfContent { get; set; }

    /// <summary>
    /// PDF file size in bytes
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// When the PDF was downloaded
    /// </summary>
    public DateTime DownloadedAt { get; set; }

    /// <summary>
    /// Additional metadata
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();
}
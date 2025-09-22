using Maliev.PurchaseOrderService.Api.DTOs;

namespace Maliev.PurchaseOrderService.Api.Services;

/// <summary>
/// Interface for PDF generation service with event-driven processing
/// </summary>
public interface IPdfGenerationService
{
    /// <summary>
    /// Generates PDF for a purchase order (internal POs only)
    /// </summary>
    /// <param name="purchaseOrderId">Purchase order ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>PDF generation result</returns>
    Task<PdfGenerationResult> GeneratePurchaseOrderPdfAsync(
        int purchaseOrderId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Handles domain event for PDF generation
    /// </summary>
    /// <param name="domainEvent">Domain event (PurchaseOrderCreated/Updated)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if PDF generation was triggered</returns>
    Task<bool> HandlePdfGenerationEventAsync(
        DomainEventDto domainEvent,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets PDF generation status for a purchase order
    /// </summary>
    /// <param name="purchaseOrderId">Purchase order ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>PDF generation status</returns>
    Task<PdfGenerationStatus> GetPdfGenerationStatusAsync(
        int purchaseOrderId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Regenerates PDF for a purchase order
    /// </summary>
    /// <param name="purchaseOrderId">Purchase order ID</param>
    /// <param name="requestedBy">User requesting regeneration</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>PDF generation result</returns>
    Task<PdfGenerationResult> RegeneratePurchaseOrderPdfAsync(
        int purchaseOrderId,
        string requestedBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if PDF generation is applicable for a purchase order
    /// </summary>
    /// <param name="purchaseOrderDto">Purchase order data</param>
    /// <returns>True if PDF should be generated (internal POs only)</returns>
    bool IsPdfGenerationApplicable(PurchaseOrderDto purchaseOrderDto);

    /// <summary>
    /// Gets the download URL for a generated PDF
    /// </summary>
    /// <param name="purchaseOrderId">Purchase order ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>PDF download URL or null if not available</returns>
    Task<string?> GetPdfDownloadUrlAsync(
        int purchaseOrderId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes pending PDF generation requests in batch
    /// </summary>
    /// <param name="batchSize">Maximum number of requests to process</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of PDFs processed</returns>
    Task<int> ProcessPendingPdfGenerationAsync(
        int batchSize = 10,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of PDF generation operation
/// </summary>
public class PdfGenerationResult
{
    /// <summary>
    /// Whether PDF generation was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Generated PDF file information
    /// </summary>
    public PurchaseOrderFileDto? PdfFile { get; set; }

    /// <summary>
    /// Error message if generation failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// PDF file path in storage
    /// </summary>
    public string? FilePath { get; set; }

    /// <summary>
    /// PDF file size in bytes
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// Generation timestamp
    /// </summary>
    public DateTime GeneratedAt { get; set; }

    /// <summary>
    /// Time taken to generate PDF
    /// </summary>
    public TimeSpan GenerationTime { get; set; }

    /// <summary>
    /// PDF generation request ID for tracking
    /// </summary>
    public string? RequestId { get; set; }

    /// <summary>
    /// Whether this was an async generation
    /// </summary>
    public bool IsAsync { get; set; }
}

/// <summary>
/// PDF generation status
/// </summary>
public class PdfGenerationStatus
{
    /// <summary>
    /// Current status of PDF generation
    /// </summary>
    public PdfStatus Status { get; set; }

    /// <summary>
    /// Last generation attempt timestamp
    /// </summary>
    public DateTime? LastAttempt { get; set; }

    /// <summary>
    /// Number of generation attempts
    /// </summary>
    public int AttemptCount { get; set; }

    /// <summary>
    /// Error message from last failed attempt
    /// </summary>
    public string? LastError { get; set; }

    /// <summary>
    /// PDF file information if available
    /// </summary>
    public PurchaseOrderFileDto? PdfFile { get; set; }

    /// <summary>
    /// Next retry timestamp if applicable
    /// </summary>
    public DateTime? NextRetry { get; set; }

    /// <summary>
    /// Whether PDF generation is applicable for this PO
    /// </summary>
    public bool IsApplicable { get; set; }

    /// <summary>
    /// Reason why PDF generation is not applicable (if applicable)
    /// </summary>
    public string? NotApplicableReason { get; set; }
}

/// <summary>
/// PDF generation status enumeration
/// </summary>
public enum PdfStatus
{
    NotApplicable,
    Pending,
    InProgress,
    Completed,
    Failed,
    Retrying
}
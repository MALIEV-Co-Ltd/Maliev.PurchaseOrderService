namespace Maliev.PurchaseOrderService.Common.Enumerations;

/// <summary>
/// Represents the type of audit action performed
/// </summary>
public enum AuditAction
{
    /// <summary>
    /// Entity was created
    /// </summary>
    Create = 0,

    /// <summary>
    /// Entity was modified
    /// </summary>
    Update = 1,

    /// <summary>
    /// Entity was deleted
    /// </summary>
    Delete = 2,

    /// <summary>
    /// Order was approved
    /// </summary>
    Approve = 3,

    /// <summary>
    /// Order was cancelled
    /// </summary>
    Cancel = 4,

    /// <summary>
    /// Data fetched from external service
    /// </summary>
    ExternalFetch = 5,

    /// <summary>
    /// External service validation performed
    /// </summary>
    ExternalValidation = 6,

    /// <summary>
    /// PDF was generated via PdfService
    /// </summary>
    PDFGenerated = 7,

    /// <summary>
    /// Domain event was published
    /// </summary>
    EventPublished = 8
}

using Maliev.PurchaseOrderService.Data.Enums;

namespace Maliev.PurchaseOrderService.Api.DTOs;

/// <summary>
/// Request to update document metadata
/// </summary>
public class UpdateDocumentRequest
{
    /// <summary>
    /// Updated file name
    /// </summary>
    public string? FileName { get; set; }

    /// <summary>
    /// Updated description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Updated document type
    /// </summary>
    public DocumentType? DocumentType { get; set; }

    /// <summary>
    /// Updated category
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Updated tags for categorization
    /// </summary>
    public List<string>? Tags { get; set; }

    /// <summary>
    /// Updated access level
    /// </summary>
    public string? AccessLevel { get; set; }

    /// <summary>
    /// Updated metadata as key-value pairs
    /// </summary>
    public Dictionary<string, string>? Metadata { get; set; }

    /// <summary>
    /// Whether to archive the document
    /// </summary>
    public bool? IsArchived { get; set; }

    /// <summary>
    /// Expiration date for the document
    /// </summary>
    public DateTime? ExpiresAt { get; set; }
}
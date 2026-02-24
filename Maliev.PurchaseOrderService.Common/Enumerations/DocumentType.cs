namespace Maliev.PurchaseOrderService.Common.Enumerations;

/// <summary>
/// Represents the type of document attached to a purchase order
/// </summary>
public enum DocumentType
{
    /// <summary>
    /// Customer purchase order document
    /// </summary>
    CustomerPO = 0,

    /// <summary>
    /// Internal approval documentation
    /// </summary>
    InternalApproval = 1,

    /// <summary>
    /// Supplier invoice or proforma invoice
    /// </summary>
    Invoice = 2,

    /// <summary>
    /// Reference documents (specs, drawings, etc.)
    /// </summary>
    Reference = 3,

    /// <summary>
    /// Automatically generated PDF via PdfService
    /// </summary>
    GeneratedPDF = 4,

    /// <summary>
    /// Other supporting documents
    /// </summary>
    Other = 5
}

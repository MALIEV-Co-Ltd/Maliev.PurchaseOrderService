namespace Maliev.PurchaseOrderService.Data.Enums;

/// <summary>
/// Type of document attached to purchase orders
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
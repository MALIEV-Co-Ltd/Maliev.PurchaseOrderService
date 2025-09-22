namespace Maliev.PurchaseOrderService.Api.DTOs;

/// <summary>
/// Request for PDF generation
/// </summary>
public class PdfGenerationRequest
{
    /// <summary>
    /// Purchase order data
    /// </summary>
    public PurchaseOrderDto PurchaseOrder { get; set; } = new();

    /// <summary>
    /// Supplier data for PDF generation
    /// </summary>
    public SupplierDto? SupplierData { get; set; }

    /// <summary>
    /// Order data for PDF generation
    /// </summary>
    public OrderDto? OrderData { get; set; }

    /// <summary>
    /// PDF template to use
    /// </summary>
    public string Template { get; set; } = "standard-purchase-order";

    /// <summary>
    /// PDF generation options
    /// </summary>
    public PdfGenerationOptions Options { get; set; } = new();

    /// <summary>
    /// Request ID for tracking
    /// </summary>
    public string? RequestId { get; set; }

    /// <summary>
    /// Requested by user
    /// </summary>
    public string? RequestedBy { get; set; }

    /// <summary>
    /// Additional metadata for PDF generation
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// PDF generation options
/// </summary>
public class PdfGenerationOptions
{
    /// <summary>
    /// Whether to include item details
    /// </summary>
    public bool IncludeItemDetails { get; set; } = true;

    /// <summary>
    /// Whether to include WHT calculation details
    /// </summary>
    public bool IncludeWHTCalculation { get; set; } = true;

    /// <summary>
    /// Whether to include address information
    /// </summary>
    public bool IncludeAddresses { get; set; } = true;

    /// <summary>
    /// Language for PDF content
    /// </summary>
    public string Language { get; set; } = "en-US";

    /// <summary>
    /// Page size (A4, Letter, etc.)
    /// </summary>
    public string PageSize { get; set; } = "A4";

    /// <summary>
    /// Page orientation (Portrait, Landscape)
    /// </summary>
    public string Orientation { get; set; } = "Portrait";

    /// <summary>
    /// Whether to include watermark
    /// </summary>
    public bool IncludeWatermark { get; set; } = false;

    /// <summary>
    /// Watermark text
    /// </summary>
    public string? WatermarkText { get; set; }

    /// <summary>
    /// Custom header text
    /// </summary>
    public string? HeaderText { get; set; }

    /// <summary>
    /// Custom footer text
    /// </summary>
    public string? FooterText { get; set; }
}
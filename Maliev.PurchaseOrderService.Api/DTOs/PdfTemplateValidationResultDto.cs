namespace Maliev.PurchaseOrderService.Api.DTOs;

/// <summary>
/// Result of PDF template data validation
/// </summary>
public class PdfTemplateValidationResultDto
{
    /// <summary>
    /// Whether the template data is valid
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// List of validation errors
    /// </summary>
    public List<string> ValidationErrors { get; set; } = new();

    /// <summary>
    /// List of validation warnings
    /// </summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>
    /// Required fields for the template
    /// </summary>
    public List<string> RequiredFields { get; set; } = new();

    /// <summary>
    /// Fields provided in the data
    /// </summary>
    public List<string> ProvidedFields { get; set; } = new();

    /// <summary>
    /// Missing required fields
    /// </summary>
    public List<string> MissingFields { get; set; } = new();

    /// <summary>
    /// When the validation was performed
    /// </summary>
    public DateTime ValidatedAt { get; set; }
}
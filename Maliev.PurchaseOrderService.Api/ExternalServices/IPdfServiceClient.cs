using Maliev.PurchaseOrderService.Api.DTOs;

namespace Maliev.PurchaseOrderService.Api.ExternalServices;

/// <summary>
/// Interface for PDF Service external API client
/// </summary>
public interface IPdfServiceClient
{
    /// <summary>
    /// Generates PDF document from HTML content
    /// </summary>
    /// <param name="htmlContent">HTML content to convert</param>
    /// <param name="options">PDF generation options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>PDF generation result</returns>
    Task<PdfGenerationResultDto?> GeneratePdfFromHtmlAsync(
        string htmlContent,
        PdfGenerationOptionsDto options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates PDF document from template and data
    /// </summary>
    /// <param name="templateId">Template ID</param>
    /// <param name="templateData">Data to merge with template</param>
    /// <param name="options">PDF generation options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>PDF generation result</returns>
    Task<PdfGenerationResultDto?> GeneratePdfFromTemplateAsync(
        string templateId,
        Dictionary<string, object> templateData,
        PdfGenerationOptionsDto options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Merges multiple PDF documents into one
    /// </summary>
    /// <param name="pdfFiles">List of PDF file streams to merge</param>
    /// <param name="options">Merge options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Merged PDF result</returns>
    Task<PdfMergeResultDto?> MergePdfsAsync(
        IEnumerable<PdfFileInput> pdfFiles,
        PdfMergeOptionsDto options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Converts PDF to images
    /// </summary>
    /// <param name="pdfStream">PDF file stream</param>
    /// <param name="options">Conversion options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Image conversion result</returns>
    Task<PdfToImageResultDto?> ConvertPdfToImagesAsync(
        Stream pdfStream,
        PdfToImageOptionsDto options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Extracts text from PDF document
    /// </summary>
    /// <param name="pdfStream">PDF file stream</param>
    /// <param name="options">Text extraction options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Text extraction result</returns>
    Task<PdfTextExtractionResultDto?> ExtractTextFromPdfAsync(
        Stream pdfStream,
        PdfTextExtractionOptionsDto options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds watermark to PDF document
    /// </summary>
    /// <param name="pdfStream">PDF file stream</param>
    /// <param name="watermarkOptions">Watermark configuration</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Watermarked PDF result</returns>
    Task<PdfWatermarkResultDto?> AddWatermarkToPdfAsync(
        Stream pdfStream,
        PdfWatermarkOptionsDto watermarkOptions,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets PDF document information (metadata, page count, etc.)
    /// </summary>
    /// <param name="pdfStream">PDF file stream</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>PDF information</returns>
    Task<PdfInfoDto?> GetPdfInfoAsync(Stream pdfStream, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates PDF document structure and content
    /// </summary>
    /// <param name="pdfStream">PDF file stream</param>
    /// <param name="validationOptions">Validation options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>PDF validation result</returns>
    Task<PdfValidationResultDto> ValidatePdfAsync(
        Stream pdfStream,
        PdfValidationOptionsDto validationOptions,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets available PDF templates
    /// </summary>
    /// <param name="category">Template category filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of available templates</returns>
    Task<IEnumerable<PdfTemplateDto>> GetAvailableTemplatesAsync(
        string? category = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads PDF template by ID
    /// </summary>
    /// <param name="templateId">Template ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Template download result</returns>
    Task<PdfTemplateDownloadDto?> DownloadTemplateAsync(string templateId, CancellationToken cancellationToken = default);
}
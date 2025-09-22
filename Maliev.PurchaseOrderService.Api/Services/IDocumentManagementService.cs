using Maliev.PurchaseOrderService.Api.DTOs;

namespace Maliev.PurchaseOrderService.Api.Services;

/// <summary>
/// Interface for managing purchase order documents and file operations
/// </summary>
public interface IDocumentManagementService
{
    /// <summary>
    /// Uploads a document for a purchase order
    /// </summary>
    /// <param name="purchaseOrderId">Purchase order ID</param>
    /// <param name="file">File stream</param>
    /// <param name="fileName">Original file name</param>
    /// <param name="contentType">MIME content type</param>
    /// <param name="uploadedBy">User who uploaded the file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Upload result with file information</returns>
    Task<DocumentUploadResult> UploadDocumentAsync(
        int purchaseOrderId,
        Stream file,
        string fileName,
        string contentType,
        string uploadedBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all documents for a purchase order
    /// </summary>
    /// <param name="purchaseOrderId">Purchase order ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of purchase order files</returns>
    Task<IEnumerable<PurchaseOrderFileDto>> GetDocumentsAsync(
        int purchaseOrderId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads a document by file ID
    /// </summary>
    /// <param name="fileId">File ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Document download result</returns>
    Task<DocumentDownloadResult> DownloadDocumentAsync(
        int fileId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a document
    /// </summary>
    /// <param name="fileId">File ID</param>
    /// <param name="deletedBy">User who deleted the file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if successfully deleted</returns>
    Task<bool> DeleteDocumentAsync(
        int fileId,
        string deletedBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates document metadata
    /// </summary>
    /// <param name="fileId">File ID</param>
    /// <param name="updateRequest">Update request</param>
    /// <param name="updatedBy">User who updated the file</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated file information</returns>
    Task<PurchaseOrderFileDto?> UpdateDocumentAsync(
        int fileId,
        UpdateDocumentRequest updateRequest,
        string updatedBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates file before upload
    /// </summary>
    /// <param name="fileName">File name</param>
    /// <param name="contentType">Content type</param>
    /// <param name="fileSize">File size in bytes</param>
    /// <returns>Validation result</returns>
    DocumentValidationResult ValidateFile(string fileName, string contentType, long fileSize);

    /// <summary>
    /// Gets file metadata without downloading content
    /// </summary>
    /// <param name="fileId">File ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>File metadata or null if not found</returns>
    Task<PurchaseOrderFileDto?> GetDocumentMetadataAsync(
        int fileId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates file preview URL for supported file types
    /// </summary>
    /// <param name="fileId">File ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Preview URL or null if preview not supported</returns>
    Task<string?> GeneratePreviewUrlAsync(
        int fileId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Archives old documents based on retention policy
    /// </summary>
    /// <param name="retentionDays">Number of days to retain documents</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of documents archived</returns>
    Task<int> ArchiveOldDocumentsAsync(
        int retentionDays = 2555, // 7 years default
        CancellationToken cancellationToken = default);
}
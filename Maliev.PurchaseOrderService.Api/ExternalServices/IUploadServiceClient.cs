using Maliev.PurchaseOrderService.Api.DTOs;

namespace Maliev.PurchaseOrderService.Api.ExternalServices;

/// <summary>
/// Interface for Upload Service external API client
/// </summary>
public interface IUploadServiceClient
{
    /// <summary>
    /// Uploads a file to the upload service
    /// </summary>
    /// <param name="file">File data to upload</param>
    /// <param name="fileName">Original filename</param>
    /// <param name="contentType">File content type</param>
    /// <param name="category">File category (e.g., "purchase-orders")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Upload result with file information</returns>
    Task<FileUploadResultDto?> UploadFileAsync(
        Stream file,
        string fileName,
        string contentType,
        string category,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Uploads multiple files in a single request
    /// </summary>
    /// <param name="files">List of files to upload</param>
    /// <param name="category">File category</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of upload results</returns>
    Task<IEnumerable<FileUploadResultDto>> UploadMultipleFilesAsync(
        IEnumerable<FileUploadRequest> files,
        string category,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets file information by file ID
    /// </summary>
    /// <param name="fileId">File ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>File information or null if not found</returns>
    Task<FileInfoDto?> GetFileInfoAsync(string fileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads file content by file ID
    /// </summary>
    /// <param name="fileId">File ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>File download result with stream</returns>
    Task<FileDownloadResultDto?> DownloadFileAsync(string fileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a file by file ID
    /// </summary>
    /// <param name="fileId">File ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deletion was successful</returns>
    Task<bool> DeleteFileAsync(string fileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets download URL for a file (for temporary access)
    /// </summary>
    /// <param name="fileId">File ID</param>
    /// <param name="expirationMinutes">URL expiration in minutes (default: 60)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Download URL information</returns>
    Task<FileDownloadUrlDto?> GetDownloadUrlAsync(
        string fileId,
        int expirationMinutes = 60,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates file type and size before upload
    /// </summary>
    /// <param name="fileName">File name</param>
    /// <param name="fileSize">File size in bytes</param>
    /// <param name="contentType">File content type</param>
    /// <param name="category">File category</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>File validation result</returns>
    Task<FileValidationResultDto> ValidateFileAsync(
        string fileName,
        long fileSize,
        string contentType,
        string category,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets files by category and optional metadata filters
    /// </summary>
    /// <param name="category">File category</param>
    /// <param name="metadata">Optional metadata filters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of file information matching criteria</returns>
    Task<IEnumerable<FileInfoDto>> GetFilesByCategoryAsync(
        string category,
        Dictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates an upload URL for file upload (alternative method for tests)
    /// </summary>
    /// <param name="fileName">Original filename</param>
    /// <param name="contentType">File content type</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Upload information with URL</returns>
    Task<UploadDto> GenerateUploadUrlAsync(
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a download URL for file download (alternative method for tests)
    /// </summary>
    /// <param name="fileId">File ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Download URL</returns>
    Task<string> GenerateDownloadUrlAsync(
        string fileId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets files by tags (alternative method for tests)
    /// </summary>
    /// <param name="tags">File tags to search for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of files matching the tags</returns>
    Task<IEnumerable<FileInfoDto>> GetFilesByTagsAsync(
        string[] tags,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates file metadata
    /// </summary>
    /// <param name="fileId">File ID</param>
    /// <param name="metadata">Updated metadata</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated file information</returns>
    Task<FileInfoDto?> UpdateFileMetadataAsync(
        string fileId,
        Dictionary<string, string> metadata,
        CancellationToken cancellationToken = default);
}
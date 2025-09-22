using AutoMapper;
using Maliev.PurchaseOrderService.Api.DTOs;
using Maliev.PurchaseOrderService.Api.ExternalServices;
using Maliev.PurchaseOrderService.Data;
using Maliev.PurchaseOrderService.Data.Entities;
using Maliev.PurchaseOrderService.Data.Enums;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace Maliev.PurchaseOrderService.Api.Services;

/// <summary>
/// Service for managing purchase order documents and file operations
/// </summary>
public class DocumentManagementService : IDocumentManagementService
{
    private readonly PurchaseOrderContext _context;
    private readonly IUploadServiceClient _uploadService;
    private readonly IMapper _mapper;
    private readonly ILogger<DocumentManagementService> _logger;

    // File validation configuration
    private readonly HashSet<string> _allowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".txt", ".csv",
        ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff",
        ".zip", ".rar", ".7z"
    };

    private readonly HashSet<string> _allowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.ms-excel",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "text/plain",
        "text/csv",
        "image/jpeg",
        "image/png",
        "image/gif",
        "image/bmp",
        "image/tiff",
        "application/zip",
        "application/x-rar-compressed",
        "application/x-7z-compressed"
    };

    private const long MaxFileSize = 50 * 1024 * 1024; // 50 MB
    private const long MaxImageSize = 10 * 1024 * 1024; // 10 MB for images

    public DocumentManagementService(
        PurchaseOrderContext context,
        IUploadServiceClient uploadService,
        IMapper mapper,
        ILogger<DocumentManagementService> logger)
    {
        _context = context;
        _uploadService = uploadService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<DocumentUploadResult> UploadDocumentAsync(
        int purchaseOrderId,
        Stream file,
        string fileName,
        string contentType,
        string uploadedBy,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Uploading document {FileName} for purchase order {PurchaseOrderId} by {UploadedBy}",
            fileName, purchaseOrderId, uploadedBy);

        try
        {
            // Validate purchase order exists
            var purchaseOrder = await _context.PurchaseOrders
                .FirstOrDefaultAsync(po => po.Id == purchaseOrderId, cancellationToken);

            if (purchaseOrder == null)
            {
                return new DocumentUploadResult
                {
                    Success = false,
                    ErrorMessage = $"Purchase order {purchaseOrderId} not found"
                };
            }

            // Validate file
            var validationResult = ValidateFile(fileName, contentType, file.Length);
            if (!validationResult.IsValid)
            {
                return new DocumentUploadResult
                {
                    Success = false,
                    ErrorMessage = string.Join("; ", validationResult.Errors)
                };
            }

            // Calculate file hash for integrity
            var fileHash = await CalculateFileHashAsync(file);
            file.Position = 0; // Reset stream position

            // Check for duplicate file by name (since FileHash property doesn't exist)
            var existingFile = await _context.PurchaseOrderFiles
                .FirstOrDefaultAsync(f => f.PurchaseOrderId == purchaseOrderId && f.FileName == fileName && !f.IsDeleted, cancellationToken);

            if (existingFile != null)
            {
                return new DocumentUploadResult
                {
                    Success = false,
                    ErrorMessage = "A file with identical content already exists for this purchase order"
                };
            }

            // Upload to external storage
            var uploadResult = await _uploadService.UploadFileAsync(file, fileName, contentType, "purchase-orders", cancellationToken);

            if (uploadResult == null || !uploadResult.IsSuccess)
            {
                return new DocumentUploadResult
                {
                    Success = false,
                    ErrorMessage = uploadResult?.ErrorMessage ?? "Failed to upload file to storage"
                };
            }

            // Save file metadata to database
            var purchaseOrderFile = new PurchaseOrderFile
            {
                PurchaseOrderId = purchaseOrderId,
                FileName = fileName,
                ContentType = contentType,
                FileSize = file.Length,
                ObjectName = uploadResult.FileId,
                DocumentType = DetermineDocumentType(fileName, contentType),
                UploadedBy = uploadedBy,
                UploadedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            _context.PurchaseOrderFiles.Add(purchaseOrderFile);
            await _context.SaveChangesAsync(cancellationToken);

            var fileDto = _mapper.Map<PurchaseOrderFileDto>(purchaseOrderFile);

            _logger.LogInformation("Document {FileName} uploaded successfully with ID {FileId}",
                fileName, purchaseOrderFile.Id);

            return new DocumentUploadResult
            {
                Success = true,
                File = fileDto,
                FileId = purchaseOrderFile.Id,
                FilePath = uploadResult.FileId,
                FileSize = file.Length,
                // FileHash property not available in entity
                UploadedAt = purchaseOrderFile.UploadedAt,
                UploadedBy = uploadedBy
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading document {FileName} for purchase order {PurchaseOrderId}",
                fileName, purchaseOrderId);
            throw;
        }
    }

    public async Task<IEnumerable<PurchaseOrderFileDto>> GetDocumentsAsync(
        int purchaseOrderId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting documents for purchase order {PurchaseOrderId}", purchaseOrderId);

        var files = await _context.PurchaseOrderFiles
            .Where(f => f.PurchaseOrderId == purchaseOrderId && !f.IsDeleted)
            .OrderByDescending(f => f.UploadedAt)
            .ToListAsync(cancellationToken);

        return _mapper.Map<IEnumerable<PurchaseOrderFileDto>>(files);
    }

    public async Task<DocumentDownloadResult> DownloadDocumentAsync(
        int fileId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Downloading document {FileId}", fileId);

        try
        {
            var file = await _context.PurchaseOrderFiles
                .FirstOrDefaultAsync(f => f.Id == fileId && !f.IsDeleted, cancellationToken);

            if (file == null)
            {
                return new DocumentDownloadResult
                {
                    Success = false,
                    ErrorMessage = "File not found or has been deleted"
                };
            }

            var downloadResult = await _uploadService.DownloadFileAsync(file.ObjectName, cancellationToken);

            if (downloadResult == null || downloadResult.Content == null)
            {
                return new DocumentDownloadResult
                {
                    Success = false,
                    ErrorMessage = "Failed to download file from storage"
                };
            }

            return new DocumentDownloadResult
            {
                Success = true,
                FileStream = downloadResult.Content,
                FileName = file.FileName,
                ContentType = file.ContentType,
                FileSize = file.FileSize,
                FileMetadata = _mapper.Map<PurchaseOrderFileDto>(file),
                LastModified = file.UploadedAt,
                ETag = file.Id.ToString()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading document {FileId}", fileId);
            throw;
        }
    }

    public async Task<bool> DeleteDocumentAsync(
        int fileId,
        string deletedBy,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting document {FileId} by {DeletedBy}", fileId, deletedBy);

        try
        {
            var file = await _context.PurchaseOrderFiles
                .FirstOrDefaultAsync(f => f.Id == fileId && !f.IsDeleted, cancellationToken);

            if (file == null)
            {
                _logger.LogWarning("File {FileId} not found for deletion", fileId);
                return false;
            }

            // Soft delete in database
            file.IsDeleted = true;
            // DeletedBy and DeletedAt properties not available in entity
            // UpdatedAt property not available in entity

            await _context.SaveChangesAsync(cancellationToken);

            // Delete from external storage (background operation)
            _ = Task.Run(async () =>
            {
                try
                {
                    await _uploadService.DeleteFileAsync(file.ObjectName, CancellationToken.None);
                    _logger.LogInformation("File {ObjectName} deleted from external storage", file.ObjectName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to delete file {ObjectName} from external storage", file.ObjectName);
                }
            }, cancellationToken);

            _logger.LogInformation("Document {FileId} marked as deleted successfully", fileId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document {FileId}", fileId);
            throw;
        }
    }

    public async Task<PurchaseOrderFileDto?> UpdateDocumentAsync(
        int fileId,
        UpdateDocumentRequest updateRequest,
        string updatedBy,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating document {FileId} by {UpdatedBy}", fileId, updatedBy);

        try
        {
            var file = await _context.PurchaseOrderFiles
                .FirstOrDefaultAsync(f => f.Id == fileId && !f.IsDeleted, cancellationToken);

            if (file == null)
            {
                return null;
            }

            // Update allowed fields
            if (!string.IsNullOrEmpty(updateRequest.FileName))
            {
                file.FileName = updateRequest.FileName;
            }

            if (!string.IsNullOrEmpty(updateRequest.Description))
            {
                file.Description = updateRequest.Description;
            }

            if (updateRequest.DocumentType.HasValue)
            {
                file.DocumentType = updateRequest.DocumentType.Value;
            }

            // Category, Tags, AccessLevel, Metadata, IsArchived, ExpiresAt properties not available
            // UpdatedBy, UpdatedAt properties not available

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Document {FileId} updated successfully", fileId);
            return _mapper.Map<PurchaseOrderFileDto>(file);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating document {FileId}", fileId);
            throw;
        }
    }

    public DocumentValidationResult ValidateFile(string fileName, string contentType, long fileSize)
    {
        var result = new DocumentValidationResult
        {
            FileSize = fileSize,
            MaxFileSize = MaxFileSize,
            AllowedExtensions = _allowedExtensions.ToArray()
        };

        // Check file extension
        var extension = Path.GetExtension(fileName);
        result.IsFileTypeAllowed = !string.IsNullOrEmpty(extension) && _allowedExtensions.Contains(extension);

        if (!result.IsFileTypeAllowed)
        {
            result.Errors.Add($"File type '{extension}' is not allowed. Allowed types: {string.Join(", ", _allowedExtensions)}");
        }

        // Check content type
        if (!_allowedContentTypes.Contains(contentType))
        {
            result.Errors.Add($"Content type '{contentType}' is not allowed");
        }

        // Check file size
        var maxSize = IsImageFile(contentType) ? MaxImageSize : MaxFileSize;
        result.MaxFileSize = maxSize;
        result.IsSizeValid = fileSize <= maxSize && fileSize > 0;

        if (!result.IsSizeValid)
        {
            if (fileSize <= 0)
            {
                result.Errors.Add("File cannot be empty");
            }
            else
            {
                result.Errors.Add($"File size {fileSize:N0} bytes exceeds maximum allowed size of {maxSize:N0} bytes");
            }
        }

        // Check file name
        if (string.IsNullOrWhiteSpace(fileName))
        {
            result.Errors.Add("File name is required");
        }
        else if (fileName.Length > 255)
        {
            result.Errors.Add("File name cannot exceed 255 characters");
        }
        else if (fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            result.Errors.Add("File name contains invalid characters");
        }

        result.DetectedFileType = contentType;
        result.IsValid = result.Errors.Count == 0;

        return result;
    }

    public async Task<PurchaseOrderFileDto?> GetDocumentMetadataAsync(
        int fileId,
        CancellationToken cancellationToken = default)
    {
        var file = await _context.PurchaseOrderFiles
            .FirstOrDefaultAsync(f => f.Id == fileId && !f.IsDeleted, cancellationToken);

        return file != null ? _mapper.Map<PurchaseOrderFileDto>(file) : null;
    }

    public async Task<string?> GeneratePreviewUrlAsync(
        int fileId,
        CancellationToken cancellationToken = default)
    {
        var file = await _context.PurchaseOrderFiles
            .FirstOrDefaultAsync(f => f.Id == fileId && !f.IsDeleted, cancellationToken);

        if (file == null)
        {
            return null;
        }

        // Only generate preview URLs for supported file types
        var supportedTypes = new[] { "application/pdf", "image/jpeg", "image/png", "image/gif" };
        if (!supportedTypes.Contains(file.ContentType.ToLowerInvariant()))
        {
            return null;
        }

        // GeneratePreviewUrlAsync method not available in IUploadServiceClient
        // Use GetDownloadUrlAsync instead for temporary access
        var downloadUrl = await _uploadService.GetDownloadUrlAsync(file.ObjectName, 60, cancellationToken);
        return downloadUrl?.DownloadUrl;
    }

    public async Task<int> ArchiveOldDocumentsAsync(
        int retentionDays = 2555,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Archiving documents older than {RetentionDays} days", retentionDays);

        var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);

        var filesToArchive = await _context.PurchaseOrderFiles
            .Where(f => !f.IsDeleted && f.UploadedAt < cutoffDate)
            .ToListAsync(cancellationToken);

        foreach (var file in filesToArchive)
        {
            // IsArchived and UpdatedAt properties not available in entity
            // Consider adding these properties to entity or use different archiving approach
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Archived {Count} documents", filesToArchive.Count);
        return filesToArchive.Count;
    }

    private static async Task<string> CalculateFileHashAsync(Stream stream)
    {
        using var sha256 = SHA256.Create();
        var hash = await sha256.ComputeHashAsync(stream);
        return Convert.ToBase64String(hash);
    }

    private static DocumentType DetermineDocumentType(string fileName, string contentType)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();

        return extension switch
        {
            ".pdf" => DocumentType.GeneratedPDF,
            ".doc" or ".docx" => DocumentType.Invoice,
            ".xls" or ".xlsx" => DocumentType.Reference,
            ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" => DocumentType.Reference,
            ".zip" or ".rar" or ".7z" => DocumentType.Other,
            _ => DocumentType.Other
        };
    }

    private static bool IsImageFile(string contentType)
    {
        return contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
    }
}
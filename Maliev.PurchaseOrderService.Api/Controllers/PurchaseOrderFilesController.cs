using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Maliev.PurchaseOrderService.Api.DTOs;
using Maliev.PurchaseOrderService.Api.Services;
using Maliev.PurchaseOrderService.Data;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace Maliev.PurchaseOrderService.Api.Controllers;

/// <summary>
/// Purchase Order Files API Controller for document operations
/// </summary>
[ApiController]
[Route("purchase-orders/{purchaseOrderId:int}/files")]
[Authorize]
[Produces("application/json")]
public class PurchaseOrderFilesController : ControllerBase
{
    private readonly IDocumentManagementService _documentService;
    private readonly IPdfGenerationService _pdfService;
    private readonly PurchaseOrderContext _context;
    private readonly ILogger<PurchaseOrderFilesController> _logger;

    public PurchaseOrderFilesController(
        IDocumentManagementService documentService,
        IPdfGenerationService pdfService,
        PurchaseOrderContext context,
        ILogger<PurchaseOrderFilesController> logger)
    {
        _documentService = documentService;
        _pdfService = pdfService;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Gets all files for a purchase order
    /// </summary>
    /// <param name="purchaseOrderId">Purchase order ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of purchase order files</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<PurchaseOrderFileDto>), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.NotFound)]
    public async Task<ActionResult<IEnumerable<PurchaseOrderFileDto>>> GetFiles(
        int purchaseOrderId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting files for purchase order {PurchaseOrderId}", purchaseOrderId);

            // Verify purchase order exists
            var purchaseOrderExists = await _context.PurchaseOrders
                .AnyAsync(po => po.Id == purchaseOrderId && !po.IsDeleted, cancellationToken);

            if (!purchaseOrderExists)
            {
                return NotFound(new ErrorResponse
                {
                    Error = new ErrorInfo
                    {
                        Message = $"Purchase order with ID {purchaseOrderId} not found",
                        Code = "PURCHASE_ORDER_NOT_FOUND"
                    }
                });
            }

            var files = await _documentService.GetDocumentsAsync(purchaseOrderId, cancellationToken);
            return Ok(files);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting files for purchase order {PurchaseOrderId}", purchaseOrderId);
            return StatusCode(500, new ErrorResponse
            {
                Error = new ErrorInfo
                {
                    Message = "An error occurred while retrieving files",
                    Code = "INTERNAL_ERROR"
                }
            });
        }
    }

    /// <summary>
    /// Gets a specific file by ID
    /// </summary>
    /// <param name="purchaseOrderId">Purchase order ID</param>
    /// <param name="fileId">File ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>File metadata</returns>
    [HttpGet("{fileId:int}")]
    [ProducesResponseType(typeof(PurchaseOrderFileDto), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.NotFound)]
    public async Task<ActionResult<PurchaseOrderFileDto>> GetFile(
        int purchaseOrderId,
        int fileId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting file {FileId} for purchase order {PurchaseOrderId}", fileId, purchaseOrderId);

            var file = await _documentService.GetDocumentMetadataAsync(fileId, cancellationToken);

            if (file == null || file.PurchaseOrderId != purchaseOrderId)
            {
                return NotFound(new ErrorResponse
                {
                    Error = new ErrorInfo
                    {
                        Message = $"File with ID {fileId} not found for purchase order {purchaseOrderId}",
                        Code = "FILE_NOT_FOUND"
                    }
                });
            }

            return Ok(file);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting file {FileId} for purchase order {PurchaseOrderId}", fileId, purchaseOrderId);
            return StatusCode(500, new ErrorResponse
            {
                Error = new ErrorInfo
                {
                    Message = "An error occurred while retrieving the file",
                    Code = "INTERNAL_ERROR"
                }
            });
        }
    }

    /// <summary>
    /// Uploads a file to a purchase order
    /// </summary>
    /// <param name="purchaseOrderId">Purchase order ID</param>
    /// <param name="file">File to upload</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Upload result with file information</returns>
    [HttpPost]
    [ProducesResponseType(typeof(DocumentUploadResult), (int)HttpStatusCode.Created)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.NotFound)]
    [RequestSizeLimit(52428800)] // 50MB limit
    [RequestFormLimits(MultipartBodyLengthLimit = 52428800)]
    public async Task<ActionResult<DocumentUploadResult>> UploadFile(
        int purchaseOrderId,
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Uploading file {FileName} for purchase order {PurchaseOrderId}", file?.FileName, purchaseOrderId);

            if (file == null || file.Length == 0)
            {
                return BadRequest(new ErrorResponse
                {
                    Error = new ErrorInfo
                    {
                        Message = "No file provided or file is empty",
                        Code = "NO_FILE_PROVIDED"
                    }
                });
            }

            // Verify purchase order exists
            var purchaseOrderExists = await _context.PurchaseOrders
                .AnyAsync(po => po.Id == purchaseOrderId && !po.IsDeleted, cancellationToken);

            if (!purchaseOrderExists)
            {
                return NotFound(new ErrorResponse
                {
                    Error = new ErrorInfo
                    {
                        Message = $"Purchase order with ID {purchaseOrderId} not found",
                        Code = "PURCHASE_ORDER_NOT_FOUND"
                    }
                });
            }

            // Validate file
            var validationResult = _documentService.ValidateFile(file.FileName, file.ContentType, file.Length);
            if (!validationResult.IsValid)
            {
                return BadRequest(new ErrorResponse
                {
                    Error = new ErrorInfo
                    {
                        Message = "File validation failed",
                        Code = "FILE_VALIDATION_FAILED",
                        Details = validationResult.Errors.Select(e => new ErrorDetail
                        {
                            Field = "file",
                            Message = e
                        }).ToList()
                    }
                });
            }

            var uploadedBy = User.Identity?.Name ?? "unknown";

            using var fileStream = file.OpenReadStream();
            var uploadResult = await _documentService.UploadDocumentAsync(
                purchaseOrderId,
                fileStream,
                file.FileName,
                file.ContentType,
                uploadedBy,
                cancellationToken);

            if (uploadResult.Success)
            {
                return CreatedAtAction(
                    nameof(GetFile),
                    new { purchaseOrderId, fileId = uploadResult.FileId },
                    uploadResult);
            }
            else
            {
                return BadRequest(new ErrorResponse
                {
                    Error = new ErrorInfo
                    {
                        Message = uploadResult.ErrorMessage ?? "File upload failed",
                        Code = "UPLOAD_FAILED"
                    }
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file for purchase order {PurchaseOrderId}", purchaseOrderId);
            return StatusCode(500, new ErrorResponse
            {
                Error = new ErrorInfo
                {
                    Message = "An error occurred while uploading the file",
                    Code = "INTERNAL_ERROR"
                }
            });
        }
    }

    /// <summary>
    /// Downloads a file
    /// </summary>
    /// <param name="purchaseOrderId">Purchase order ID</param>
    /// <param name="fileId">File ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>File content</returns>
    [HttpGet("{fileId:int}/download")]
    [ProducesResponseType(typeof(FileResult), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.NotFound)]
    public async Task<ActionResult> DownloadFile(
        int purchaseOrderId,
        int fileId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Downloading file {FileId} for purchase order {PurchaseOrderId}", fileId, purchaseOrderId);

            // Verify file belongs to the purchase order
            var fileMetadata = await _documentService.GetDocumentMetadataAsync(fileId, cancellationToken);
            if (fileMetadata == null || fileMetadata.PurchaseOrderId != purchaseOrderId)
            {
                return NotFound(new ErrorResponse
                {
                    Error = new ErrorInfo
                    {
                        Message = $"File with ID {fileId} not found for purchase order {purchaseOrderId}",
                        Code = "FILE_NOT_FOUND"
                    }
                });
            }

            var downloadResult = await _documentService.DownloadDocumentAsync(fileId, cancellationToken);

            if (!downloadResult.Success)
            {
                return NotFound(new ErrorResponse
                {
                    Error = new ErrorInfo
                    {
                        Message = downloadResult.ErrorMessage ?? "File not found",
                        Code = "DOWNLOAD_FAILED"
                    }
                });
            }

            return File(
                downloadResult.FileStream!,
                downloadResult.ContentType,
                downloadResult.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading file {FileId} for purchase order {PurchaseOrderId}", fileId, purchaseOrderId);
            return StatusCode(500, new ErrorResponse
            {
                Error = new ErrorInfo
                {
                    Message = "An error occurred while downloading the file",
                    Code = "INTERNAL_ERROR"
                }
            });
        }
    }

    /// <summary>
    /// Deletes a file
    /// </summary>
    /// <param name="purchaseOrderId">Purchase order ID</param>
    /// <param name="fileId">File ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content if successful</returns>
    [HttpDelete("{fileId:int}")]
    [ProducesResponseType((int)HttpStatusCode.NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.NotFound)]
    public async Task<ActionResult> DeleteFile(
        int purchaseOrderId,
        int fileId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Deleting file {FileId} for purchase order {PurchaseOrderId}", fileId, purchaseOrderId);

            // Verify file belongs to the purchase order
            var fileMetadata = await _documentService.GetDocumentMetadataAsync(fileId, cancellationToken);
            if (fileMetadata == null || fileMetadata.PurchaseOrderId != purchaseOrderId)
            {
                return NotFound(new ErrorResponse
                {
                    Error = new ErrorInfo
                    {
                        Message = $"File with ID {fileId} not found for purchase order {purchaseOrderId}",
                        Code = "FILE_NOT_FOUND"
                    }
                });
            }

            var deletedBy = User.Identity?.Name ?? "unknown";
            var deleted = await _documentService.DeleteDocumentAsync(fileId, deletedBy, cancellationToken);

            if (!deleted)
            {
                return NotFound(new ErrorResponse
                {
                    Error = new ErrorInfo
                    {
                        Message = $"File with ID {fileId} not found",
                        Code = "FILE_NOT_FOUND"
                    }
                });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file {FileId} for purchase order {PurchaseOrderId}", fileId, purchaseOrderId);
            return StatusCode(500, new ErrorResponse
            {
                Error = new ErrorInfo
                {
                    Message = "An error occurred while deleting the file",
                    Code = "INTERNAL_ERROR"
                }
            });
        }
    }

    /// <summary>
    /// Updates file metadata
    /// </summary>
    /// <param name="purchaseOrderId">Purchase order ID</param>
    /// <param name="fileId">File ID</param>
    /// <param name="request">Update request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated file metadata</returns>
    [HttpPut("{fileId:int}")]
    [ProducesResponseType(typeof(PurchaseOrderFileDto), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.BadRequest)]
    public async Task<ActionResult<PurchaseOrderFileDto>> UpdateFile(
        int purchaseOrderId,
        int fileId,
        [FromBody] UpdateDocumentRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Updating file {FileId} for purchase order {PurchaseOrderId}", fileId, purchaseOrderId);

            if (!ModelState.IsValid)
            {
                return BadRequest(new ErrorResponse
                {
                    Error = new ErrorInfo
                    {
                        Message = "Invalid request data",
                        Code = "INVALID_REQUEST"
                    }
                });
            }

            // Verify file belongs to the purchase order
            var fileMetadata = await _documentService.GetDocumentMetadataAsync(fileId, cancellationToken);
            if (fileMetadata == null || fileMetadata.PurchaseOrderId != purchaseOrderId)
            {
                return NotFound(new ErrorResponse
                {
                    Error = new ErrorInfo
                    {
                        Message = $"File with ID {fileId} not found for purchase order {purchaseOrderId}",
                        Code = "FILE_NOT_FOUND"
                    }
                });
            }

            var updatedBy = User.Identity?.Name ?? "unknown";
            var updatedFile = await _documentService.UpdateDocumentAsync(fileId, request, updatedBy, cancellationToken);

            if (updatedFile == null)
            {
                return NotFound(new ErrorResponse
                {
                    Error = new ErrorInfo
                    {
                        Message = $"File with ID {fileId} not found",
                        Code = "FILE_NOT_FOUND"
                    }
                });
            }

            return Ok(updatedFile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating file {FileId} for purchase order {PurchaseOrderId}", fileId, purchaseOrderId);
            return StatusCode(500, new ErrorResponse
            {
                Error = new ErrorInfo
                {
                    Message = "An error occurred while updating the file",
                    Code = "INTERNAL_ERROR"
                }
            });
        }
    }

    /// <summary>
    /// Generates a PDF for the purchase order (internal POs only)
    /// </summary>
    /// <param name="purchaseOrderId">Purchase order ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>PDF generation result</returns>
    [HttpPost("generate-pdf")]
    [ProducesResponseType(typeof(PdfGenerationResult), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.BadRequest)]
    [Authorize(Roles = "Employee,Manager,Procurement,Admin")]
    public async Task<ActionResult<PdfGenerationResult>> GeneratePdf(
        int purchaseOrderId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Generating PDF for purchase order {PurchaseOrderId}", purchaseOrderId);

            // Verify purchase order exists
            var purchaseOrderExists = await _context.PurchaseOrders
                .AnyAsync(po => po.Id == purchaseOrderId && !po.IsDeleted, cancellationToken);

            if (!purchaseOrderExists)
            {
                return NotFound(new ErrorResponse
                {
                    Error = new ErrorInfo
                    {
                        Message = $"Purchase order with ID {purchaseOrderId} not found",
                        Code = "PURCHASE_ORDER_NOT_FOUND"
                    }
                });
            }

            var pdfResult = await _pdfService.GeneratePurchaseOrderPdfAsync(purchaseOrderId, cancellationToken);

            if (!pdfResult.Success)
            {
                return BadRequest(new ErrorResponse
                {
                    Error = new ErrorInfo
                    {
                        Message = pdfResult.ErrorMessage ?? "PDF generation failed",
                        Code = "PDF_GENERATION_FAILED"
                    }
                });
            }

            return Ok(pdfResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating PDF for purchase order {PurchaseOrderId}", purchaseOrderId);
            return StatusCode(500, new ErrorResponse
            {
                Error = new ErrorInfo
                {
                    Message = "An error occurred while generating the PDF",
                    Code = "INTERNAL_ERROR"
                }
            });
        }
    }

    /// <summary>
    /// Gets PDF generation status for a purchase order
    /// </summary>
    /// <param name="purchaseOrderId">Purchase order ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>PDF generation status</returns>
    [HttpGet("pdf-status")]
    [ProducesResponseType(typeof(PdfGenerationStatus), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.NotFound)]
    public async Task<ActionResult<PdfGenerationStatus>> GetPdfStatus(
        int purchaseOrderId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting PDF status for purchase order {PurchaseOrderId}", purchaseOrderId);

            // Verify purchase order exists
            var purchaseOrderExists = await _context.PurchaseOrders
                .AnyAsync(po => po.Id == purchaseOrderId && !po.IsDeleted, cancellationToken);

            if (!purchaseOrderExists)
            {
                return NotFound(new ErrorResponse
                {
                    Error = new ErrorInfo
                    {
                        Message = $"Purchase order with ID {purchaseOrderId} not found",
                        Code = "PURCHASE_ORDER_NOT_FOUND"
                    }
                });
            }

            var status = await _pdfService.GetPdfGenerationStatusAsync(purchaseOrderId, cancellationToken);
            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting PDF status for purchase order {PurchaseOrderId}", purchaseOrderId);
            return StatusCode(500, new ErrorResponse
            {
                Error = new ErrorInfo
                {
                    Message = "An error occurred while retrieving PDF status",
                    Code = "INTERNAL_ERROR"
                }
            });
        }
    }

    /// <summary>
    /// Gets a preview URL for a file (if supported)
    /// </summary>
    /// <param name="purchaseOrderId">Purchase order ID</param>
    /// <param name="fileId">File ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Preview URL</returns>
    [HttpGet("{fileId:int}/preview")]
    [ProducesResponseType(typeof(FilePreviewResponse), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.NotFound)]
    public async Task<ActionResult<FilePreviewResponse>> GetFilePreview(
        int purchaseOrderId,
        int fileId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting preview for file {FileId} in purchase order {PurchaseOrderId}", fileId, purchaseOrderId);

            // Verify file belongs to the purchase order
            var fileMetadata = await _documentService.GetDocumentMetadataAsync(fileId, cancellationToken);
            if (fileMetadata == null || fileMetadata.PurchaseOrderId != purchaseOrderId)
            {
                return NotFound(new ErrorResponse
                {
                    Error = new ErrorInfo
                    {
                        Message = $"File with ID {fileId} not found for purchase order {purchaseOrderId}",
                        Code = "FILE_NOT_FOUND"
                    }
                });
            }

            var previewUrl = await _documentService.GeneratePreviewUrlAsync(fileId, cancellationToken);

            var response = new FilePreviewResponse
            {
                FileId = fileId,
                FileName = fileMetadata.FileName,
                ContentType = fileMetadata.ContentType,
                PreviewUrl = previewUrl,
                IsPreviewSupported = !string.IsNullOrEmpty(previewUrl)
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting preview for file {FileId} in purchase order {PurchaseOrderId}", fileId, purchaseOrderId);
            return StatusCode(500, new ErrorResponse
            {
                Error = new ErrorInfo
                {
                    Message = "An error occurred while retrieving file preview",
                    Code = "INTERNAL_ERROR"
                }
            });
        }
    }
}

/// <summary>
/// File preview response
/// </summary>
public class FilePreviewResponse
{
    /// <summary>
    /// File ID
    /// </summary>
    public int FileId { get; set; }

    /// <summary>
    /// File name
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Content type
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// Preview URL (if supported)
    /// </summary>
    public string? PreviewUrl { get; set; }

    /// <summary>
    /// Whether preview is supported for this file type
    /// </summary>
    public bool IsPreviewSupported { get; set; }
}
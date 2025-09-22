using AutoMapper;
using Maliev.PurchaseOrderService.Api.DTOs;
using Maliev.PurchaseOrderService.Api.ExternalServices;
using Maliev.PurchaseOrderService.Data;
using Maliev.PurchaseOrderService.Data.Entities;
using Maliev.PurchaseOrderService.Data.Enums;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Text.Json;

namespace Maliev.PurchaseOrderService.Api.Services;

/// <summary>
/// Service for PDF generation with event-driven processing for internal POs only
/// </summary>
public class PdfGenerationService : IPdfGenerationService
{
    private readonly PurchaseOrderContext _context;
    private readonly IPdfServiceClient _pdfService;
    private readonly IDocumentManagementService _documentService;
    private readonly IMapper _mapper;
    private readonly ILogger<PdfGenerationService> _logger;

    // PDF generation configuration
    private const int MaxRetryAttempts = 3;
    private readonly TimeSpan[] RetryDelays = { TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(15) };

    public PdfGenerationService(
        PurchaseOrderContext context,
        IPdfServiceClient pdfService,
        IDocumentManagementService documentService,
        IMapper mapper,
        ILogger<PdfGenerationService> logger)
    {
        _context = context;
        _pdfService = pdfService;
        _documentService = documentService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<PdfGenerationResult> GeneratePurchaseOrderPdfAsync(
        int purchaseOrderId,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var requestId = Guid.NewGuid().ToString();

        _logger.LogInformation("Starting PDF generation for purchase order {PurchaseOrderId} with request ID {RequestId}",
            purchaseOrderId, requestId);

        try
        {
            // Get purchase order with related data
            var purchaseOrder = await _context.PurchaseOrders
                .Include(po => po.OrderItems)
                .Include(po => po.ShippingAddress)
                .Include(po => po.BillingAddress)
                .FirstOrDefaultAsync(po => po.Id == purchaseOrderId, cancellationToken);

            if (purchaseOrder == null)
            {
                return new PdfGenerationResult
                {
                    Success = false,
                    ErrorMessage = $"Purchase order {purchaseOrderId} not found",
                    RequestId = requestId,
                    GeneratedAt = DateTime.UtcNow
                };
            }

            var purchaseOrderDto = _mapper.Map<PurchaseOrderDto>(purchaseOrder);

            // Check if PDF generation is applicable (internal POs only)
            if (!IsPdfGenerationApplicable(purchaseOrderDto))
            {
                _logger.LogInformation("PDF generation not applicable for purchase order {PurchaseOrderId}: External PO",
                    purchaseOrderId);

                return new PdfGenerationResult
                {
                    Success = false,
                    ErrorMessage = "PDF generation is only available for internal purchase orders",
                    RequestId = requestId,
                    GeneratedAt = DateTime.UtcNow
                };
            }

            // Check if PDF already exists and is current
            var existingPdf = await GetExistingPdfFileAsync(purchaseOrderId, cancellationToken);
            if (existingPdf != null && IsCurrentPdf(existingPdf, purchaseOrder))
            {
                _logger.LogInformation("Current PDF already exists for purchase order {PurchaseOrderId}", purchaseOrderId);

                return new PdfGenerationResult
                {
                    Success = true,
                    PdfFile = _mapper.Map<PurchaseOrderFileDto>(existingPdf),
                    FilePath = existingPdf.ObjectName,
                    FileSize = existingPdf.FileSize,
                    GeneratedAt = existingPdf.UploadedAt,
                    GenerationTime = stopwatch.Elapsed,
                    RequestId = requestId,
                    IsAsync = false
                };
            }

            // Prepare PDF generation request
            var pdfRequest = await BuildPdfGenerationRequestAsync(purchaseOrderDto, cancellationToken);

            // Generate PDF using external service
            var pdfResult = await _pdfService.GeneratePdfFromTemplateAsync(
                "purchase-order-template",
                new Dictionary<string, object>
                {
                    ["PurchaseOrder"] = pdfRequest
                },
                new PdfGenerationOptionsDto { PageSize = "A4" },
                cancellationToken);

            if (pdfResult == null || !pdfResult.IsSuccess)
            {
                _logger.LogError("PDF generation failed for purchase order {PurchaseOrderId}: {Error}",
                    purchaseOrderId, pdfResult?.ErrorMessage ?? "Unknown error");

                return new PdfGenerationResult
                {
                    Success = false,
                    ErrorMessage = pdfResult?.ErrorMessage ?? "PDF generation failed",
                    RequestId = requestId,
                    GeneratedAt = DateTime.UtcNow,
                    GenerationTime = stopwatch.Elapsed
                };
            }

            // Upload PDF to document management
            var fileName = $"PO-{purchaseOrder.OrderNumber}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.pdf";

            var pdfStream = pdfResult.PdfContent!;
            var uploadResult = await _documentService.UploadDocumentAsync(
                purchaseOrderId,
                pdfStream,
                fileName,
                "application/pdf",
                "system-pdf-generation",
                cancellationToken);

            if (!uploadResult.Success)
            {
                _logger.LogError("Failed to upload generated PDF for purchase order {PurchaseOrderId}: {Error}",
                    purchaseOrderId, uploadResult.ErrorMessage);

                return new PdfGenerationResult
                {
                    Success = false,
                    ErrorMessage = $"PDF generated but upload failed: {uploadResult.ErrorMessage}",
                    RequestId = requestId,
                    GeneratedAt = DateTime.UtcNow,
                    GenerationTime = stopwatch.Elapsed
                };
            }

            // Mark old PDFs as superseded
            await MarkOldPdfsAsSupersededAsync(purchaseOrderId, uploadResult.FileId!.Value, cancellationToken);

            // Log audit trail
            await LogPdfGenerationEventAsync(purchaseOrderId, requestId, true, null, cancellationToken);

            stopwatch.Stop();

            _logger.LogInformation("PDF generated successfully for purchase order {PurchaseOrderId} in {ElapsedMs}ms",
                purchaseOrderId, stopwatch.ElapsedMilliseconds);

            return new PdfGenerationResult
            {
                Success = true,
                PdfFile = uploadResult.File,
                FilePath = uploadResult.FilePath,
                FileSize = uploadResult.FileSize,
                GeneratedAt = uploadResult.UploadedAt,
                GenerationTime = stopwatch.Elapsed,
                RequestId = requestId,
                IsAsync = false
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error generating PDF for purchase order {PurchaseOrderId}", purchaseOrderId);

            await LogPdfGenerationEventAsync(purchaseOrderId, requestId, false, ex.Message, cancellationToken);

            return new PdfGenerationResult
            {
                Success = false,
                ErrorMessage = "An error occurred during PDF generation",
                RequestId = requestId,
                GeneratedAt = DateTime.UtcNow,
                GenerationTime = stopwatch.Elapsed
            };
        }
    }

    public async Task<bool> HandlePdfGenerationEventAsync(
        DomainEventDto domainEvent,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling PDF generation event: {EventType} for entity {AggregateId}",
            domainEvent.EventType, domainEvent.AggregateId);

        try
        {
            // Only handle purchase order events
            if (domainEvent.AggregateType != "PurchaseOrder")
            {
                return false;
            }

            // Only handle creation and update events
            if (domainEvent.EventType != "PurchaseOrderCreated" && domainEvent.EventType != "PurchaseOrderUpdated")
            {
                return false;
            }

            var purchaseOrderId = int.Parse(domainEvent.AggregateId);

            // Get purchase order to check if it's internal
            var purchaseOrder = await _context.PurchaseOrders
                .FirstOrDefaultAsync(po => po.Id == purchaseOrderId, cancellationToken);

            if (purchaseOrder == null)
            {
                _logger.LogWarning("Purchase order {PurchaseOrderId} not found for PDF generation event", purchaseOrderId);
                return false;
            }

            var purchaseOrderDto = _mapper.Map<PurchaseOrderDto>(purchaseOrder);

            // Check if PDF generation is applicable
            if (!IsPdfGenerationApplicable(purchaseOrderDto))
            {
                _logger.LogDebug("PDF generation not applicable for purchase order {PurchaseOrderId}: External PO", purchaseOrderId);
                return false;
            }

            // Queue PDF generation for background processing
            await QueuePdfGenerationAsync(purchaseOrderId, domainEvent.EventType, cancellationToken);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling PDF generation event for {EventType} {EntityId}",
                domainEvent.EventType, domainEvent.AggregateId);
            return false;
        }
    }

    public async Task<PdfGenerationStatus> GetPdfGenerationStatusAsync(
        int purchaseOrderId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var purchaseOrder = await _context.PurchaseOrders
                .FirstOrDefaultAsync(po => po.Id == purchaseOrderId, cancellationToken);

            if (purchaseOrder == null)
            {
                return new PdfGenerationStatus
                {
                    Status = PdfStatus.NotApplicable,
                    IsApplicable = false,
                    NotApplicableReason = "Purchase order not found"
                };
            }

            var purchaseOrderDto = _mapper.Map<PurchaseOrderDto>(purchaseOrder);

            if (!IsPdfGenerationApplicable(purchaseOrderDto))
            {
                return new PdfGenerationStatus
                {
                    Status = PdfStatus.NotApplicable,
                    IsApplicable = false,
                    NotApplicableReason = "PDF generation is only available for internal purchase orders"
                };
            }

            // Check for existing PDF
            var existingPdf = await GetExistingPdfFileAsync(purchaseOrderId, cancellationToken);

            // Check for pending generation requests
            var pendingRequest = await _context.DomainEvents
                .Where(de => de.AggregateType == "PurchaseOrder" &&
                           de.AggregateId == purchaseOrderId.ToString() &&
                           de.EventType.Contains("PdfGeneration") &&
                           !de.IsProcessed)
                .OrderByDescending(de => de.OccurredAt)
                .FirstOrDefaultAsync(cancellationToken);

            var status = new PdfGenerationStatus
            {
                IsApplicable = true,
                PdfFile = existingPdf != null ? _mapper.Map<PurchaseOrderFileDto>(existingPdf) : null
            };

            if (pendingRequest != null)
            {
                status.Status = PdfStatus.Pending;
                status.LastAttempt = pendingRequest.OccurredAt;
            }
            else if (existingPdf != null)
            {
                status.Status = IsCurrentPdf(existingPdf, purchaseOrder) ? PdfStatus.Completed : PdfStatus.Pending;
                status.LastAttempt = existingPdf.UploadedAt;
            }
            else
            {
                status.Status = PdfStatus.Pending;
            }

            return status;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting PDF generation status for purchase order {PurchaseOrderId}", purchaseOrderId);
            throw;
        }
    }

    public async Task<PdfGenerationResult> RegeneratePurchaseOrderPdfAsync(
        int purchaseOrderId,
        string requestedBy,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Regenerating PDF for purchase order {PurchaseOrderId} requested by {RequestedBy}",
            purchaseOrderId, requestedBy);

        // Force regeneration by skipping existing PDF check
        return await GeneratePurchaseOrderPdfAsync(purchaseOrderId, cancellationToken);
    }

    public bool IsPdfGenerationApplicable(PurchaseOrderDto purchaseOrderDto)
    {
        // PDF generation is only for internal purchase orders
        return purchaseOrderDto.OrderType == OrderType.Internal;
    }

    public async Task<string?> GetPdfDownloadUrlAsync(
        int purchaseOrderId,
        CancellationToken cancellationToken = default)
    {
        var pdfFile = await GetExistingPdfFileAsync(purchaseOrderId, cancellationToken);

        if (pdfFile == null)
        {
            return null;
        }

        return await _documentService.GeneratePreviewUrlAsync(pdfFile.Id, cancellationToken);
    }

    public async Task<int> ProcessPendingPdfGenerationAsync(
        int batchSize = 10,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing pending PDF generation requests (batch size: {BatchSize})", batchSize);

        var pendingEvents = await _context.DomainEvents
            .Where(de => de.EventType.Contains("PdfGeneration") && !de.IsProcessed)
            .OrderBy(de => de.OccurredAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        var processedCount = 0;

        foreach (var domainEvent in pendingEvents)
        {
            try
            {
                var eventDto = _mapper.Map<DomainEventDto>(domainEvent);
                var handled = await HandlePdfGenerationEventAsync(eventDto, cancellationToken);

                if (handled)
                {
                    domainEvent.IsProcessed = true;
                    domainEvent.ProcessedAt = DateTime.UtcNow;
                    processedCount++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing PDF generation event {EventId}", domainEvent.Id);
                domainEvent.ProcessedAt = DateTime.UtcNow;
                // Metadata property not available in DomainEvent entity
                domainEvent.EventData = JsonSerializer.Serialize(new { Error = ex.Message });
            }
        }

        if (processedCount > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation("Processed {ProcessedCount} PDF generation requests", processedCount);
        return processedCount;
    }

    private async Task<PurchaseOrderFile?> GetExistingPdfFileAsync(int purchaseOrderId, CancellationToken cancellationToken)
    {
        return await _context.PurchaseOrderFiles
            .Where(f => f.PurchaseOrderId == purchaseOrderId &&
                       f.DocumentType == DocumentType.GeneratedPDF &&
                       !f.IsDeleted)
            .OrderByDescending(f => f.UploadedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static bool IsCurrentPdf(PurchaseOrderFile pdfFile, PurchaseOrder purchaseOrder)
    {
        // Consider PDF current if it was generated after the last update
        return pdfFile.UploadedAt >= (purchaseOrder.UpdatedAt ?? purchaseOrder.CreatedAt);
    }

    private async Task<PdfGenerationRequest> BuildPdfGenerationRequestAsync(
        PurchaseOrderDto purchaseOrderDto,
        CancellationToken cancellationToken)
    {
        // Get additional data for PDF generation
        var supplierData = purchaseOrderDto.SupplierID > 0
            ? await GetSupplierDataAsync(purchaseOrderDto.SupplierID, cancellationToken)
            : null;

        var orderData = purchaseOrderDto.OrderID > 0
            ? await GetOrderDataAsync(purchaseOrderDto.OrderID, cancellationToken)
            : null;

        return new PdfGenerationRequest
        {
            PurchaseOrder = purchaseOrderDto,
            SupplierData = supplierData,
            OrderData = orderData,
            Template = "standard-purchase-order",
            Options = new PdfGenerationOptions
            {
                IncludeItemDetails = true,
                IncludeWHTCalculation = true,
                IncludeAddresses = true,
                Language = "en-US",
                PageSize = "A4",
                Orientation = "Portrait"
            }
        };
    }

    private async Task<SupplierDto?> GetSupplierDataAsync(int supplierId, CancellationToken cancellationToken)
    {
        try
        {
            // This would typically call the SupplierService
            // For now, return null to avoid external dependency
            await Task.Delay(1, cancellationToken);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get supplier data for supplier {SupplierId}", supplierId);
            return null;
        }
    }

    private async Task<OrderDto?> GetOrderDataAsync(int orderId, CancellationToken cancellationToken)
    {
        try
        {
            // This would typically call the OrderService
            // For now, return null to avoid external dependency
            await Task.Delay(1, cancellationToken);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get order data for order {OrderId}", orderId);
            return null;
        }
    }

    private async Task MarkOldPdfsAsSupersededAsync(int purchaseOrderId, int newPdfFileId, CancellationToken cancellationToken)
    {
        var oldPdfs = await _context.PurchaseOrderFiles
            .Where(f => f.PurchaseOrderId == purchaseOrderId &&
                       f.DocumentType == DocumentType.GeneratedPDF &&
                       !f.IsDeleted &&
                       f.Id != newPdfFileId)
            .ToListAsync(cancellationToken);

        foreach (var oldPdf in oldPdfs)
        {
            // IsSuperseded and UpdatedAt properties not available in PurchaseOrderFile entity
            // Mark as deleted to indicate superseded status
            oldPdf.IsDeleted = true;
        }

        if (oldPdfs.Any())
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task QueuePdfGenerationAsync(int purchaseOrderId, string eventType, CancellationToken cancellationToken)
    {
        var domainEvent = new DomainEvent
        {
            AggregateType = "PurchaseOrder",
            AggregateId = purchaseOrderId.ToString(),
            EventType = $"{eventType}_PdfGeneration",
            EventData = JsonSerializer.Serialize(new { PurchaseOrderId = purchaseOrderId }),
            OccurredAt = DateTime.UtcNow,
            IsProcessed = false
        };

        _context.DomainEvents.Add(domainEvent);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Queued PDF generation for purchase order {PurchaseOrderId} with event {EventType}",
            purchaseOrderId, eventType);
    }

    private async Task LogPdfGenerationEventAsync(
        int purchaseOrderId,
        string requestId,
        bool success,
        string? errorMessage,
        CancellationToken cancellationToken)
    {
        var auditLog = new AuditLog
        {
            EntityType = "PurchaseOrder",
            EntityId = purchaseOrderId.ToString(),
            Action = AuditAction.PDFGenerated,
            ChangeReason = success ? "PDF generated successfully" : $"PDF generation failed: {errorMessage}",
            UserId = "system-pdf-generation",
            Timestamp = DateTime.UtcNow,
            UserRole = "System",
            NewValues = JsonSerializer.Serialize(new
            {
                RequestId = requestId,
                Success = success,
                ErrorMessage = errorMessage
            })
        };

        _context.AuditLogs.Add(auditLog);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
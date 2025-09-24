using Moq;
using Maliev.PurchaseOrderService.Api.ExternalServices;
using Maliev.PurchaseOrderService.Api.Services;
using Maliev.PurchaseOrderService.Api.DTOs;
using Maliev.PurchaseOrderService.Data.Enums;

namespace Maliev.PurchaseOrderService.Tests.TestInfrastructure;

/// <summary>
/// Factory for creating and configuring mock services with consistent behavior
/// </summary>
public static class MockServiceFactory
{
    #region External Service Mocks

    /// <summary>
    /// Creates a configured mock for ISupplierServiceClient
    /// </summary>
    public static Mock<ISupplierServiceClient> CreateSupplierServiceMock()
    {
        var mock = new Mock<ISupplierServiceClient>();

        // Default successful validation
        mock.Setup(x => x.ValidateSupplierAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestDataFactory.CreateSupplierDto());

        // Get supplier by ID
        mock.Setup(x => x.GetSupplierAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int id, CancellationToken _) => TestDataFactory.CreateSupplierDto());

        // Get supplier contact
        mock.Setup(x => x.GetSupplierContactAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SupplierContactDto
            {
                SupplierId = 1234,
                ContactName = "Test Contact",
                ContactEmail = "contact@supplier.com",
                ContactPhone = "+66-2-555-0100"
            });

        return mock;
    }

    /// <summary>
    /// Creates a configured mock for ICurrencyServiceClient
    /// </summary>
    public static Mock<ICurrencyServiceClient> CreateCurrencyServiceMock()
    {
        var mock = new Mock<ICurrencyServiceClient>();

        // Default currency validation
        mock.Setup(x => x.ValidateCurrencyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestDataFactory.CreateCurrencyDto("THB", "Thai Baht"));

        // Get currency info by code
        mock.Setup(x => x.GetCurrencyInfoAsync("THB", It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestDataFactory.CreateCurrencyDto("THB", "Thai Baht"));

        mock.Setup(x => x.GetCurrencyInfoAsync("USD", It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestDataFactory.CreateCurrencyDto("USD", "US Dollar"));

        // Exchange rates
        mock.Setup(x => x.GetExchangeRateAsync("USD", "THB", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExchangeRateDto
            {
                FromCurrency = "USD",
                ToCurrency = "THB",
                Rate = 35.25m,
                RateDate = DateTime.UtcNow,
                Source = "Bank of Thailand"
            });

        // Currency conversion
        mock.Setup(x => x.ConvertCurrencyAsync(It.IsAny<decimal>(), "USD", "THB", It.IsAny<CancellationToken>()))
            .ReturnsAsync((decimal amount, string from, string to, CancellationToken _) =>
                new CurrencyConversionDto
                {
                    FromCurrency = from,
                    ToCurrency = to,
                    OriginalAmount = amount,
                    ConvertedAmount = amount * 35.25m,
                    ExchangeRate = 35.25m,
                    ConversionDate = DateTime.UtcNow
                });

        return mock;
    }

    /// <summary>
    /// Creates a configured mock for IOrderServiceClient
    /// </summary>
    public static Mock<IOrderServiceClient> CreateOrderServiceMock()
    {
        var mock = new Mock<IOrderServiceClient>();

        // Default order items
        mock.Setup(x => x.GetOrderItemsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OrderItemDto>
            {
                TestDataFactory.CreateOrderItemDto(quantity: 1, unitPrice: 1000.00m),
                TestDataFactory.CreateOrderItemDto(quantity: 2, unitPrice: 500.00m)
            });

        // Validate order for purchase order creation
        mock.Setup(x => x.ValidateOrderForPurchaseOrderAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Get order by ID
        mock.Setup(x => x.GetOrderAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OrderDto
            {
                Id = 5678,
                OrderNumber = "ORD-5678",
                Status = "Active",
                CustomerName = "Test Customer",
                TotalAmount = 1500.00m,
                CreatedAt = DateTime.UtcNow.AddDays(-5)
            });

        return mock;
    }

    /// <summary>
    /// Creates a configured mock for IUploadServiceClient
    /// </summary>
    public static Mock<IUploadServiceClient> CreateUploadServiceMock()
    {
        var mock = new Mock<IUploadServiceClient>();

        // Successful file upload
        mock.Setup(x => x.UploadFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Stream stream, string fileName, string contentType, string category, CancellationToken _) =>
                new FileUploadResultDto
                {
                    FileId = Guid.NewGuid().ToString(),
                    FileName = fileName,
                    FileSize = stream.Length,
                    ContentType = contentType,
                    Category = category,
                    UploadedAt = DateTime.UtcNow,
                    IsSuccess = true
                });

        // Successful file deletion
        mock.Setup(x => x.DeleteFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Get file info
        mock.Setup(x => x.GetFileInfoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string fileId, CancellationToken _) => new FileInfoDto
            {
                FileId = fileId,
                FileName = "test-document.pdf",
                FileSize = 1024,
                ContentType = "application/pdf",
                Category = "purchase-orders",
                UploadedAt = DateTime.UtcNow
            });

        // Get download URL
        mock.Setup(x => x.GetDownloadUrlAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string fileId, int expiry, CancellationToken _) =>
                new FileDownloadUrlDto
                {
                    DownloadUrl = $"https://storage.googleapis.com/test/{fileId}",
                    ExpiresAt = DateTime.UtcNow.AddMinutes(expiry),
                    IsTemporary = false
                });

        return mock;
    }

    #endregion

    #region Application Service Mocks

    /// <summary>
    /// Creates a configured mock for IDomainEventService
    /// </summary>
    public static Mock<IDomainEventService> CreateDomainEventServiceMock()
    {
        var mock = new Mock<IDomainEventService>();

        // Successful event publishing
        mock.Setup(x => x.PublishEventAsync(It.IsAny<DomainEventDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Random.Shared.NextInt64(1, 1000000));

        // Get unprocessed events
        mock.Setup(x => x.GetUnprocessedEventsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DomainEventDto>
            {
                TestDataFactory.CreateDomainEventDto("PurchaseOrderCreated"),
                TestDataFactory.CreateDomainEventDto("PurchaseOrderUpdated")
            });

        // Mark event as processed
        mock.Setup(x => x.MarkEventAsProcessedAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Get events by entity
        mock.Setup(x => x.GetEventsByEntityAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string entityType, string entityId, CancellationToken _) => new List<DomainEventDto>
            {
                TestDataFactory.CreateDomainEventDto($"{entityType}Created", entityId),
                TestDataFactory.CreateDomainEventDto($"{entityType}Updated", entityId)
            });

        return mock;
    }

    /// <summary>
    /// Creates a configured mock for IPdfGenerationService
    /// </summary>
    public static Mock<IPdfGenerationService> CreatePdfGenerationServiceMock()
    {
        var mock = new Mock<IPdfGenerationService>();

        // Successful PDF generation
        mock.Setup(x => x.GeneratePurchaseOrderPdfAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PdfGenerationResult
            {
                Success = true,
                FileSize = 2048,
                GeneratedAt = DateTime.UtcNow,
                GenerationTime = TimeSpan.FromSeconds(2),
                RequestId = Guid.NewGuid().ToString(),
                IsAsync = false,
                FilePath = "/storage/pdfs/purchase-order.pdf",
                PdfFile = new PurchaseOrderFileDto
                {
                    Id = 1,
                    PurchaseOrderId = 1,
                    FileName = "purchase-order.pdf",
                    ObjectName = "purchase-orders/1/pdfs/purchase-order.pdf",
                    DocumentType = Data.Enums.DocumentType.GeneratedPDF,
                    FileSize = 2048,
                    ContentType = "application/pdf",
                    UploadedBy = "system",
                    UploadedAt = DateTime.UtcNow,
                    Description = "Generated PDF for purchase order"
                }
            });

        // Check if PDF generation is applicable
        mock.Setup(x => x.IsPdfGenerationApplicable(It.IsAny<PurchaseOrderDto>()))
            .Returns(true);

        return mock;
    }

    /// <summary>
    /// Creates a configured mock for IWHTCalculationService
    /// </summary>
    public static Mock<IWHTCalculationService> CreateWHTCalculationServiceMock()
    {
        var mock = new Mock<IWHTCalculationService>();

        // Default WHT calculation
        mock.Setup(x => x.CalculateWHTAsync(It.IsAny<SupplierDto>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SupplierDto supplier, decimal amount, string currency, CancellationToken _) =>
                TestDataFactory.CreateWHTCalculationResult(amount, 0.03m));

        // Get WHT rate
        mock.Setup(x => x.GetWHTRate(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
            .Returns(0.03m);

        // Check WHT applicability
        mock.Setup(x => x.IsWHTApplicable(It.IsAny<SupplierDto>(), It.IsAny<decimal>(), It.IsAny<string>()))
            .Returns(true);

        return mock;
    }

    /// <summary>
    /// Creates a configured mock for IDocumentManagementService
    /// </summary>
    public static Mock<IDocumentManagementService> CreateDocumentManagementServiceMock()
    {
        var mock = new Mock<IDocumentManagementService>();

        // Upload document
        mock.Setup(x => x.UploadDocumentAsync(It.IsAny<int>(), It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int purchaseOrderId, Stream stream, string fileName, string contentType, string uploadedBy, CancellationToken _) =>
                TestDataFactory.CreateDocumentUploadResult(fileName));

        // Delete document
        mock.Setup(x => x.DeleteDocumentAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Get documents for purchase order
        mock.Setup(x => x.GetDocumentsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int purchaseOrderId, CancellationToken _) =>
                new List<PurchaseOrderFileDto>
                {
                    new()
                    {
                        Id = 1,
                        PurchaseOrderId = purchaseOrderId,
                        FileName = "test-document-1.pdf",
                        ContentType = "application/pdf",
                        FileSize = 2048,
                        ObjectName = "test-obj-1",
                        DocumentType = DocumentType.Invoice,
                        UploadedBy = "test-user",
                        UploadedAt = DateTime.UtcNow.AddDays(-1),
                        Description = "Test invoice document",
                        IsDeleted = false
                    },
                    new()
                    {
                        Id = 2,
                        PurchaseOrderId = purchaseOrderId,
                        FileName = "test-document-2.pdf",
                        ContentType = "application/pdf",
                        FileSize = 1536,
                        ObjectName = "test-obj-2",
                        DocumentType = DocumentType.Reference,
                        UploadedBy = "test-user",
                        UploadedAt = DateTime.UtcNow.AddHours(-2),
                        Description = "Test reference document",
                        IsDeleted = false
                    }
                });

        // Get document metadata
        mock.Setup(x => x.GetDocumentMetadataAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int fileId, CancellationToken _) =>
                new PurchaseOrderFileDto
                {
                    Id = fileId,
                    PurchaseOrderId = 1,
                    FileName = $"test-document-{fileId}.pdf",
                    ContentType = "application/pdf",
                    FileSize = 2048,
                    ObjectName = $"test-obj-{fileId}",
                    DocumentType = DocumentType.Invoice,
                    UploadedBy = "test-user",
                    UploadedAt = DateTime.UtcNow,
                    Description = "Test document",
                    IsDeleted = false
                });

        // Download document
        mock.Setup(x => x.DownloadDocumentAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int documentId, CancellationToken _) =>
                new DocumentDownloadResult
                {
                    Success = true,
                    FileName = "test-document.pdf",
                    ContentType = "application/pdf",
                    FileSize = 1024
                });

        // Update document
        mock.Setup(x => x.UpdateDocumentAsync(It.IsAny<int>(), It.IsAny<UpdateDocumentRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int fileId, UpdateDocumentRequest request, string updatedBy, CancellationToken _) =>
                new PurchaseOrderFileDto
                {
                    Id = fileId,
                    PurchaseOrderId = 1,
                    FileName = request.FileName ?? $"updated-document-{fileId}.pdf",
                    ContentType = "application/pdf",
                    FileSize = 2048,
                    ObjectName = $"test-obj-{fileId}",
                    DocumentType = request.DocumentType ?? DocumentType.Invoice,
                    UploadedBy = "test-user",
                    UploadedAt = DateTime.UtcNow,
                    Description = request.Description ?? "Updated test document",
                    IsDeleted = false
                });

        // Validate file
        mock.Setup(x => x.ValidateFile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<long>()))
            .Returns((string fileName, string contentType, long fileSize) =>
                new DocumentValidationResult
                {
                    IsValid = true,
                    FileSize = fileSize,
                    MaxFileSize = 50 * 1024 * 1024,
                    AllowedExtensions = new[] { ".pdf", ".doc", ".docx", ".jpg", ".png" },
                    DetectedFileType = contentType,
                    IsFileTypeAllowed = true,
                    IsSizeValid = fileSize <= 50 * 1024 * 1024
                });

        // Generate preview URL
        mock.Setup(x => x.GeneratePreviewUrlAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int fileId, CancellationToken _) =>
                $"https://preview.example.com/file/{fileId}");

        // Archive old documents
        mock.Setup(x => x.ArchiveOldDocumentsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        return mock;
    }

    /// <summary>
    /// Creates a configured mock for IPdfServiceClient
    /// </summary>
    public static Mock<IPdfServiceClient> CreatePdfServiceClientMock()
    {
        var mock = new Mock<IPdfServiceClient>();

        // Successful PDF generation from HTML
        mock.Setup(x => x.GeneratePdfFromHtmlAsync(It.IsAny<string>(), It.IsAny<PdfGenerationOptionsDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string html, PdfGenerationOptionsDto options, CancellationToken _) =>
                new PdfGenerationResultDto
                {
                    DocumentId = Guid.NewGuid().ToString(),
                    FileName = "generated.pdf",
                    FileSize = 2048,
                    PageCount = 1,
                    IsSuccess = true,
                    GeneratedAt = DateTime.UtcNow,
                    GenerationTime = TimeSpan.FromSeconds(2)
                });

        // Successful PDF generation from template
        mock.Setup(x => x.GeneratePdfFromTemplateAsync(It.IsAny<string>(), It.IsAny<Dictionary<string, object>>(), It.IsAny<PdfGenerationOptionsDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string templateId, Dictionary<string, object> data, PdfGenerationOptionsDto options, CancellationToken _) =>
                new PdfGenerationResultDto
                {
                    DocumentId = Guid.NewGuid().ToString(),
                    FileName = $"{templateId}-output.pdf",
                    FileSize = 3072,
                    PageCount = 1,
                    IsSuccess = true,
                    GeneratedAt = DateTime.UtcNow,
                    GenerationTime = TimeSpan.FromSeconds(3)
                });

        // Get PDF info
        mock.Setup(x => x.GetPdfInfoAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PdfInfoDto
            {
                Title = "Test PDF",
                Author = "Test Author",
                PageCount = 1,
                FileSize = 2048,
                CreationDate = DateTime.UtcNow,
                Version = "1.4"
            });

        // Get available templates
        mock.Setup(x => x.GetAvailableTemplatesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PdfTemplateDto>
            {
                new()
                {
                    Id = "purchase-order",
                    Name = "Purchase Order Template",
                    Description = "Standard purchase order template",
                    Category = "business",
                    Version = "1.0",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                }
            });

        // Validate PDF
        mock.Setup(x => x.ValidatePdfAsync(It.IsAny<Stream>(), It.IsAny<PdfValidationOptionsDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PdfValidationResultDto
            {
                IsValid = true,
                PageCount = 1,
                FileSize = 2048,
                PdfVersion = "1.4",
                ValidatedAt = DateTime.UtcNow
            });

        return mock;
    }

    #endregion

    #region Failure Scenarios

    /// <summary>
    /// Configures mocks to simulate service failures
    /// </summary>
    public static void ConfigureFailureScenarios(
        Mock<ISupplierServiceClient>? supplierMock = null,
        Mock<ICurrencyServiceClient>? currencyMock = null,
        Mock<IOrderServiceClient>? orderMock = null,
        Mock<IUploadServiceClient>? uploadMock = null)
    {
        // Supplier service failures
        supplierMock?.Setup(x => x.ValidateSupplierAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("SupplierService is unavailable"));

        // Currency service failures
        currencyMock?.Setup(x => x.ValidateCurrencyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException("CurrencyService timeout"));

        // Order service failures
        orderMock?.Setup(x => x.GetOrderItemsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("OrderService circuit breaker open"));

        // Upload service failures
        uploadMock?.Setup(x => x.UploadFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("UploadService storage quota exceeded"));
    }

    /// <summary>
    /// Configures mocks to simulate authentication failures
    /// </summary>
    public static void ConfigureAuthenticationFailures(params Mock[] mocks)
    {
        foreach (var mock in mocks)
        {
            // Configure all methods to throw unauthorized exceptions
            if (mock is Mock<ISupplierServiceClient> supplierMock)
            {
                supplierMock.Setup(x => x.ValidateSupplierAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new UnauthorizedAccessException("Invalid authentication token"));
            }

            if (mock is Mock<ICurrencyServiceClient> currencyMock)
            {
                currencyMock.Setup(x => x.ValidateCurrencyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new UnauthorizedAccessException("Invalid authentication token"));
            }

            if (mock is Mock<IOrderServiceClient> orderMock)
            {
                orderMock.Setup(x => x.GetOrderItemsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(new UnauthorizedAccessException("Invalid authentication token"));
            }
        }
    }

    #endregion
}
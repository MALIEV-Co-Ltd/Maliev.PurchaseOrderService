using Maliev.PurchaseOrderService.Application.DTOs;
using Maliev.PurchaseOrderService.Application.Interfaces;
using Maliev.PurchaseOrderService.Domain.Entities;
using Maliev.PurchaseOrderService.Domain.Enumerations;
using Maliev.PurchaseOrderService.Infrastructure.Consumers;
using Maliev.PurchaseOrderService.Infrastructure.Persistence;
using Maliev.PurchaseOrderService.Infrastructure.Services;
using Maliev.MessagingContracts.Contracts.Search;
using Maliev.MessagingContracts.Contracts.Shared;
using MassTransit;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Maliev.PurchaseOrderService.Tests.Unit;

public class PurchaseOrderServiceMethodTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly PurchaseOrderContext _context;
    private readonly PurchaseOrderServiceImpl _service;
    private readonly Mock<ILogger<PurchaseOrderServiceImpl>> _loggerMock;
    private readonly Mock<ISupplierServiceClient> _supplierClientMock;
    private readonly Mock<IOrderServiceClient> _orderClientMock;
    private readonly Mock<ICurrencyServiceClient> _currencyClientMock;
    private readonly Mock<IWHTCalculationService> _whtCalculatorMock;
    private readonly Mock<IAuditLogService> _auditLogMock;
    private readonly Mock<IPublishEndpoint> _publishEndpointMock;

    public PurchaseOrderServiceMethodTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<PurchaseOrderContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new PurchaseOrderContext(options);
        _context.Database.EnsureCreated();
        _loggerMock = new Mock<ILogger<PurchaseOrderServiceImpl>>();
        _supplierClientMock = new Mock<ISupplierServiceClient>();
        _orderClientMock = new Mock<IOrderServiceClient>();
        _currencyClientMock = new Mock<ICurrencyServiceClient>();
        _whtCalculatorMock = new Mock<IWHTCalculationService>();
        _auditLogMock = new Mock<IAuditLogService>();
        _publishEndpointMock = new Mock<IPublishEndpoint>();

        _service = new PurchaseOrderServiceImpl(
            _context,
            _loggerMock.Object,
            _supplierClientMock.Object,
            _orderClientMock.Object,
            _currencyClientMock.Object,
            _whtCalculatorMock.Object,
            _auditLogMock.Object,
            _publishEndpointMock.Object);
    }

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WithValidRequest_CreatesPurchaseOrder()
    {
        var request = new CreatePurchaseOrderRequest
        {
            OrderType = OrderType.Internal,
            SupplierID = 1,
            OrderID = 100,
            CurrencyID = 1,
            WHTRate = 3,
            Items = new List<PartialOrderItemRequest>()
        };

        _supplierClientMock.Setup(x => x.GetSupplierAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Application.Interfaces.SupplierDto
            {
                Id = 1,
                Name = "Test Supplier",
                ContactInfo = "contact@test.com",
                Email = "supplier@test.com",
                Phone = "123456"
            });

        _orderClientMock.Setup(x => x.GetOrderAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Application.Interfaces.OrderDto
            {
                Id = 100,
                OrderNumber = "ORD-100",
                OrderDate = DateTime.UtcNow,
                Status = "Active",
                Items = new List<Application.Interfaces.OrderItemDto>()
            });

        _orderClientMock.Setup(x => x.GetOrderItemsAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Application.Interfaces.OrderItemDto>
            {
                new() { Id = 1, ProductCode = "P001", ProductName = "Product 1", Quantity = 10, UnitOfMeasure = "EA", UnitPrice = 100, TotalPrice = 1000, Currency = "THB" }
            });

        _currencyClientMock.Setup(x => x.GetCurrencyAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Application.Interfaces.CurrencyDto
            {
                Id = 1,
                Code = "THB",
                Symbol = "฿",
                Name = "Thai Baht",
                ExchangeRate = 1.0m
            });

        _whtCalculatorMock.Setup(x => x.CalculateWHT(1000m, 3m)).Returns(30m);

        var result = await _service.CreateAsync(request, "user123", "admin");

        Assert.NotNull(result);
        Assert.NotNull(result.OrderNumber);
        Assert.Equal("Test Supplier", result.SupplierName);
        _publishEndpointMock.Verify(
            endpoint => endpoint.Publish(
                It.Is<SearchDocumentUpsertedEvent>(message =>
                    message.Payload.SourceService == "PurchaseOrderService" &&
                    message.Payload.ResourceType == "purchase-order" &&
                    message.Payload.ResourceId == result.Id.ToString() &&
                    message.Payload.RequiredPermission == "purchase-order.orders.read"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithInvalidSupplier_ThrowsException()
    {
        var request = new CreatePurchaseOrderRequest
        {
            OrderType = OrderType.Internal,
            SupplierID = 999,
            OrderID = 100,
            CurrencyID = 1
        };

        _supplierClientMock.Setup(x => x.GetSupplierAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Application.Interfaces.SupplierDto?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateAsync(request, "user123", "admin"));
    }

    [Fact]
    public async Task CreateAsync_WithInvalidOrder_ThrowsException()
    {
        var request = new CreatePurchaseOrderRequest
        {
            OrderType = OrderType.Internal,
            SupplierID = 1,
            OrderID = 999,
            CurrencyID = 1
        };

        _supplierClientMock.Setup(x => x.GetSupplierAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Application.Interfaces.SupplierDto { Id = 1, Name = "Supplier" });

        _orderClientMock.Setup(x => x.GetOrderAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Application.Interfaces.OrderDto?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateAsync(request, "user123", "admin"));
    }

    [Fact]
    public async Task CreateAsync_WithInvalidCurrency_ThrowsException()
    {
        var request = new CreatePurchaseOrderRequest
        {
            OrderType = OrderType.Internal,
            SupplierID = 1,
            OrderID = 100,
            CurrencyID = 999
        };

        _supplierClientMock.Setup(x => x.GetSupplierAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Application.Interfaces.SupplierDto { Id = 1, Name = "Supplier" });

        _orderClientMock.Setup(x => x.GetOrderAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Application.Interfaces.OrderDto { Id = 100, OrderNumber = "ORD-001" });

        _currencyClientMock.Setup(x => x.GetCurrencyAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Application.Interfaces.CurrencyDto?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateAsync(request, "user123", "admin"));
    }

    [Fact]
    public async Task RegisterFileAsync_WithValidMetadata_PersistsPurchaseOrderFile()
    {
        var purchaseOrder = new PurchaseOrder
        {
            OrderNumber = "PO-2026-0001",
            SupplierID = 1,
            SupplierName = "Test Supplier",
            OrderID = 100,
            CurrencyID = 1,
            CurrencyCode = "THB",
            CurrencySymbol = "THB",
            OrderDate = DateTime.UtcNow,
            Status = OrderStatus.Pending,
            OrderType = OrderType.Internal,
            DepartmentId = 1,
            CreatedBy = "user123",
            CreatedAt = DateTime.UtcNow
        };
        _context.PurchaseOrders.Add(purchaseOrder);
        await _context.SaveChangesAsync();

        var result = await _service.RegisterFileAsync(
            purchaseOrder.Id,
            new RegisterPurchaseOrderFileRequest
            {
                FileName = "supplier-quote.pdf",
                ObjectName = "purchase-orders/1/supplier-quote.pdf",
                FileSize = 2048,
                ContentType = "application/pdf",
                DocumentType = DocumentType.Reference,
                Description = "Supplier quote"
            },
            "user123",
            "admin");

        Assert.Equal(purchaseOrder.Id, result.PurchaseOrderId);
        Assert.Equal("supplier-quote.pdf", result.FileName);
        Assert.Equal("purchase-orders/1/supplier-quote.pdf", result.ObjectName);
        Assert.Equal(2048, result.FileSize);
        Assert.Equal(DocumentType.Reference, result.DocumentType);
        Assert.Equal("user123", result.UploadedBy);

        var file = await _context.Files.SingleAsync();
        Assert.Equal(result.Id, file.Id);
        Assert.Equal("Supplier quote", file.Description);
        _auditLogMock.Verify(
            log => log.LogAuditAsync(
                "PurchaseOrder",
                purchaseOrder.Id.ToString(),
                AuditAction.Update,
                "user123",
                "admin",
                null,
                It.IsAny<string>(),
                null,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region ApproveAsync Tests

    [Fact]
    public async Task ApproveAsync_WithPendingOrder_TransitionsToApproved()
    {
        var po = CreateTestPurchaseOrder(1, "PO-001", "user1");
        _context.PurchaseOrders.Add(po);
        await _context.SaveChangesAsync();

        var result = await _service.ApproveAsync(1, "approver", "admin");

        Assert.NotNull(result.ApprovedBy);
        Assert.NotNull(result.ApprovedAt);
    }

    [Fact]
    public async Task ApproveAsync_WithApprovedOrder_ThrowsException()
    {
        var po = CreateTestPurchaseOrder(1, "PO-001", "user1");
        _context.PurchaseOrders.Add(po);
        await _context.SaveChangesAsync();

        await _service.ApproveAsync(1, "approver1", "admin");

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.ApproveAsync(1, "approver2", "admin"));
    }

    #endregion

    #region CancelAsync Tests

    [Fact]
    public async Task CancelAsync_WithPendingOrder_TransitionsToCancelled()
    {
        var po = CreateTestPurchaseOrder(1, "PO-001", "user1");
        _context.PurchaseOrders.Add(po);
        await _context.SaveChangesAsync();

        var result = await _service.CancelAsync(1, "cancellation reason", "canceller", "admin");

        Assert.NotNull(result);
    }

    [Fact]
    public async Task CancelAsync_WithDeliveredOrder_ThrowsException()
    {
        var po = CreateTestPurchaseOrder(1, "PO-001", "user1");
        po.Status = OrderStatus.Delivered;
        _context.PurchaseOrders.Add(po);
        await _context.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CancelAsync(1, "cancellation reason", "canceller", "admin"));
    }

    #endregion

    [Fact]
    public async Task SearchReindexRequestedConsumer_WhenRequested_RepublishesPurchaseOrderSearchDocuments()
    {
        _context.PurchaseOrders.Add(CreateTestPurchaseOrder(1, "PO-001", "user1"));
        _context.PurchaseOrders.Add(CreateTestPurchaseOrder(2, "PO-002", "user2"));
        await _context.SaveChangesAsync();

        var consumer = new SearchReindexRequestedConsumer(
            _context,
            _publishEndpointMock.Object,
            Mock.Of<ILogger<SearchReindexRequestedConsumer>>());

        var command = new SearchReindexRequestedCommand(
            Guid.NewGuid(),
            nameof(SearchReindexRequestedCommand),
            MessageType.Command,
            "1.0.0",
            "SearchService",
            ["PurchaseOrderService"],
            Guid.NewGuid(),
            null,
            DateTimeOffset.UtcNow,
            false,
            new SearchReindexRequestedCommandPayload(null, "test", DateTimeOffset.UtcNow));

        var consumeContext = new Mock<ConsumeContext<SearchReindexRequestedCommand>>();
        consumeContext.SetupGet(context => context.Message).Returns(command);
        consumeContext.SetupGet(context => context.CancellationToken).Returns(CancellationToken.None);

        await consumer.Consume(consumeContext.Object);

        _publishEndpointMock.Verify(
            endpoint => endpoint.Publish(
                It.Is<SearchDocumentUpsertedEvent>(message =>
                    message.Payload.SourceService == "PurchaseOrderService" &&
                    message.Payload.ResourceType == "purchase-order"),
                It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    #region SendToSupplierAsync Tests

    [Fact]
    public async Task SendToSupplierAsync_WithApprovedOrder_TransitionsToOrdered()
    {
        var po = CreateTestPurchaseOrder(1, "PO-001", "user1");
        _context.PurchaseOrders.Add(po);
        await _context.SaveChangesAsync();

        await _service.ApproveAsync(1, "approver", "admin");

        var result = await _service.SendToSupplierAsync(1, "sender", "admin");

        Assert.NotNull(result);
    }

    [Fact]
    public async Task SendToSupplierAsync_WithPendingOrder_ThrowsException()
    {
        var po = CreateTestPurchaseOrder(1, "PO-001", "user1");
        _context.PurchaseOrders.Add(po);
        await _context.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.SendToSupplierAsync(1, "sender", "admin"));
    }

    #endregion

    #region ReceiveGoodsAsync Tests

    [Fact]
    public async Task ReceiveGoodsAsync_WithOrderedOrder_TransitionsToDelivered()
    {
        var po = CreateTestPurchaseOrder(1, "PO-001", "user1");
        _context.PurchaseOrders.Add(po);
        await _context.SaveChangesAsync();

        await _service.ApproveAsync(1, "approver", "admin");
        await _service.SendToSupplierAsync(1, "sender", "admin");

        var result = await _service.ReceiveGoodsAsync(1, false, "receiver", "admin");

        Assert.NotNull(result);
    }

    [Fact]
    public async Task ReceiveGoodsAsync_WithOrderedOrder_PartialReceipt_StaysOrdered()
    {
        var po = CreateTestPurchaseOrder(1, "PO-001", "user1");
        _context.PurchaseOrders.Add(po);
        await _context.SaveChangesAsync();

        await _service.ApproveAsync(1, "approver", "admin");
        await _service.SendToSupplierAsync(1, "sender", "admin");

        var result = await _service.ReceiveGoodsAsync(1, true, "receiver", "admin");

        Assert.NotNull(result);
    }

    [Fact]
    public async Task ReceiveGoodsAsync_WithPendingOrder_ThrowsException()
    {
        var po = CreateTestPurchaseOrder(1, "PO-001", "user1");
        _context.PurchaseOrders.Add(po);
        await _context.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.ReceiveGoodsAsync(1, false, "receiver", "admin"));
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithValidRequest_UpdatesPurchaseOrder()
    {
        var po = CreateTestPurchaseOrder(1, "PO-001", "user1");
        _context.PurchaseOrders.Add(po);
        await _context.SaveChangesAsync();

        var request = new UpdatePurchaseOrderRequest
        {
            CurrencyID = 1,
            CustomerPO = "CPO-001"
        };

        _currencyClientMock.Setup(x => x.GetCurrencyAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Application.Interfaces.CurrencyDto { Id = 1, Code = "USD", ExchangeRate = 35m });

        _whtCalculatorMock.Setup(x => x.CalculateWHT(It.IsAny<decimal>(), It.IsAny<decimal?>())).Returns(30m);

        var result = await _service.UpdateAsync(1, request, "updater", "admin");

        Assert.NotNull(result);
        Assert.Equal("CPO-001", result.CustomerPO);
    }

    #endregion

    private static PurchaseOrder CreateTestPurchaseOrder(int id, string orderNumber, string createdBy)
    {
        return new PurchaseOrder
        {
            Id = id,
            OrderNumber = orderNumber,
            SupplierID = 1,
            SupplierName = "Test Supplier",
            OrderID = 100 + id,
            CurrencyID = 1,
            CurrencyCode = "THB",
            CurrencySymbol = "฿",
            Status = OrderStatus.Pending,
            OrderType = OrderType.Internal,
            SubtotalAmount = 1000m,
            TotalAmount = 1000m,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }
}

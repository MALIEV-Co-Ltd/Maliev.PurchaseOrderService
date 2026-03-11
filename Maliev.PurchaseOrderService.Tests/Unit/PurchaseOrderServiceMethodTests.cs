using Maliev.PurchaseOrderService.Application.DTOs;
using Maliev.PurchaseOrderService.Application.Interfaces;
using Maliev.PurchaseOrderService.Domain.Entities;
using Maliev.PurchaseOrderService.Domain.Enumerations;
using Maliev.PurchaseOrderService.Infrastructure.Persistence;
using Maliev.PurchaseOrderService.Infrastructure.Services;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Maliev.PurchaseOrderService.Tests.Unit;

public class PurchaseOrderServiceMethodTests
{
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
        var options = new DbContextOptionsBuilder<PurchaseOrderContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new PurchaseOrderContext(options);
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
}

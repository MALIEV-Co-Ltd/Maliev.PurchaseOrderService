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

public class PurchaseOrderServiceImplTests
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

    public PurchaseOrderServiceImplTests()
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

    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsPurchaseOrder()
    {
        var po = CreateTestPurchaseOrder(1, "PO-001", "admin");
        _context.PurchaseOrders.Add(po);
        await _context.SaveChangesAsync();

        var result = await _service.GetByIdAsync(1, "admin", "admin");

        Assert.NotNull(result);
        Assert.Equal("PO-001", result.OrderNumber);
    }

    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ReturnsNull()
    {
        var result = await _service.GetByIdAsync(999, "admin", "admin");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_AsEmployee_ReturnsOnlyOwnOrders()
    {
        var po1 = CreateTestPurchaseOrder(1, "PO-001", "user1");
        var po2 = CreateTestPurchaseOrder(2, "PO-002", "user2");
        _context.PurchaseOrders.AddRange(po1, po2);
        await _context.SaveChangesAsync();

        var result = await _service.GetByIdAsync(2, "user1", "employee");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_AsAdmin_ReturnsAnyOrder()
    {
        var po = CreateTestPurchaseOrder(1, "PO-001", "user1");
        _context.PurchaseOrders.Add(po);
        await _context.SaveChangesAsync();

        var result = await _service.GetByIdAsync(1, "admin", "admin");

        Assert.NotNull(result);
        Assert.Equal("PO-001", result.OrderNumber);
    }

    [Fact]
    public async Task GetByIdAsync_ValidId_MapsAllFields()
    {
        var po = CreateTestPurchaseOrder(1, "PO-001", "admin");
        po.SupplierContactInfo = "contact@supplier.com";
        po.CustomerPO = "CPO-123";
        po.WHTRate = 3m;
        po.WHTAmount = 30m;
        po.Notes = "Test notes";
        po.ApprovedBy = "approver";
        po.ApprovedAt = DateTime.UtcNow;
        po.SubtotalAmount = 1000m;
        _context.PurchaseOrders.Add(po);
        await _context.SaveChangesAsync();

        var result = await _service.GetByIdAsync(1, "admin", "admin");

        Assert.NotNull(result);
        Assert.Equal("PO-001", result.OrderNumber);
        Assert.Equal("contact@supplier.com", result.SupplierContactInfo);
        Assert.Equal("CPO-123", result.CustomerPO);
        Assert.Equal(3m, result.WHTRate);
        Assert.Equal(30m, result.WHTAmount);
        Assert.Equal("Test notes", result.Notes);
        Assert.Equal("approver", result.ApprovedBy);
    }

    [Fact]
    public async Task SearchAsync_ReturnsPaginatedResults()
    {
        for (int i = 1; i <= 15; i++)
        {
            _context.PurchaseOrders.Add(CreateTestPurchaseOrder(i, $"PO-{i:D3}", "admin"));
        }
        await _context.SaveChangesAsync();

        var request = new SearchPurchaseOrdersRequest(SupplierId: null, Status: null, OrderType: null, OrderId: null, FromDate: null, ToDate: null, SortBy: null, SortDirection: null, Page: 1, PageSize: 10);
        var result = await _service.SearchAsync(request, "admin", "admin");

        Assert.Equal(15, result.TotalCount);
        Assert.Equal(10, result.Items.Count);
        Assert.Equal(1, result.Page);
        Assert.Equal(10, result.PageSize);
    }

    [Fact]
    public async Task SearchAsync_AsEmployee_ReturnsOnlyOwnOrders()
    {
        _context.PurchaseOrders.Add(CreateTestPurchaseOrder(1, "PO-001", "user1"));
        _context.PurchaseOrders.Add(CreateTestPurchaseOrder(2, "PO-002", "user2"));
        await _context.SaveChangesAsync();

        var request = new SearchPurchaseOrdersRequest(SupplierId: null, Status: null, OrderType: null, OrderId: null, FromDate: null, ToDate: null, SortBy: null, SortDirection: null, Page: 1, PageSize: 10);
        var result = await _service.SearchAsync(request, "user1", "employee");

        Assert.Equal(1, result.TotalCount);
        Assert.Equal("PO-001", result.Items[0].OrderNumber);
    }

    [Fact]
    public async Task SearchAsync_EmptyResults_ReturnsEmptyList()
    {
        var request = new SearchPurchaseOrdersRequest(SupplierId: null, Status: null, OrderType: null, OrderId: null, FromDate: null, ToDate: null, SortBy: null, SortDirection: null, Page: 1, PageSize: 10);
        var result = await _service.SearchAsync(request, "admin", "admin");

        Assert.Equal(0, result.TotalCount);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task SearchAsync_Page2_ReturnsCorrectItems()
    {
        for (int i = 1; i <= 15; i++)
        {
            _context.PurchaseOrders.Add(CreateTestPurchaseOrder(i, $"PO-{i:D3}", "admin"));
        }
        await _context.SaveChangesAsync();

        var request = new SearchPurchaseOrdersRequest(SupplierId: null, Status: null, OrderType: null, OrderId: null, FromDate: null, ToDate: null, SortBy: null, SortDirection: null, Page: 2, PageSize: 10);
        var result = await _service.SearchAsync(request, "admin", "admin");

        Assert.Equal(15, result.TotalCount);
        Assert.Equal(5, result.Items.Count);
        Assert.Equal("PO-005", result.Items[0].OrderNumber);
    }

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
            DepartmentId = 1,
            SubtotalAmount = 1000m,
            TotalAmount = 1000m,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow
        };
    }
}

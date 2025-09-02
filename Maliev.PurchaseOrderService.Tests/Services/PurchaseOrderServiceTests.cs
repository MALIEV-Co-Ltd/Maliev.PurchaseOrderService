namespace Maliev.PurchaseOrderService.Tests.Services
{
    using AutoMapper;
    using Maliev.PurchaseOrderService.Api.DTOs;
    using Maliev.PurchaseOrderService.Api.Services;
    using Maliev.PurchaseOrderService.Data;
    using Maliev.PurchaseOrderService.Data.Entities;
    using Microsoft.EntityFrameworkCore;
    using Moq;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Xunit;

    public class PurchaseOrderServiceTests : IDisposable
    {
        private readonly PurchaseOrderContext _context;
        private readonly IMapper _mapper;
        private readonly PurchaseOrderService _service;

        public PurchaseOrderServiceTests()
        {
            var options = new DbContextOptionsBuilder<PurchaseOrderContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new PurchaseOrderContext(options);
            _context.Database.EnsureCreated(); // Ensure the database is created for each test

            _mapper = new Mock<IMapper>().Object; // Initialize _mapper as a mock object

            // Setup mock behavior for IMapper.Map<TDestination>(TSource source)
            Mock.Get(_mapper).Setup(m => m.Map<IEnumerable<PurchaseOrderDto>>(It.IsAny<IEnumerable<PurchaseOrder>>()))
                .Returns((IEnumerable<PurchaseOrder> source) => source.Select(po => new PurchaseOrderDto { Id = po.Id, Notes = po.Notes, CreatedDate = po.CreatedDate, ModifiedDate = po.ModifiedDate }).ToList());

            Mock.Get(_mapper).Setup(m => m.Map<PurchaseOrderDto>(It.IsAny<PurchaseOrder>()))
                .Returns((PurchaseOrder source) => source == null ? null : new PurchaseOrderDto { Id = source.Id, Notes = source.Notes, CreatedDate = source.CreatedDate, ModifiedDate = source.ModifiedDate });

            Mock.Get(_mapper).Setup(m => m.Map<PurchaseOrder>(It.IsAny<CreatePurchaseOrderDto>()))
                .Returns((CreatePurchaseOrderDto source) => new PurchaseOrder { Notes = source.Notes, CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow });

            Mock.Get(_mapper).Setup(m => m.Map(It.IsAny<UpdatePurchaseOrderDto>(), It.IsAny<PurchaseOrder>()))
                .Callback((UpdatePurchaseOrderDto source, PurchaseOrder destination) => { destination.Notes = source.Notes; destination.ModifiedDate = DateTime.UtcNow; });

            // Similar setups for OrderItem, Address, and PurchaseOrderFile mappings
            Mock.Get(_mapper).Setup(m => m.Map<IEnumerable<OrderItemDto>>(It.IsAny<IEnumerable<OrderItem>>()))
                .Returns((IEnumerable<OrderItem> source) => source.Select(oi => new OrderItemDto { Id = oi.Id, PartNumber = oi.PartNumber }).ToList());
            Mock.Get(_mapper).Setup(m => m.Map<OrderItemDto>(It.IsAny<OrderItem>()))
                .Returns((OrderItem source) => source == null ? null : new OrderItemDto { Id = source.Id, PartNumber = source.PartNumber });
            Mock.Get(_mapper).Setup(m => m.Map<OrderItem>(It.IsAny<CreateOrderItemDto>()))
                .Returns((CreateOrderItemDto source) => new OrderItem { PurchaseOrderId = source.PurchaseOrderId, PartNumber = source.PartNumber, CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow });
            Mock.Get(_mapper).Setup(m => m.Map(It.IsAny<UpdateOrderItemDto>(), It.IsAny<OrderItem>()))
                .Callback((UpdateOrderItemDto source, OrderItem destination) => { destination.PartNumber = source.PartNumber; destination.ModifiedDate = DateTime.UtcNow; });

            Mock.Get(_mapper).Setup(m => m.Map<IEnumerable<AddressDto>>(It.IsAny<IEnumerable<Address>>()))
                .Returns((IEnumerable<Address> source) => source.Select(a => new AddressDto { Id = a.Id, Building = a.Building, AddressLine1 = a.AddressLine1, City = a.City, State = a.State, PostalCode = a.PostalCode, CountryId = a.CountryId }).ToList());
            Mock.Get(_mapper).Setup(m => m.Map<AddressDto>(It.IsAny<Address>()))
                .Returns((Address source) => source == null ? null : new AddressDto { Id = source.Id, Building = source.Building, AddressLine1 = source.AddressLine1, City = source.City, State = source.State, PostalCode = source.PostalCode, CountryId = source.CountryId });
            Mock.Get(_mapper).Setup(m => m.Map<Address>(It.IsAny<CreateAddressDto>()))
                .Returns((CreateAddressDto source) => new Address { Building = source.Building, AddressLine1 = source.AddressLine1, City = source.City, State = source.State, PostalCode = source.PostalCode, CountryId = source.CountryId, CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow });
            Mock.Get(_mapper).Setup(m => m.Map(It.IsAny<UpdateAddressDto>(), It.IsAny<Address>()))
                .Callback((UpdateAddressDto source, Address destination) => { destination.Building = source.Building; destination.AddressLine1 = source.AddressLine1; destination.City = source.City; destination.State = source.State; destination.PostalCode = source.PostalCode; destination.CountryId = source.CountryId; destination.ModifiedDate = DateTime.UtcNow; });

            Mock.Get(_mapper).Setup(m => m.Map<IEnumerable<PurchaseOrderFileDto>>(It.IsAny<IEnumerable<PurchaseOrderFile>>()))
                .Returns((IEnumerable<PurchaseOrderFile> source) => source.Select(pof => new PurchaseOrderFileDto { Id = pof.Id, Bucket = pof.Bucket, ObjectName = pof.ObjectName }).ToList());
            Mock.Get(_mapper).Setup(m => m.Map<PurchaseOrderFileDto>(It.IsAny<PurchaseOrderFile>()))
                .Returns((PurchaseOrderFile source) => source == null ? null : new PurchaseOrderFileDto { Id = source.Id, Bucket = source.Bucket, ObjectName = source.ObjectName });
            Mock.Get(_mapper).Setup(m => m.Map<PurchaseOrderFile>(It.IsAny<CreatePurchaseOrderFileDto>()))
                .Returns((CreatePurchaseOrderFileDto source) => new PurchaseOrderFile { PurchaseOrderId = source.PurchaseOrderId, Bucket = source.Bucket, ObjectName = source.ObjectName, CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow });
            Mock.Get(_mapper).Setup(m => m.Map(It.IsAny<UpdatePurchaseOrderFileDto>(), It.IsAny<PurchaseOrderFile>()))
                .Callback((UpdatePurchaseOrderFileDto source, PurchaseOrderFile destination) => { destination.Bucket = source.Bucket; destination.ObjectName = source.ObjectName; destination.ModifiedDate = DateTime.UtcNow; });

            _service = new PurchaseOrderService(_context, _mapper);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted(); // Clean up the database after each test
            _context.Dispose();
        }

        private void SeedData()
        {
            _context.PurchaseOrder.AddRange(
                new PurchaseOrder { Id = 1, CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow, Notes = "Test PO 1" },
                new PurchaseOrder { Id = 2, CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow, Notes = "Test PO 2" }
            );
            _context.Address.AddRange(
                new Address { Id = 1, Building = "B1", AddressLine1 = "A1", City = "C1", State = "S1", PostalCode = "P1", CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow },
                new Address { Id = 2, Building = "B2", AddressLine1 = "A2", City = "C2", State = "S2", PostalCode = "P2", CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow }
            );
            _context.OrderItem.AddRange(
                new OrderItem { Id = 1, PurchaseOrderId = 1, CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow, PartNumber = "PN1" },
                new OrderItem { Id = 2, PurchaseOrderId = 1, CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow, PartNumber = "PN2" }
            );
            _context.PurchaseOrderFile.AddRange(
                new PurchaseOrderFile { Id = 1, PurchaseOrderId = 1, Bucket = "Bucket1", ObjectName = "Object1", CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow },
                new PurchaseOrderFile { Id = 2, PurchaseOrderId = 1, Bucket = "Bucket2", ObjectName = "Object2", CreatedDate = DateTime.UtcNow, ModifiedDate = DateTime.UtcNow }
            );
            _context.SaveChanges();
        }

        [Fact]
        public async Task GetAllPurchaseOrdersAsync_ReturnsAllPurchaseOrders()
        {
            // Arrange
            SeedData();

            // Act
            var result = await _service.GetAllPurchaseOrdersAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
        }

        [Fact]
        public async Task GetPurchaseOrderByIdAsync_ReturnsPurchaseOrder_WhenFound()
        {
            // Arrange
            SeedData();

            // Act
            var result = await _service.GetPurchaseOrderByIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
        }

        [Fact]
        public async Task GetPurchaseOrderByIdAsync_ReturnsNull_WhenNotFound()
        {
            // Arrange
            // No seeding, so database is empty

            // Act
            var result = await _service.GetPurchaseOrderByIdAsync(99);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task CreatePurchaseOrderAsync_CreatesAndReturnsPurchaseOrder()
        {
            // Arrange
            var createDto = new CreatePurchaseOrderDto { Notes = "New PO" };

            // Act
            var result = await _service.CreatePurchaseOrderAsync(createDto);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Id > 0);
            Assert.Equal("New PO", result.Notes);
            Assert.Equal(1, _context.PurchaseOrder.Count());
        }

        [Fact]
        public async Task UpdatePurchaseOrderAsync_UpdatesAndReturnsPurchaseOrder_WhenFound()
        {
            // Arrange
            SeedData();
            var updateDto = new UpdatePurchaseOrderDto { Notes = "Updated PO" };

            // Act
            var result = await _service.UpdatePurchaseOrderAsync(1, updateDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Updated PO", result.Notes);
            Assert.Equal(2, _context.PurchaseOrder.Count()); // Still 2, one updated
        }

        [Fact]
        public async Task UpdatePurchaseOrderAsync_ReturnsNull_WhenNotFound()
        {
            // Arrange
            // No seeding
            var updateDto = new UpdatePurchaseOrderDto { Notes = "Updated PO" };

            // Act
            var result = await _service.UpdatePurchaseOrderAsync(99, updateDto);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task DeletePurchaseOrderAsync_ReturnsTrue_WhenFoundAndDeleted()
        {
            // Arrange
            SeedData();

            // Act
            var result = await _service.DeletePurchaseOrderAsync(1);

            // Assert
            Assert.True(result);
            Assert.Equal(1, _context.PurchaseOrder.Count());
        }

        [Fact]
        public async Task DeletePurchaseOrderAsync_ReturnsFalse_WhenNotFound()
        {
            // Arrange
            // No seeding

            // Act
            var result = await _service.DeletePurchaseOrderAsync(99);

            // Assert
            Assert.False(result);
        }

        // Order Items Tests
        [Fact]
        public async Task GetOrderItemsByPurchaseOrderIdAsync_ReturnsOrderItems()
        {
            // Arrange
            SeedData();

            // Act
            var result = await _service.GetOrderItemsByPurchaseOrderIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
        }

        [Fact]
        public async Task GetOrderItemByIdAsync_ReturnsOrderItem_WhenFound()
        {
            // Arrange
            SeedData();

            // Act
            var result = await _service.GetOrderItemByIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
        }

        [Fact]
        public async Task GetOrderItemByIdAsync_ReturnsNull_WhenNotFound()
        {
            // Arrange
            // No seeding

            // Act
            var result = await _service.GetOrderItemByIdAsync(99);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task CreateOrderItemAsync_CreatesAndReturnsOrderItem()
        {
            // Arrange
            var createDto = new CreateOrderItemDto { PurchaseOrderId = 1, PartNumber = "New PN" };

            // Act
            var result = await _service.CreateOrderItemAsync(createDto);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Id > 0);
            Assert.Equal("New PN", result.PartNumber);
            Assert.Equal(1, _context.OrderItem.Count());
        }

        [Fact]
        public async Task UpdateOrderItemAsync_UpdatesAndReturnsOrderItem_WhenFound()
        {
            // Arrange
            SeedData();
            var updateDto = new UpdateOrderItemDto { PartNumber = "Updated PN" };

            // Act
            var result = await _service.UpdateOrderItemAsync(1, updateDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Updated PN", result.PartNumber);
            Assert.Equal(2, _context.OrderItem.Count());
        }

        [Fact]
        public async Task UpdateOrderItemAsync_ReturnsNull_WhenNotFound()
        {
            // Arrange
            // No seeding
            var updateDto = new UpdateOrderItemDto { PartNumber = "Updated PN" };

            // Act
            var result = await _service.UpdateOrderItemAsync(99, updateDto);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task DeleteOrderItemAsync_ReturnsTrue_WhenFoundAndDeleted()
        {
            // Arrange
            SeedData();

            // Act
            var result = await _service.DeleteOrderItemAsync(1);

            // Assert
            Assert.True(result);
            Assert.Equal(1, _context.OrderItem.Count());
        }

        [Fact]
        public async Task DeleteOrderItemAsync_ReturnsFalse_WhenNotFound()
        {
            // Arrange
            // No seeding

            // Act
            var result = await _service.DeleteOrderItemAsync(99);

            // Assert
            Assert.False(result);
        }

        // Address Tests
        [Fact]
        public async Task GetAllAddressesAsync_ReturnsAllAddresses()
        {
            // Arrange
            SeedData();

            // Act
            var result = await _service.GetAllAddressesAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
        }

        [Fact]
        public async Task GetAddressByIdAsync_ReturnsAddress_WhenFound()
        {
            // Arrange
            SeedData();

            // Act
            var result = await _service.GetAddressByIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
        }

        [Fact]
        public async Task GetAddressByIdAsync_ReturnsNull_WhenNotFound()
        {
            // Arrange
            // No seeding

            // Act
            var result = await _service.GetAddressByIdAsync(99);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task CreateAddressAsync_CreatesAndReturnsAddress()
        {
            // Arrange
            var createDto = new CreateAddressDto { Building = "New B", AddressLine1 = "New A1", City = "New C", State = "New S", PostalCode = "New P", CountryId = 1 };

            // Act
            var result = await _service.CreateAddressAsync(createDto);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Id > 0);
            Assert.Equal("New B", result.Building);
            Assert.Equal(1, _context.Address.Count());
        }

        [Fact]
        public async Task UpdateAddressAsync_UpdatesAndReturnsAddress_WhenFound()
        {
            // Arrange
            SeedData();
            var updateDto = new UpdateAddressDto { Building = "Updated B", AddressLine1 = "Updated A1", City = "Updated C", State = "Updated S", PostalCode = "Updated P", CountryId = 1 };

            // Act
            var result = await _service.UpdateAddressAsync(1, updateDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Updated B", result.Building);
            Assert.Equal(2, _context.Address.Count());
        }

        [Fact]
        public async Task UpdateAddressAsync_ReturnsNull_WhenNotFound()
        {
            // Arrange
            // No seeding
            var updateDto = new UpdateAddressDto { Building = "Updated B", AddressLine1 = "Updated A1", City = "Updated C", State = "Updated S", PostalCode = "Updated P", CountryId = 1 };

            // Act
            var result = await _service.UpdateAddressAsync(99, updateDto);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task DeleteAddressAsync_ReturnsTrue_WhenFoundAndDeleted()
        {
            // Arrange
            SeedData();

            // Act
            var result = await _service.DeleteAddressAsync(1);

            // Assert
            Assert.True(result);
            Assert.Equal(1, _context.Address.Count());
        }

        [Fact]
        public async Task DeleteAddressAsync_ReturnsFalse_WhenNotFound()
        {
            // Arrange
            // No seeding

            // Act
            var result = await _service.DeleteAddressAsync(99);

            // Assert
            Assert.False(result);
        }

        // Purchase Order Files Tests
        [Fact]
        public async Task GetPurchaseOrderFilesByPurchaseOrderIdAsync_ReturnsPurchaseOrderFiles()
        {
            // Arrange
            SeedData();

            // Act
            var result = await _service.GetPurchaseOrderFilesByPurchaseOrderIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
        }

        [Fact]
        public async Task GetPurchaseOrderFileByIdAsync_ReturnsPurchaseOrderFile_WhenFound()
        {
            // Arrange
            SeedData();

            // Act
            var result = await _service.GetPurchaseOrderFileByIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
        }

        [Fact]
        public async Task GetPurchaseOrderFileByIdAsync_ReturnsNull_WhenNotFound()
        {
            // Arrange
            // No seeding

            // Act
            var result = await _service.GetPurchaseOrderFileByIdAsync(99);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task CreatePurchaseOrderFileAsync_CreatesAndReturnsPurchaseOrderFile()
        {
            // Arrange
            var createDto = new CreatePurchaseOrderFileDto { PurchaseOrderId = 1, Bucket = "New B", ObjectName = "New O" };

            // Act
            var result = await _service.CreatePurchaseOrderFileAsync(createDto);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Id > 0);
            Assert.Equal("New B", result.Bucket);
            Assert.Equal(1, _context.PurchaseOrderFile.Count());
        }

        [Fact]
        public async Task UpdatePurchaseOrderFileAsync_UpdatesAndReturnsPurchaseOrderFile_WhenFound()
        {
            // Arrange
            SeedData();
            var updateDto = new UpdatePurchaseOrderFileDto { PurchaseOrderId = 1, Bucket = "Updated B", ObjectName = "Updated O" };

            // Act
            var result = await _service.UpdatePurchaseOrderFileAsync(1, updateDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Updated B", result.Bucket);
            Assert.Equal(2, _context.PurchaseOrderFile.Count());
        }

        [Fact]
        public async Task UpdatePurchaseOrderFileAsync_ReturnsNull_WhenNotFound()
        {
            // Arrange
            // No seeding
            var updateDto = new UpdatePurchaseOrderFileDto { PurchaseOrderId = 1, Bucket = "Updated B", ObjectName = "Updated O" };

            // Act
            var result = await _service.UpdatePurchaseOrderFileAsync(99, updateDto);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task DeletePurchaseOrderFileAsync_ReturnsTrue_WhenFoundAndDeleted()
        {
            // Arrange
            SeedData();

            // Act
            var result = await _service.DeletePurchaseOrderFileAsync(1);

            // Assert
            Assert.True(result);
            Assert.Equal(1, _context.PurchaseOrderFile.Count());
        }

        [Fact]
        public async Task DeletePurchaseOrderFileAsync_ReturnsFalse_WhenNotFound()
        {
            // Arrange
            // No seeding

            // Act
            var result = await _service.DeletePurchaseOrderFileAsync(99);

            // Assert
            Assert.False(result);
        }
    }
}
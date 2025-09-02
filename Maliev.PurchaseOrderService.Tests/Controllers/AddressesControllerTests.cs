namespace Maliev.PurchaseOrderService.Tests.Controllers
{
    using Maliev.PurchaseOrderService.Api.Controllers;
    using Maliev.PurchaseOrderService.Api.DTOs;
    using Maliev.PurchaseOrderService.Api.Services;
    using Microsoft.AspNetCore.Mvc;
    using Moq;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Xunit;

    public class AddressesControllerTests
    {
        private readonly Mock<IPurchaseOrderService> _mockService;
        private readonly AddressesController _controller;

        public AddressesControllerTests()
        {
            _mockService = new Mock<IPurchaseOrderService>();
            _controller = new AddressesController(_mockService.Object);
        }

        [Fact]
        public async Task GetAddresses_ReturnsOkResult_WithListOfAddresses()
        {
            // Arrange
            var addresses = new List<AddressDto> { new AddressDto { Id = 1, Building = "B1", AddressLine1 = "A1", City = "C1", State = "S1", PostalCode = "P1" }, new AddressDto { Id = 2, Building = "B2", AddressLine1 = "A2", City = "C2", State = "S2", PostalCode = "P2" } };
            _mockService.Setup(s => s.GetAllAddressesAsync()).ReturnsAsync(addresses);

            // Act
            var result = await _controller.GetAddresses();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedAddresses = Assert.IsType<List<AddressDto>>(okResult.Value);
            Assert.Equal(2, returnedAddresses.Count);
        }

        [Fact]
        public async Task GetAddress_ReturnsOkResult_WithAddress_WhenFound()
        {
            // Arrange
            var address = new AddressDto { Id = 1, Building = "B1", AddressLine1 = "A1", City = "C1", State = "S1", PostalCode = "P1" };
            _mockService.Setup(s => s.GetAddressByIdAsync(1)).ReturnsAsync(address);

            // Act
            var result = await _controller.GetAddress(1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedAddress = Assert.IsType<AddressDto>(okResult.Value);
            Assert.Equal(1, returnedAddress.Id);
        }

        [Fact]
        public async Task GetAddress_ReturnsNotFoundResult_WhenNotFound()
        {
            // Arrange
            _mockService.Setup(s => s.GetAddressByIdAsync(1)).ReturnsAsync((AddressDto?)null);

            // Act
            var result = await _controller.GetAddress(1);

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task PostAddress_ReturnsCreatedAtActionResult()
        {
            // Arrange
            var createAddressDto = new CreateAddressDto { Building = "B1", AddressLine1 = "A1", City = "C1", State = "S1", PostalCode = "P1" };
            var createdAddress = new AddressDto { Id = 1, Building = "B1", AddressLine1 = "A1", City = "C1", State = "S1", PostalCode = "P1" };
            _mockService.Setup(s => s.CreateAddressAsync(It.IsAny<CreateAddressDto>())).ReturnsAsync(createdAddress);

            // Act
            var result = await _controller.PostAddress(createAddressDto);

            // Assert
            var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            Assert.Equal(nameof(AddressesController.GetAddress), createdAtActionResult.ActionName);
            Assert.Equal(1, ((AddressDto)createdAtActionResult.Value!).Id);
        }

        [Fact]
        public async Task PutAddress_ReturnsNoContentResult_WhenSuccessful()
        {
            // Arrange
            var updateAddressDto = new UpdateAddressDto { Building = "B1", AddressLine1 = "A1", City = "C1", State = "S1", PostalCode = "P1" };
            _mockService.Setup(s => s.UpdateAddressAsync(1, It.IsAny<UpdateAddressDto>())).ReturnsAsync(new AddressDto { Id = 1, Building = "B1", AddressLine1 = "A1", City = "C1", State = "S1", PostalCode = "P1" });

            // Act
            var result = await _controller.PutAddress(1, updateAddressDto);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task PutAddress_ReturnsNotFoundResult_WhenAddressNotFound()
        {
            // Arrange
            var updateAddressDto = new UpdateAddressDto { Building = "B1", AddressLine1 = "A1", City = "C1", State = "S1", PostalCode = "P1" };
            _mockService.Setup(s => s.UpdateAddressAsync(1, It.IsAny<UpdateAddressDto>())).ReturnsAsync((AddressDto?)null);

            // Act
            var result = await _controller.PutAddress(1, updateAddressDto);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task DeleteAddress_ReturnsNoContentResult_WhenSuccessful()
        {
            // Arrange
            _mockService.Setup(s => s.DeleteAddressAsync(1)).ReturnsAsync(true);

            // Act
            var result = await _controller.DeleteAddress(1);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task DeleteAddress_ReturnsNotFoundResult_WhenAddressNotFound()
        {
            // Arrange
            _mockService.Setup(s => s.DeleteAddressAsync(1)).ReturnsAsync(false);

            // Act
            var result = await _controller.DeleteAddress(1);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }
    }
}

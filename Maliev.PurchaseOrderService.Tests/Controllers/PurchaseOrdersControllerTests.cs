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

    public class PurchaseOrdersControllerTests
    {
        private readonly Mock<IPurchaseOrderService> _mockService;
        private readonly PurchaseOrdersController _controller;

        public PurchaseOrdersControllerTests()
        {
            _mockService = new Mock<IPurchaseOrderService>();
            _controller = new PurchaseOrdersController(_mockService.Object);
        }

        [Fact]
        public async Task GetPurchaseOrders_ReturnsOkResult_WithListOfPurchaseOrders()
        {
            // Arrange
            var purchaseOrders = new List<PurchaseOrderDto> { new PurchaseOrderDto { Id = 1 }, new PurchaseOrderDto { Id = 2 } };
            _mockService.Setup(s => s.GetAllPurchaseOrdersAsync()).ReturnsAsync(purchaseOrders);

            // Act
            var result = await _controller.GetPurchaseOrders();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedPurchaseOrders = Assert.IsType<List<PurchaseOrderDto>>(okResult.Value);
            Assert.Equal(2, returnedPurchaseOrders.Count);
        }

        [Fact]
        public async Task GetPurchaseOrder_ReturnsOkResult_WithPurchaseOrder_WhenFound()
        {
            // Arrange
            var purchaseOrder = new PurchaseOrderDto { Id = 1 };
            _mockService.Setup(s => s.GetPurchaseOrderByIdAsync(1)).ReturnsAsync(purchaseOrder);

            // Act
            var result = await _controller.GetPurchaseOrder(1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedPurchaseOrder = Assert.IsType<PurchaseOrderDto>(okResult.Value);
            Assert.Equal(1, returnedPurchaseOrder.Id);
        }

        [Fact]
        public async Task GetPurchaseOrder_ReturnsNotFoundResult_WhenNotFound()
        {
            // Arrange
            _mockService.Setup(s => s.GetPurchaseOrderByIdAsync(1)).ReturnsAsync((PurchaseOrderDto?)null);

            // Act
            var result = await _controller.GetPurchaseOrder(1);

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task PostPurchaseOrder_ReturnsCreatedAtActionResult()
        {
            // Arrange
            var createPurchaseOrderDto = new CreatePurchaseOrderDto();
            var createdPurchaseOrder = new PurchaseOrderDto { Id = 1 };
            _mockService.Setup(s => s.CreatePurchaseOrderAsync(It.IsAny<CreatePurchaseOrderDto>())).ReturnsAsync(createdPurchaseOrder);

            // Act
            var result = await _controller.PostPurchaseOrder(createPurchaseOrderDto);

            // Assert
            var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            Assert.Equal(nameof(PurchaseOrdersController.GetPurchaseOrder), createdAtActionResult.ActionName);
            Assert.Equal(1, ((PurchaseOrderDto)createdAtActionResult.Value!).Id);
        }

        [Fact]
        public async Task PutPurchaseOrder_ReturnsNoContentResult_WhenSuccessful()
        {
            // Arrange
            var updatePurchaseOrderDto = new UpdatePurchaseOrderDto();
            _mockService.Setup(s => s.UpdatePurchaseOrderAsync(1, It.IsAny<UpdatePurchaseOrderDto>())).ReturnsAsync(new PurchaseOrderDto { Id = 1 });

            // Act
            var result = await _controller.PutPurchaseOrder(1, updatePurchaseOrderDto);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task PutPurchaseOrder_ReturnsNotFoundResult_WhenPurchaseOrderNotFound()
        {
            // Arrange
            var updatePurchaseOrderDto = new UpdatePurchaseOrderDto();
            _mockService.Setup(s => s.UpdatePurchaseOrderAsync(1, It.IsAny<UpdatePurchaseOrderDto>())).ReturnsAsync((PurchaseOrderDto?)null);

            // Act
            var result = await _controller.PutPurchaseOrder(1, updatePurchaseOrderDto);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task DeletePurchaseOrder_ReturnsNoContentResult_WhenSuccessful()
        {
            // Arrange
            _mockService.Setup(s => s.DeletePurchaseOrderAsync(1)).ReturnsAsync(true);

            // Act
            var result = await _controller.DeletePurchaseOrder(1);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task DeletePurchaseOrder_ReturnsNotFoundResult_WhenPurchaseOrderNotFound()
        {
            // Arrange
            _mockService.Setup(s => s.DeletePurchaseOrderAsync(1)).ReturnsAsync(false);

            // Act
            var result = await _controller.DeletePurchaseOrder(1);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }
    }
}

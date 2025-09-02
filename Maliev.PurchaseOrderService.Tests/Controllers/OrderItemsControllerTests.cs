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

    public class OrderItemsControllerTests
    {
        private readonly Mock<IPurchaseOrderService> _mockService;
        private readonly OrderItemsController _controller;

        public OrderItemsControllerTests()
        {
            _mockService = new Mock<IPurchaseOrderService>();
            _controller = new OrderItemsController(_mockService.Object);
        }

        [Fact]
        public async Task GetOrderItemsByPurchaseOrderId_ReturnsOkResult_WithListOfOrderItems()
        {
            // Arrange
            var orderItems = new List<OrderItemDto> { new OrderItemDto { Id = 1 }, new OrderItemDto { Id = 2 } };
            _mockService.Setup(s => s.GetOrderItemsByPurchaseOrderIdAsync(It.IsAny<int>())).ReturnsAsync(orderItems);

            // Act
            var result = await _controller.GetOrderItemsByPurchaseOrderId(1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedOrderItems = Assert.IsType<List<OrderItemDto>>(okResult.Value);
            Assert.Equal(2, returnedOrderItems.Count);
        }

        [Fact]
        public async Task GetOrderItem_ReturnsOkResult_WithOrderItem_WhenFound()
        {
            // Arrange
            var orderItem = new OrderItemDto { Id = 1 };
            _mockService.Setup(s => s.GetOrderItemByIdAsync(1)).ReturnsAsync(orderItem);

            // Act
            var result = await _controller.GetOrderItem(1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedOrderItem = Assert.IsType<OrderItemDto>(okResult.Value);
            Assert.Equal(1, returnedOrderItem.Id);
        }

        [Fact]
        public async Task GetOrderItem_ReturnsNotFoundResult_WhenNotFound()
        {
            // Arrange
            _mockService.Setup(s => s.GetOrderItemByIdAsync(1)).ReturnsAsync((OrderItemDto?)null);

            // Act
            var result = await _controller.GetOrderItem(1);

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task PostOrderItem_ReturnsCreatedAtActionResult()
        {
            // Arrange
            var createOrderItemDto = new CreateOrderItemDto();
            var createdOrderItem = new OrderItemDto { Id = 1 };
            _mockService.Setup(s => s.CreateOrderItemAsync(It.IsAny<CreateOrderItemDto>())).ReturnsAsync(createdOrderItem);

            // Act
            var result = await _controller.PostOrderItem(createOrderItemDto);

            // Assert
            var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            Assert.Equal(nameof(OrderItemsController.GetOrderItem), createdAtActionResult.ActionName);
            Assert.Equal(1, ((OrderItemDto)createdAtActionResult.Value!).Id);
        }

        [Fact]
        public async Task PutOrderItem_ReturnsNoContentResult_WhenSuccessful()
        {
            // Arrange
            var updateOrderItemDto = new UpdateOrderItemDto();
            _mockService.Setup(s => s.UpdateOrderItemAsync(1, It.IsAny<UpdateOrderItemDto>())).ReturnsAsync(new OrderItemDto { Id = 1 });

            // Act
            var result = await _controller.PutOrderItem(1, updateOrderItemDto);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task PutOrderItem_ReturnsNotFoundResult_WhenOrderItemNotFound()
        {
            // Arrange
            var updateOrderItemDto = new UpdateOrderItemDto();
            _mockService.Setup(s => s.UpdateOrderItemAsync(1, It.IsAny<UpdateOrderItemDto>())).ReturnsAsync((OrderItemDto?)null);

            // Act
            var result = await _controller.PutOrderItem(1, updateOrderItemDto);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task DeleteOrderItem_ReturnsNoContentResult_WhenSuccessful()
        {
            // Arrange
            _mockService.Setup(s => s.DeleteOrderItemAsync(1)).ReturnsAsync(true);

            // Act
            var result = await _controller.DeleteOrderItem(1);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task DeleteOrderItem_ReturnsNotFoundResult_WhenOrderItemNotFound()
        {
            // Arrange
            _mockService.Setup(s => s.DeleteOrderItemAsync(1)).ReturnsAsync(false);

            // Act
            var result = await _controller.DeleteOrderItem(1);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }
    }
}

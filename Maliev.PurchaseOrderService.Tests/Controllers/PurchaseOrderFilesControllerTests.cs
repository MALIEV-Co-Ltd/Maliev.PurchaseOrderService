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

    public class PurchaseOrderFilesControllerTests
    {
        private readonly Mock<IPurchaseOrderService> _mockService;
        private readonly PurchaseOrderFilesController _controller;

        public PurchaseOrderFilesControllerTests()
        {
            _mockService = new Mock<IPurchaseOrderService>();
            _controller = new PurchaseOrderFilesController(_mockService.Object);
        }

        [Fact]
        public async Task GetPurchaseOrderFilesByPurchaseOrderId_ReturnsOkResult_WithListOfPurchaseOrderFiles()
        {
            // Arrange
            var purchaseOrderFiles = new List<PurchaseOrderFileDto> { new PurchaseOrderFileDto { Id = 1, Bucket = "B1", ObjectName = "O1" }, new PurchaseOrderFileDto { Id = 2, Bucket = "B2", ObjectName = "O2" } };
            _mockService.Setup(s => s.GetPurchaseOrderFilesByPurchaseOrderIdAsync(It.IsAny<int>())).ReturnsAsync(purchaseOrderFiles);

            // Act
            var result = await _controller.GetPurchaseOrderFilesByPurchaseOrderId(1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedPurchaseOrderFiles = Assert.IsType<List<PurchaseOrderFileDto>>(okResult.Value);
            Assert.Equal(2, returnedPurchaseOrderFiles.Count);
        }

        [Fact]
        public async Task GetPurchaseOrderFile_ReturnsOkResult_WithPurchaseOrderFile_WhenFound()
        {
            // Arrange
            var purchaseOrderFile = new PurchaseOrderFileDto { Id = 1, Bucket = "B1", ObjectName = "O1" };
            _mockService.Setup(s => s.GetPurchaseOrderFileByIdAsync(1)).ReturnsAsync(purchaseOrderFile);

            // Act
            var result = await _controller.GetPurchaseOrderFile(1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedPurchaseOrderFile = Assert.IsType<PurchaseOrderFileDto>(okResult.Value);
            Assert.Equal(1, returnedPurchaseOrderFile.Id);
        }

        [Fact]
        public async Task GetPurchaseOrderFile_ReturnsNotFoundResult_WhenNotFound()
        {
            // Arrange
            _mockService.Setup(s => s.GetPurchaseOrderFileByIdAsync(1)).ReturnsAsync((PurchaseOrderFileDto?)null);

            // Act
            var result = await _controller.GetPurchaseOrderFile(1);

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task PostPurchaseOrderFile_ReturnsCreatedAtActionResult()
        {
            // Arrange
            var createPurchaseOrderFileDto = new CreatePurchaseOrderFileDto { Bucket = "B1", ObjectName = "O1" };
            var createdPurchaseOrderFile = new PurchaseOrderFileDto { Id = 1, Bucket = "B1", ObjectName = "O1" };
            _mockService.Setup(s => s.CreatePurchaseOrderFileAsync(It.IsAny<CreatePurchaseOrderFileDto>())).ReturnsAsync(createdPurchaseOrderFile);

            // Act
            var result = await _controller.PostPurchaseOrderFile(createPurchaseOrderFileDto);

            // Assert
            var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            Assert.Equal(nameof(PurchaseOrderFilesController.GetPurchaseOrderFile), createdAtActionResult.ActionName);
            Assert.Equal(1, ((PurchaseOrderFileDto)createdAtActionResult.Value!).Id);
        }

        [Fact]
        public async Task PutPurchaseOrderFile_ReturnsNoContentResult_WhenSuccessful()
        {
            // Arrange
            var updatePurchaseOrderFileDto = new UpdatePurchaseOrderFileDto { Bucket = "B1", ObjectName = "O1" };
            _mockService.Setup(s => s.UpdatePurchaseOrderFileAsync(1, It.IsAny<UpdatePurchaseOrderFileDto>())).ReturnsAsync(new PurchaseOrderFileDto { Id = 1, Bucket = "B1", ObjectName = "O1" });

            // Act
            var result = await _controller.PutPurchaseOrderFile(1, updatePurchaseOrderFileDto);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task PutPurchaseOrderFile_ReturnsNotFoundResult_WhenPurchaseOrderFileNotFound()
        {
            // Arrange
            var updatePurchaseOrderFileDto = new UpdatePurchaseOrderFileDto { Bucket = "B1", ObjectName = "O1" };
            _mockService.Setup(s => s.UpdatePurchaseOrderFileAsync(1, It.IsAny<UpdatePurchaseOrderFileDto>())).ReturnsAsync((PurchaseOrderFileDto?)null);

            // Act
            var result = await _controller.PutPurchaseOrderFile(1, updatePurchaseOrderFileDto);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task DeletePurchaseOrderFile_ReturnsNoContentResult_WhenSuccessful()
        {
            // Arrange
            _mockService.Setup(s => s.DeletePurchaseOrderFileAsync(1)).ReturnsAsync(true);

            // Act
            var result = await _controller.DeletePurchaseOrderFile(1);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task DeletePurchaseOrderFile_ReturnsNotFoundResult_WhenPurchaseOrderFileNotFound()
        {
            // Arrange
            _mockService.Setup(s => s.DeletePurchaseOrderFileAsync(1)).ReturnsAsync(false);

            // Act
            var result = await _controller.DeletePurchaseOrderFile(1);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }
    }
}

using Maliev.PurchaseOrderService.Api.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Maliev.PurchaseOrderService.Tests.Unit;

public class UserPermissionServiceTests
{
    private readonly Mock<IMemoryCache> _cacheMock;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<ILogger<UserPermissionService>> _loggerMock;
    private readonly UserPermissionService _service;

    public UserPermissionServiceTests()
    {
        _cacheMock = new Mock<IMemoryCache>();
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _loggerMock = new Mock<ILogger<UserPermissionService>>();
        _service = new UserPermissionService(_cacheMock.Object, _httpClientFactoryMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetUserPermissionsAsync_ShouldReturnFromCache_IfPresent()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var expectedPermissions = new List<string> { "p1", "p2" };
        object? cacheEntry = expectedPermissions;

        _cacheMock.Setup(m => m.TryGetValue(It.IsAny<object>(), out cacheEntry))
            .Returns(true);

        // Act
        var result = await _service.GetUserPermissionsAsync(userId);

        // Assert
        Assert.Equal(expectedPermissions, result);
        _cacheMock.Verify(m => m.TryGetValue(It.Is<string>(s => s.Contains(userId)), out It.Ref<object?>.IsAny), Times.Once);
    }

    [Fact]
    public async Task GetUserPermissionsAsync_ShouldFetchAndCache_IfNotPresent()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var expectedPermissions = new List<string> { "p1" };
        object? cacheEntry = null;

        _cacheMock.Setup(m => m.TryGetValue(It.IsAny<object>(), out cacheEntry))
            .Returns(false);

        var cacheEntryMock = new Mock<ICacheEntry>();
        _cacheMock.Setup(m => m.CreateEntry(It.IsAny<object>()))
            .Returns(cacheEntryMock.Object);

        // Act
        // Note: Actual fetching from IAM will be mocked or handled in service.
        // For now, testing the caching logic.
        var result = await _service.GetUserPermissionsAsync(userId);

        // Assert
        _cacheMock.Verify(m => m.CreateEntry(It.Is<string>(s => s.Contains(userId))), Times.Once);
    }
}

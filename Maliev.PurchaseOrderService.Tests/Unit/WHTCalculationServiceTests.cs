using Maliev.PurchaseOrderService.Api.Services;
using Xunit;

namespace Maliev.PurchaseOrderService.Tests.Unit;

public class WHTCalculationServiceTests
{
    private readonly WHTCalculationService _service;

    public WHTCalculationServiceTests()
    {
        _service = new WHTCalculationService();
    }

    [Theory]
    [InlineData("100", "3", "3")]
    [InlineData("100", null, "0")]
    [InlineData("100", "0", "0")]
    [InlineData("100", "-1", "0")]
    [InlineData("105.50", "3", "3.17")] // 105.50 * 0.03 = 3.165 -> 3.17
    [InlineData("105.49", "3", "3.16")] // 105.49 * 0.03 = 3.1647 -> 3.16
    public void CalculateWHT_ShouldReturnCorrectAmount(string subtotalStr, string? rateStr, string expectedStr)
    {
        // Arrange
        decimal subtotal = decimal.Parse(subtotalStr);
        decimal? rate = rateStr != null ? decimal.Parse(rateStr) : null;
        decimal expected = decimal.Parse(expectedStr);

        // Act
        var result = _service.CalculateWHT(subtotal, rate);

        // Assert
        Assert.Equal(expected, result);
    }
}

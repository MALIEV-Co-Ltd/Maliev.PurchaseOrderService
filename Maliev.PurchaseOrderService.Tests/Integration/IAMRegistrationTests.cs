using Maliev.PurchaseOrderService.Api.Services;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using Xunit;
using System.Net;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Maliev.MessagingContracts.Generated;

namespace Maliev.PurchaseOrderService.Tests.Integration;

public class IAMRegistrationTests : IntegrationTestBase
{
    [Fact]
    public async Task Service_ShouldAttemptToRegisterPermissionsWithIAM_OnStartup()
    {
        // Arrange - Wait for background registration service to run
        await Task.Delay(5000);

        // Check HTTP registration (legacy/fallback)
        var allRequests = IAMServiceMock.LogEntries;
        var registrationRequests = allRequests.Where(entry =>
            entry.RequestMessage.Method == "POST" &&
            entry.RequestMessage.Path.Contains("/permissions/register")
        ).ToList();

        // Check RabbitMQ registration (preferred)
        var harness = Factory.Services.GetRequiredService<ITestHarness>();
        var publishedToBus = await harness.Published.Any<PermissionRegistrationRequest>();

        // Assert - At least one method should have been used
        Assert.True(registrationRequests.Any() || publishedToBus,
            "Service should have attempted to register permissions via either HTTP or RabbitMQ.");
    }
}

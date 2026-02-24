using Maliev.PurchaseOrderService.Api.Services;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using Xunit;
using System.Net;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Maliev.MessagingContracts.Contracts.Iam;
using Maliev.MessagingContracts.Generated;

namespace Maliev.PurchaseOrderService.Tests.Integration;

public class IAMRegistrationTests : IntegrationTestBase
{
    [Fact]
    public async Task Service_ShouldAttemptToRegisterPermissionsWithIAM_OnStartup()
    {
        // Get the test harness
        var harness = Factory.Services.GetTestHarness();

        // The BackgroundIAMRegistrationService waits 2 seconds before attempting registration
        // We allow some time for the service to start and publish the message
        // The harness collects published messages

        // Wait until the message is published or timeout (default timeout is usually sufficient, but we can rely on Any to check)
        // Since the service starts in background, we might need to wait for the condition.
        // TestHarness doesn't have a "WaitUntil" method exposed directly on Published collection in this version usually,
        // but we can loop or just wait a fixed delay like the original test did, then check.

        await Task.Delay(4000); // Wait for BackgroundService (2s delay + execution time)

        // Assert - Check for published PermissionRegistrationRequest
        var published = await harness.Published.Any<PermissionRegistrationRequest>();

        Assert.True(published, "PermissionRegistrationRequest should have been published to MassTransit");

        // Verify the content if possible
        var messages = harness.Published.Select<PermissionRegistrationRequest>().ToList();
        Assert.NotEmpty(messages);
        Assert.Equal("purchase-order", messages.First().Context.Message.ServiceName);
    }
}

using Maliev.PurchaseOrderService.Api.Services;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using Xunit;
using System.Net;

namespace Maliev.PurchaseOrderService.Tests.Integration;

public class IAMRegistrationTests : IntegrationTestBase
{
    [Fact]
    public async Task Service_ShouldAttemptToRegisterPermissionsWithIAM_OnStartup()
    {
        // Arrange - Configure WireMock stub BEFORE service starts
        // This test runs after the factory is initialized, so we need to check logs
        // The BackgroundIAMRegistrationService waits 2 seconds before attempting registration
        await Task.Delay(3000);

        // Assert - Check for ANY POST request to the permissions/register endpoint
        // The request might have received 404 since we didn't stub it, but it should be logged
        var allRequests = IAMServiceMock.LogEntries;
        var registrationRequests = allRequests.Where(entry =>
            entry.RequestMessage.Method == "POST" &&
            entry.RequestMessage.Path.Contains("/permissions/register")
        ).ToList();

        // If no requests found at all, service might be disabled or not starting
        Assert.NotEmpty(registrationRequests);

        // Verify it's the correct path
        Assert.Contains(registrationRequests, r => r.RequestMessage.Path.EndsWith("/iam/v1/permissions/register"));
    }
}
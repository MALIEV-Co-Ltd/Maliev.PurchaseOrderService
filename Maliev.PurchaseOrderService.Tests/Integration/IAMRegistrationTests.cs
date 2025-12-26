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
        // Arrange
        // Background registration has a short delay
        await Task.Delay(3000); 
        
        // Assert
        var requests = IAMServiceMock.FindLogEntries(
            Request.Create().WithPath("/iam/v1/permissions/register").UsingPost()
        );

        // It might have failed with 404 because we didn't setup the mock early enough,
        // but the fact that it's in the log means it tried.
        Assert.NotEmpty(requests);
    }
}
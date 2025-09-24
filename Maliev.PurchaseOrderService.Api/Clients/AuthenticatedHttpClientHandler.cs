using System.Net.Http.Headers;

namespace Maliev.PurchaseOrderService.Api.Clients;

/// <summary>
/// HTTP client handler that adds JWT authentication to outbound requests
/// </summary>
public class AuthenticatedHttpClientHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuthenticatedHttpClientHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        InnerHandler = new HttpClientHandler();
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Get the authorization header from the current HTTP context
        var authHeader = _httpContextAccessor.HttpContext?.Request.Headers.Authorization.FirstOrDefault();

        // If we have an authorization header, forward it to the external service
        if (!string.IsNullOrEmpty(authHeader))
        {
            request.Headers.Authorization = AuthenticationHeaderValue.Parse(authHeader);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
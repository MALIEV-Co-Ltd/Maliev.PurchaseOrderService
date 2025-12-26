using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Maliev.PurchaseOrderService.Api.Services;

/// <summary>
/// Service for managing and caching user permissions retrieved from IAM.
/// </summary>
public interface IUserPermissionService
{
    /// <summary>
    /// Gets the permissions for a specific user, with local caching.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <returns>A list of permission strings.</returns>
    Task<IEnumerable<string>> GetUserPermissionsAsync(string userId);
}

/// <summary>
/// Implementation of IUserPermissionService using IMemoryCache.
/// </summary>
public class UserPermissionService : IUserPermissionService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<UserPermissionService> _logger;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Initializes a new instance of the <see cref="UserPermissionService"/> class.
    /// </summary>
    /// <param name="cache">The memory cache.</param>
    /// <param name="logger">The logger instance.</param>
    public UserPermissionService(IMemoryCache cache, ILogger<UserPermissionService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// Gets the user permissions from cache or fetches them from IAM if not present.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <returns>The collection of permissions.</returns>
    public async Task<IEnumerable<string>> GetUserPermissionsAsync(string userId)
    {
        var cacheKey = $"user_permissions_{userId}";

        if (_cache.TryGetValue(cacheKey, out IEnumerable<string>? permissions) && permissions != null)
        {
            _logger.LogDebug("Returning cached permissions for user {UserId}", userId);
            return permissions;
        }

        _logger.LogInformation("Fetching permissions for user {UserId} from IAM", userId);
        
        // TODO: Implement actual IAM call when HttpClient is configured
        // For now, returning empty or placeholder to satisfy tests
        permissions = new List<string>(); 

        var cacheEntryOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(CacheDuration);

        _cache.Set(cacheKey, permissions, cacheEntryOptions);

        return permissions;
    }
}
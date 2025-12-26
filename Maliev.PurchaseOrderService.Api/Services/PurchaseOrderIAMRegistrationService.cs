using Maliev.Aspire.ServiceDefaults.IAM;
using RoleRegistration = Maliev.Aspire.ServiceDefaults.IAM.RoleRegistration;

namespace Maliev.PurchaseOrderService.Api.Services;

/// <summary>
/// Service that handles registration of purchase order permissions and roles with the central IAM service on startup.
/// </summary>
public class PurchaseOrderIAMRegistrationService : IAMRegistrationService
{
    private readonly ILogger<PurchaseOrderIAMRegistrationService> _logger;

    /// <summary>
    /// Initializes a new instance of the PurchaseOrderIAMRegistrationService class.
    /// </summary>
    public PurchaseOrderIAMRegistrationService(
        IHttpClientFactory httpClientFactory,
        ILogger<PurchaseOrderIAMRegistrationService> logger)
        : base(httpClientFactory, logger, "purchase-order")
    {
        _logger = logger;
    }

    /// <summary>
    /// Gets the list of permissions to register with IAM.
    /// </summary>
    protected override IEnumerable<PermissionRegistration> GetPermissions()
    {
        return PurchaseOrderPermissions.All.Select(p => new PermissionRegistration
        {
            PermissionId = p,
            Description = $"Permission: {p}"
        });
    }

    /// <summary>
    /// Gets the list of predefined roles to register with IAM.
    /// </summary>
    protected override IEnumerable<RoleRegistration> GetPredefinedRoles()
    {
        return PurchaseOrderPredefinedRoles.All.Select(r => new RoleRegistration
        {
            RoleId = r.RoleId,
            Description = r.Description,
            PermissionIds = r.Permissions.ToList()
        });
    }
}

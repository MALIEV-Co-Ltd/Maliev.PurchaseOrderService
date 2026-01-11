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
    /// Initializes a new instance of the <see cref="PurchaseOrderIAMRegistrationService"/> class.
    /// </summary>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="logger">Logger instance.</param>
    public PurchaseOrderIAMRegistrationService(
        IConfiguration configuration,
        ILogger<PurchaseOrderIAMRegistrationService> logger)
        : base(configuration, logger, "purchase-order")
    {
        _logger = logger;
    }

    /// <summary>
    /// Gets the list of permissions to register with IAM.
    /// </summary>
    protected override IEnumerable<PermissionRegistration> GetPermissions()
    {
        return PurchaseOrderPermissions.AllWithDescriptions.Select(p => new PermissionRegistration
        {
            PermissionId = p.Key,
            Description = p.Value
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
            PermissionIds = r.Permissions.ToList(),
            IsCustom = false
        });
    }
}

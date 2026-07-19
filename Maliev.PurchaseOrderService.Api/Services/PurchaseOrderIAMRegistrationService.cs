using Maliev.Aspire.ServiceDefaults.IAM;
using Maliev.PurchaseOrderService.Domain.Constants;

namespace Maliev.PurchaseOrderService.Api.Services;

/// <summary>
/// Registers PurchaseOrderService permissions and roles with the central IAM system on startup.
/// </summary>
public class PurchaseOrderIAMRegistrationService : IAMRegistrationService
{
    private const string ServiceNameValue = "purchase-order";

    /// <summary>
    /// Initializes a new instance of the <see cref="PurchaseOrderIAMRegistrationService"/> class.
    /// </summary>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="logger">The logger.</param>
    public PurchaseOrderIAMRegistrationService(
        IConfiguration configuration,
        ILogger<PurchaseOrderIAMRegistrationService> logger)
        : base(configuration, logger, ServiceNameValue)
    {
    }

    /// <inheritdoc/>
    protected override IEnumerable<PermissionRegistration> GetPermissions()
    {
        return PurchaseOrderPermissions.AllWithDescriptions.Select(p => new PermissionRegistration
        {
            PermissionId = p.Key,
            Description = p.Value
        });
    }

    /// <inheritdoc/>
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

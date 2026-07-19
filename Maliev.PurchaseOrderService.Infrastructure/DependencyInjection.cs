using Maliev.PurchaseOrderService.Application.Interfaces;
using Maliev.PurchaseOrderService.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Maliev.PurchaseOrderService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<PurchaseOrderContext>();
        services.AddScoped<IPurchaseOrderDbContext>(sp => sp.GetRequiredService<PurchaseOrderContext>());
        return services;
    }
}

using Microsoft.Extensions.DependencyInjection;

namespace Maliev.PurchaseOrderService.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        return services;
    }
}

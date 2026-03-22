using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using OrderManagement.Application.Orders;

namespace OrderManagement.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IOrderService, OrderService>();
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        return services;
    }
}
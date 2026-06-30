using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrderManagement.Application.Abstractions.Caching;
using OrderManagement.Application.Abstractions.Persistence;
using OrderManagement.Infrastructure.Caching;
using OrderManagement.Infrastructure.Persistence;

namespace OrderManagement.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IAppDbContext>(provider => provider.GetRequiredService<AppDbContext>());

        var cachingSection = configuration.GetSection(CachingOptions.SectionName);
        var sizeLimit = long.TryParse(
            cachingSection[nameof(CachingOptions.SizeLimit)],
            out var configuredSizeLimit)
            ? Math.Max(1, configuredSizeLimit)
            : 10_000;
        services.Configure<CachingOptions>(options =>
        {
            if (bool.TryParse(cachingSection[nameof(CachingOptions.Enabled)], out var enabled))
                options.Enabled = enabled;
            if (int.TryParse(cachingSection[nameof(CachingOptions.OrderDetailSeconds)], out var detailSeconds))
                options.OrderDetailSeconds = detailSeconds;
            if (int.TryParse(cachingSection[nameof(CachingOptions.OrderListSeconds)], out var listSeconds))
                options.OrderListSeconds = listSeconds;
            options.SizeLimit = sizeLimit;
        });
        services.AddMemoryCache(options =>
            options.SizeLimit = sizeLimit);
        services.AddSingleton<ICacheService, MemoryCacheService>();

        return services;
    }
}

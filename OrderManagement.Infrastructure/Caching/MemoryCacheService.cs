using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using OrderManagement.Application.Abstractions.Caching;

namespace OrderManagement.Infrastructure.Caching;

public sealed class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _cache;
    private readonly CachingOptions _options;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _keyLocks = new();
    private readonly ConcurrentDictionary<string, long> _versions = new();

    public MemoryCacheService(IMemoryCache cache, IOptions<CachingOptions> options)
    {
        _cache = cache;
        _options = options.Value;
    }

    public async Task<T?> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T?>> factory,
        CachePolicy policy,
        CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            return await factory(cancellationToken);
        }

        if (_cache.TryGetValue(key, out T? cached))
        {
            return cached;
        }

        var keyLock = _keyLocks.GetOrAdd(key, static _ => new SemaphoreSlim(1, 1));
        await keyLock.WaitAsync(cancellationToken);

        try
        {
            if (_cache.TryGetValue(key, out cached))
            {
                return cached;
            }

            var result = await factory(cancellationToken);

            if (result is not null)
            {
                _cache.Set(key, result, GetExpiration(policy));
            }

            return result;
        }
        finally
        {
            keyLock.Release();
        }
    }

    public long GetVersion(string key) =>
        _versions.GetOrAdd(key, 0);

    public void IncrementVersion(string key) =>
        _versions.AddOrUpdate(key, 1, static (_, current) => checked(current + 1));

    public void Remove(string key) =>
        _cache.Remove(key);

    private TimeSpan GetExpiration(CachePolicy policy) => policy switch
    {
        CachePolicy.OrderDetail => TimeSpan.FromSeconds(Math.Max(1, _options.OrderDetailSeconds)),
        CachePolicy.OrderList => TimeSpan.FromSeconds(Math.Max(1, _options.OrderListSeconds)),
        _ => throw new ArgumentOutOfRangeException(nameof(policy), policy, null)
    };
}

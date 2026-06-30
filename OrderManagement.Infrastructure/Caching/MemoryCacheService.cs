using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using OrderManagement.Application.Abstractions.Caching;

namespace OrderManagement.Infrastructure.Caching;

public sealed class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _cache;
    private readonly CachingOptions _options;
    private readonly ConcurrentDictionary<string, Lazy<Task<object?>>> _inflight = new();
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

        var lazy = _inflight.GetOrAdd(
            key,
            _ => new Lazy<Task<object?>>(
                async () => await factory(cancellationToken),
                LazyThreadSafetyMode.ExecutionAndPublication));

        try
        {
            var result = (T?)await lazy.Value.WaitAsync(cancellationToken);

            if (result is not null)
            {
                _cache.Set(key, result, GetExpiration(policy));
            }

            return result;
        }
        finally
        {
            _inflight.TryRemove(new KeyValuePair<string, Lazy<Task<object?>>>(key, lazy));
        }
    }

    public long GetVersion(string key) =>
        _versions.GetOrAdd(key, 0);

    public void IncrementVersion(string key) =>
        _versions.AddOrUpdate(key, 1, static (_, current) => checked(current + 1));

    public void Remove(string key)
    {
        if (_options.Enabled)
        {
            _cache.Remove(key);
        }
    }

    private TimeSpan GetExpiration(CachePolicy policy) => policy switch
    {
        CachePolicy.OrderDetail => TimeSpan.FromSeconds(Math.Max(1, _options.OrderDetailSeconds)),
        CachePolicy.OrderList => TimeSpan.FromSeconds(Math.Max(1, _options.OrderListSeconds)),
        _ => throw new ArgumentOutOfRangeException(nameof(policy), policy, null)
    };
}

namespace OrderManagement.Application.Abstractions.Caching;

public interface ICacheService
{
    Task<T?> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T?>> factory,
        CachePolicy policy,
        CancellationToken cancellationToken);

    long GetVersion(string key);

    void IncrementVersion(string key);

    void Remove(string key);
}

public enum CachePolicy
{
    OrderDetail,
    OrderList
}
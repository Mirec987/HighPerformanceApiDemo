using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using OrderManagement.Application.Abstractions.Caching;
using OrderManagement.Infrastructure.Caching;

namespace IntegrationTests.Infrastructure.Caching;

public class MemoryCacheServiceTests
{
    [Fact]
    public void Remove_Should_Clear_Entry_When_Caching_Is_Disabled()
    {
        using var memoryCache = new MemoryCache(Options.Create(new MemoryCacheOptions()));
        memoryCache.Set("stale-key", "stale-value");
        var service = new MemoryCacheService(
            memoryCache,
            Options.Create(new CachingOptions { Enabled = false }));

        service.Remove("stale-key");

        memoryCache.TryGetValue("stale-key", out _).Should().BeFalse();
    }

    [Fact]
    public async Task Cancelling_First_Request_Should_Not_Cancel_Waiting_Request()
    {
        using var memoryCache = new MemoryCache(Options.Create(new MemoryCacheOptions()));
        var service = new MemoryCacheService(
            memoryCache,
            Options.Create(new CachingOptions { Enabled = true }));
        using var firstCancellation = new CancellationTokenSource();
        var firstFactoryStarted = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);

        var firstRequest = service.GetOrCreateAsync<string>(
            "shared-key",
            async token =>
            {
                firstFactoryStarted.SetResult();
                await Task.Delay(Timeout.InfiniteTimeSpan, token);
                return "first";
            },
            CachePolicy.OrderDetail,
            firstCancellation.Token);

        await firstFactoryStarted.Task;

        var secondRequest = service.GetOrCreateAsync<string>(
            "shared-key",
            _ => Task.FromResult<string?>("second"),
            CachePolicy.OrderDetail,
            CancellationToken.None);

        firstCancellation.Cancel();

        Func<Task> waitForFirstRequest = async () => await firstRequest;
        await waitForFirstRequest.Should().ThrowAsync<OperationCanceledException>();
        (await secondRequest).Should().Be("second");
    }
}

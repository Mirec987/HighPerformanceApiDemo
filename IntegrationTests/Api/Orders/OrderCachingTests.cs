using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using IntegrationTests.Infrastructure.Factories;
using Microsoft.Extensions.DependencyInjection;
using OrderManagement.Application.Orders.DTOs;
using OrderManagement.Application.Services.Interfaces;

namespace IntegrationTests.Api.Orders;

public class OrderCachingTests : IntegrationTestBase<TestWebApplicationFactory>
{
    public OrderCachingTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetAll_Should_Not_Mutate_Paging_Request()
    {
        using var scope = Factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IOrderService>();
        var request = new GetOrdersRequest
        {
            CustomerId = Guid.NewGuid(),
            Page = 0,
            PageSize = 500
        };

        var result = await service.GetAllAsync(request, CancellationToken.None);

        request.Page.Should().Be(0);
        request.PageSize.Should().Be(500);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(100);
    }

    [Fact]
    public async Task Updating_Order_Should_Invalidate_Cached_Detail()
    {
        var order = await CreateOrderAsync();

        var cachedDetail = await Client.GetFromJsonAsync<OrderDetailResponse>($"/api/orders/{order.Id}");
        cachedDetail.Should().NotBeNull();
        cachedDetail!.Status.Should().Be("Draft");

        var updateResponse = await Client.PatchAsJsonAsync(
            $"/api/orders/{order.Id}/status",
            new UpdateOrderRequest
            {
                Status = "Paid",
                RowVersion = cachedDetail.RowVersion
            });

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var refreshedDetail = await Client.GetFromJsonAsync<OrderDetailResponse>($"/api/orders/{order.Id}");
        refreshedDetail.Should().NotBeNull();
        refreshedDetail!.Status.Should().Be("Paid");
        refreshedDetail.RowVersion.Should().NotBe(cachedDetail.RowVersion);
    }

    [Fact]
    public async Task Creating_Order_Should_Advance_List_Cache_Version()
    {
        await CreateOrderAsync();

        var cachedList = await Client.GetFromJsonAsync<PagedResponse<OrderResponse>>(
            "/api/orders?page=1&pageSize=100");
        cachedList.Should().NotBeNull();

        await CreateOrderAsync();

        var refreshedList = await Client.GetFromJsonAsync<PagedResponse<OrderResponse>>(
            "/api/orders?page=1&pageSize=100");
        refreshedList.Should().NotBeNull();
        refreshedList!.TotalCount.Should().Be(cachedList!.TotalCount + 1);
    }

    private async Task<OrderResponse> CreateOrderAsync()
    {
        var response = await Client.PostAsJsonAsync(
            "/api/orders",
            new CreateOrderRequest
            {
                CustomerId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Items =
                [
                    new CreateOrderItemRequest
                    {
                        ProductId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                        Quantity = 1
                    }
                ]
            });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<OrderResponse>())!;
    }
}

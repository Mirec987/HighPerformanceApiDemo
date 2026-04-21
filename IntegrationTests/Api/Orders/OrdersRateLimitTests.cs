using FluentAssertions;
using IntegrationTests.Infrastructure.Factories;
using OrderManagement.Contracts.Requests;
using System.Net;
using System.Net.Http.Json;

namespace IntegrationTests.Api.Orders;

public class OrdersRateLimitTests : IntegrationTestBase<RateLimitWebApplicationFactory>
{
    public OrdersRateLimitTests(RateLimitWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Create_Order_Should_Return_TooManyRequests_When_RateLimit_Is_Exceeded()
    {
        var request = new CreateOrderRequest
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
        };

        var firstResponse = await Client.PostAsJsonAsync("/api/orders", request);
        var secondResponse = await Client.PostAsJsonAsync("/api/orders", request);
        var thirdResponse = await Client.PostAsJsonAsync("/api/orders", request);

        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        thirdResponse.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);

        var body = await thirdResponse.Content.ReadAsStringAsync();
        body.Should().Contain("Too many requests");
    }
}
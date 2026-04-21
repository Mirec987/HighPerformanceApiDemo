using FluentAssertions;
using IntegrationTests.Infrastructure.Factories;
using OrderManagement.Contracts.Requests;
using OrderManagement.Contracts.Responses;
using System.Net;
using System.Net.Http.Json;

namespace IntegrationTests.Api.Orders;

public class OrdersTests : IntegrationTestBase<TestWebApplicationFactory>
{
    public OrdersTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Create_Order_Should_Return_Created()
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

        var response = await Client.PostAsJsonAsync("/api/orders", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<OrderResponse>();

        body.Should().NotBeNull();
        body!.OrderNumber.Should().NotBeNullOrWhiteSpace();
        body.TotalAmount.Should().Be(1200);
        body.Status.Should().Be("Draft");
    }

    [Fact]
    public async Task Get_Orders_Should_Return_Ok()
    {
        var createRequest = new CreateOrderRequest
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

        await Client.PostAsJsonAsync("/api/orders", createRequest);

        var response = await Client.GetAsync("/api/orders?page=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<PagedResponse<OrderResponse>>();

        body.Should().NotBeNull();
        body!.Items.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Update_Order_Status_With_Stale_RowVersion_Should_Return_Conflict()
    {
        var createRequest = new CreateOrderRequest
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

        var createResponse = await Client.PostAsJsonAsync("/api/orders", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createdOrder = await createResponse.Content.ReadFromJsonAsync<OrderResponse>();
        createdOrder.Should().NotBeNull();

        var detailResponse = await Client.GetAsync($"/api/orders/{createdOrder!.Id}");
        detailResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var detail = await detailResponse.Content.ReadFromJsonAsync<OrderDetailResponse>();
        detail.Should().NotBeNull();
        detail!.RowVersion.Should().NotBeNullOrWhiteSpace();

        var originalRowVersion = detail.RowVersion;

        var firstUpdateRequest = new UpdateOrderRequest
        {
            Status = "Paid",
            RowVersion = originalRowVersion
        };

        var firstUpdateResponse = await Client.PatchAsJsonAsync(
            $"/api/orders/{createdOrder.Id}/status",
            firstUpdateRequest);

        firstUpdateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var secondUpdateRequest = new UpdateOrderRequest
        {
            Status = "Shipped",
            RowVersion = originalRowVersion
        };

        var secondUpdateResponse = await Client.PatchAsJsonAsync(
            $"/api/orders/{createdOrder.Id}/status",
            secondUpdateRequest);

        secondUpdateResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var body = await secondUpdateResponse.Content.ReadAsStringAsync();
        body.Should().Contain("Concurrency conflict");
    }
}
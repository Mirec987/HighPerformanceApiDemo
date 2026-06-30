using System.Text.Json;
using BenchmarkDotNet.Attributes;
using FluentValidation;
using OrderManagement.Application.Orders.DTOs;
using OrderManagement.Application.Orders.Validators;

namespace Benchmarks;

[MemoryDiagnoser]
public class RequestBenchmarks
{
    private readonly CreateOrderRequest _request = new()
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

    private readonly IValidator<CreateOrderRequest> _validator = new CreateOrderRequestValidator();

    [Benchmark]
    public Task<FluentValidation.Results.ValidationResult> ValidateCreateOrder() =>
        _validator.ValidateAsync(_request);

    [Benchmark]
    public byte[] SerializeCreateOrder() =>
        JsonSerializer.SerializeToUtf8Bytes(_request);

    [Benchmark]
    public OrderResponse MapOrderResponse() => new()
    {
        Id = Guid.Empty,
        OrderNumber = "ORD-20260630-ABC123",
        TotalAmount = 1200m,
        Status = "Draft"
    };
}

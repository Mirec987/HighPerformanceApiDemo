using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OrderManagement.Application.Orders.DTOs;
using OrderManagement.Application.Services.Interfaces;

namespace OrderManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponse<OrderResponse>>> GetAll(
        [FromQuery] GetOrdersRequest request,
        CancellationToken ct)
    {
        var response = await _orderService.GetAllAsync(request, ct);
        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrderDetailResponse>> GetById(Guid id, CancellationToken ct)
    {
        var order = await _orderService.GetByIdAsync(id, ct);

        if (order is null)
        {
            return NotFound();
        }

        return Ok(order);
    }

    [EnableRateLimiting("write-policy")]
    [HttpPost]
    public async Task<ActionResult<OrderResponse>> Create(CreateOrderRequest request, CancellationToken ct)
    {
        var response = await _orderService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    [EnableRateLimiting("write-policy")]
    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<OrderResponse>> UpdateStatus(
    Guid id,
    UpdateOrderRequest request,
    CancellationToken ct)
    {
        var response = await _orderService.UpdateOrderAsync(id, request, ct);
        return Ok(response);
    }
}
using FluentValidation;
using OrderManagement.Contracts.Requests;

namespace OrderManagement.Application.Orders.Validators;

public class CreateOrderItemRequestValidator : AbstractValidator<CreateOrderItemRequest>
{
    public CreateOrderItemRequestValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty()
            .WithMessage("ProductId is required.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .WithMessage("Quantity must be greater than 0.");
    }
}
using FluentValidation;
using OrderManagement.Contracts.Requests;

namespace OrderManagement.Application.Orders.Validators;

public class CreateOrderRequestValidator : AbstractValidator<CreateOrderRequest>
{
    public CreateOrderRequestValidator()
    {
        RuleFor(x => x.CustomerId)
            .NotEmpty()
            .WithMessage("CustomerId is required.");

        RuleFor(x => x.Items)
            .NotNull()
            .WithMessage("Items are required.")
            .Must(x => x != null && x.Count > 0)
            .WithMessage("Order must contain at least one item.");

        RuleForEach(x => x.Items)
            .SetValidator(new CreateOrderItemRequestValidator());
    }
}
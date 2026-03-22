using FluentValidation;
using OrderManagement.Contracts.Requests;

namespace OrderManagement.Application.Orders.Validators;

public class UpdateOrderRequestValidator : AbstractValidator<UpdateOrderRequest>
{
    public UpdateOrderRequestValidator()
    {
        RuleFor(x => x.Status)
            .NotEmpty()
            .WithMessage("Status is required.");

        RuleFor(x => x.RowVersion)
            .NotEmpty()
            .WithMessage("RowVersion is required.");
    }
}
using FluentValidation;
using OrderManagement.Application.Orders.DTOs;

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
            .Must(BeValidBase64)
            .WithMessage("RowVersion must be a valid Base64 string.");
    }

    private bool BeValidBase64(string rowVersion)
    {
        return Convert.TryFromBase64String(rowVersion, new Span<byte>(new byte[rowVersion.Length]), out _);
    }
}
using FluentValidation;
using OrderManagement.Application.Orders.DTOs;
using OrderManagement.Domain.Enums;

namespace OrderManagement.Application.Orders.Validators;

public class UpdateOrderRequestValidator : AbstractValidator<UpdateOrderRequest>
{
    public UpdateOrderRequestValidator()
    {
        RuleFor(x => x.Status)
            .NotEmpty()
            .Must(BeValidStatus)
            .WithMessage("Status must be a valid order status.")
            .When(x => !string.IsNullOrWhiteSpace(x.Status));

        RuleFor(x => x.RowVersion)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Must(BeValidRowVersion)
            .WithMessage("RowVersion must be a valid 8-byte Base64 row version.");
    }

    private static bool BeValidStatus(string? status)
    {
        return Enum.TryParse<OrderStatus>(status, true, out var parsedStatus) &&
               Enum.IsDefined(parsedStatus);
    }

    private static bool BeValidRowVersion(string? rowVersion)
    {
        if (string.IsNullOrWhiteSpace(rowVersion))
        {
            return false;
        }

        Span<byte> bytes = stackalloc byte[8];
        return Convert.TryFromBase64String(rowVersion, bytes, out var bytesWritten) &&
               bytesWritten == bytes.Length;
    }
}
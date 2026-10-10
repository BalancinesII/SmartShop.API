using FluentValidation;

namespace SmartShop.Application.Payments.Commands.CreateCheckout;

public class CreateCheckoutValidator : AbstractValidator<CreateCheckoutCommand>
{
    public const int MaxQuantity = 10;

    public CreateCheckoutValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("Product id is required.");

        RuleFor(x => x.Quantity)
            .InclusiveBetween(1, MaxQuantity)
            .WithMessage($"Quantity must be between 1 and {MaxQuantity}.");
    }
}

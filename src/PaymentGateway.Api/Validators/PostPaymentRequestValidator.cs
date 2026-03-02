using FluentValidation;

using PaymentGateway.Api.Models.Requests;

namespace PaymentGateway.Api.Validators;

public class PostPaymentRequestValidator : AbstractValidator<PostPaymentRequest>
{
    private static readonly HashSet<string> AllowedCurrencies = new(StringComparer.OrdinalIgnoreCase) { "GBP", "USD", "EUR" };

    public PostPaymentRequestValidator()
    {
        RuleFor(x => x.CardNumber)
            .NotEmpty().WithMessage("Card number is required.")
            .Length(14, 19).WithMessage("Card number must be between 14 and 19 characters.")
            .Matches("^[0-9]+$").WithMessage("Card number must contain only numeric characters.");

        RuleFor(x => x.ExpiryMonth)
            .InclusiveBetween(1, 12).WithMessage("Expiry month must be between 1 and 12.");

        RuleFor(x => x.ExpiryYear)
            .GreaterThan(0).WithMessage("Expiry year is required.");

        RuleFor(x => x)
            .Must(IsExpiryInTheFuture)
            .WithMessage("Expiry date must be in the future.")
            .When(x => x.ExpiryMonth >= 1 && x.ExpiryMonth <= 12 && x.ExpiryYear > 0);

        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("Currency is required.")
            .Length(3).WithMessage("Currency must be 3 characters.")
            .Must(currency => AllowedCurrencies.Contains(currency))
            .WithMessage("Currency must be one of the supported ISO codes (e.g. GBP, USD, EUR).");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Amount must be a positive integer.");

        RuleFor(x => x.Cvv)
            .NotEmpty().WithMessage("CVV is required.")
            .Length(3, 4).WithMessage("CVV must be 3 or 4 characters.")
            .Matches("^[0-9]+$").WithMessage("CVV must contain only numeric characters.");
    }

    private static bool IsExpiryInTheFuture(PostPaymentRequest request)
    {
        var lastDayOfExpiry = new DateTime(request.ExpiryYear, request.ExpiryMonth, DateTime.DaysInMonth(request.ExpiryYear, request.ExpiryMonth));
        return lastDayOfExpiry >= DateTime.UtcNow.Date;
    }
}

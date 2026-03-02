using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Validators;

namespace PaymentGateway.Api.Tests.UnitTests;

public class PostPaymentRequestValidatorTests
{
    private readonly PostPaymentRequestValidator _sut = new();

    private static PostPaymentRequest ValidRequest() => new()
    {
        CardNumber = "1234567890123456",
        ExpiryMonth = 12,
        ExpiryYear = DateTime.UtcNow.Year + 1,
        Currency = "GBP",
        Amount = 100,
        Cvv = "123"
    };

    [Fact]
    public void Validate_WhenAllFieldsValid_ReturnsNoErrors()
    {
        // Arrange
        var request = ValidRequest();

        // Act
        var result = _sut.Validate(request);

        // Assert
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WhenCardNumberEmpty_ReturnsError(string? cardNumber)
    {
        // Arrange
        var request = ValidRequest();
        request.CardNumber = cardNumber ?? string.Empty;

        // Act
        var result = _sut.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(PostPaymentRequest.CardNumber));
    }

    [Theory]
    [InlineData("1234567890123")]        // 13
    [InlineData("12345678901234567890")] // 20
    public void Validate_WhenCardNumberWrongLength_ReturnsError(string cardNumber)
    {
        // Arrange
        var request = ValidRequest();
        request.CardNumber = cardNumber;

        // Act
        var result = _sut.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(PostPaymentRequest.CardNumber) && e.ErrorMessage.Contains("14 and 19"));
    }

    [Theory]
    [InlineData("12345678901234")]   // 14 - valid
    [InlineData("123456789012345")]  // 15
    [InlineData("1234567890123456")] // 16
    [InlineData("1234567890123456789")] // 19 - valid
    public void Validate_WhenCardNumberValidLength_PassesLengthRule(string cardNumber)
    {
        // Arrange
        var request = ValidRequest();
        request.CardNumber = cardNumber;

        // Act
        var result = _sut.Validate(request);

        // Assert
        Assert.DoesNotContain(result.Errors, e => e.PropertyName == nameof(PostPaymentRequest.CardNumber) && e.ErrorMessage.Contains("14 and 19"));
    }

    [Theory]
    [InlineData("12345678901234ab")]
    [InlineData("1234-5678-9012-3456")]
    [InlineData("12345678901234 ")]
    public void Validate_WhenCardNumberNotNumeric_ReturnsError(string cardNumber)
    {
        // Arrange
        var request = ValidRequest();
        request.CardNumber = cardNumber;

        // Act
        var result = _sut.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(PostPaymentRequest.CardNumber) && e.ErrorMessage.Contains("numeric"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public void Validate_WhenExpiryMonthOutOfRange_ReturnsError(int month)
    {
        // Arrange
        var request = ValidRequest();
        request.ExpiryMonth = month;

        // Act
        var result = _sut.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(PostPaymentRequest.ExpiryMonth));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(6)]
    [InlineData(12)]
    public void Validate_WhenExpiryMonthInRange_Passes(int month)
    {
        // Arrange
        var request = ValidRequest();
        request.ExpiryMonth = month;
        request.ExpiryYear = DateTime.UtcNow.Year + 1;

        // Act
        var result = _sut.Validate(request);

        // Assert
        Assert.DoesNotContain(result.Errors, e => e.PropertyName == nameof(PostPaymentRequest.ExpiryMonth));
    }

    [Fact]
    public void Validate_WhenExpiryInThePast_ReturnsError()
    {
        // Arrange
        var request = ValidRequest();
        request.ExpiryMonth = 1;
        request.ExpiryYear = DateTime.UtcNow.Year - 1;

        // Act
        var result = _sut.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("future"));
    }

    [Fact]
    public void Validate_WhenExpiryThisMonth_Passes()
    {
        var now = DateTime.UtcNow;
        var request = ValidRequest();
        request.ExpiryMonth = now.Month;
        request.ExpiryYear = now.Year;
        var result = _sut.Validate(request);
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("AB")]
    [InlineData("ABCD")]
    public void Validate_WhenCurrencyInvalid_ReturnsError(string? currency)
    {
        // Arrange
        var request = ValidRequest();
        request.Currency = currency ?? string.Empty;

        // Act
        var result = _sut.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(PostPaymentRequest.Currency));
    }

    [Theory]
    [InlineData("GBP")]
    [InlineData("USD")]
    [InlineData("EUR")]
    [InlineData("gbp")]
    public void Validate_WhenCurrencyAllowed_Passes(string currency)
    {
        // Arrange
        var request = ValidRequest();
        request.Currency = currency;

        // Act
        var result = _sut.Validate(request);

        // Assert
        Assert.DoesNotContain(result.Errors, e => e.PropertyName == nameof(PostPaymentRequest.Currency) && e.ErrorMessage.Contains("supported"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_WhenAmountNotPositive_ReturnsError(int amount)
    {
        var request = ValidRequest();
        request.Amount = amount;
        var result = _sut.Validate(request);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(PostPaymentRequest.Amount));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("12")]
    [InlineData("12345")]
    public void Validate_WhenCvvInvalidLength_ReturnsError(string? cvv)
    {
        // Arrange
        var request = ValidRequest();
        request.Cvv = cvv ?? string.Empty;

        // Act
        var result = _sut.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(PostPaymentRequest.Cvv));
    }

    [Theory]
    [InlineData("123")]
    [InlineData("1234")]
    public void Validate_WhenCvvValidLength_Passes(string cvv)
    {
        // Arrange
        var request = ValidRequest();
        request.Cvv = cvv;

        // Act
        var result = _sut.Validate(request);

        // Assert
        Assert.DoesNotContain(result.Errors, e => e.PropertyName == nameof(PostPaymentRequest.Cvv) && e.ErrorMessage.Contains("3 or 4"));
    }

    [Theory]
    [InlineData("12a")]
    [InlineData("1234a")]
    public void Validate_WhenCvvNotNumeric_ReturnsError(string cvv)
    {
        // Arrange
        var request = ValidRequest();
        request.Cvv = cvv;

        // Act
        var result = _sut.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(PostPaymentRequest.Cvv) && e.ErrorMessage.Contains("numeric"));
    }
}

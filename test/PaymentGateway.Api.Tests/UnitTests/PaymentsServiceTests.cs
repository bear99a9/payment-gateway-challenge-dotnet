using System.Net;

using AutoFixture;

using Microsoft.Extensions.Logging;

using NSubstitute;

using OneOf;

using PaymentGateway.Api.ApiRepository;
using PaymentGateway.Api.DataRepository;
using PaymentGateway.Api.Enums;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Services;

namespace PaymentGateway.Api.Tests.UnitTests;

public class PaymentsServiceTests
{
    private readonly Fixture _fixture = new();
    private readonly IPaymentsRepository _paymentRepository;
    private readonly ILogger<PaymentsService> _logger;
    private readonly IBankSimulatorApi _bankSimulatorApi;
    private readonly PaymentsService _sut;

    public PaymentsServiceTests()
    {
        _paymentRepository = Substitute.For<IPaymentsRepository>();
        _logger = Substitute.For<ILogger<PaymentsService>>();
        _bankSimulatorApi = Substitute.For<IBankSimulatorApi>();
        _sut = new PaymentsService(_logger, _paymentRepository, _bankSimulatorApi);
    }


    [Fact]
    public void GetPayment_WhenPaymentExists_ReturnsPayment()
    {
        // Arrange
        var payment = _fixture.Create<PostPaymentResponse>();

        _paymentRepository.Get(payment.Id).Returns(payment);

        // Act
        var result = _sut.GetPayment(payment.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(payment.Id, result!.Id);
        Assert.Equal(payment.Status, result.Status);
        Assert.Equal(payment.CardNumberLastFour, result.CardNumberLastFour);
        Assert.Equal(payment.ExpiryMonth, result.ExpiryMonth);
        Assert.Equal(payment.ExpiryYear, result.ExpiryYear);
        Assert.Equal(payment.Currency, result.Currency);
        Assert.Equal(payment.Amount, result.Amount);
    }

    [Fact]
    public void GetPayment_WhenPaymentDoesNotExist_ReturnsNotFound(){
        // Arrange
        var paymentId = Guid.NewGuid();
        _paymentRepository.Get(paymentId).Returns((PostPaymentResponse?)null);

        // Act
        var result = _sut.GetPayment(paymentId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ProcessPayment_WhenBankAuthorizes_ReturnsPostPaymentResponseWithAuthorizedAndStoresInRepository()
    {
        // Arrange
        var request = new PostPaymentRequest
        {
            CardNumber = "1234567890123456",
            ExpiryMonth = 6,
            ExpiryYear = 2028,
            Currency = "GBP",
            Amount = 1000,
            Cvv = "123"
        };
        var bankResponse = new BankSimulatorPaymentResponse { Authorized = true, AuthorizationCode = "auth-123" };
        _bankSimulatorApi.ProcessPayment(Arg.Any<BankSimulatorPaymentRequest>())
            .Returns(OneOf<BankSimulatorPaymentResponse, PaymentStatus>.FromT0(bankResponse));

        // Act
        var result = await _sut.ProcessPayment(request);

        // Assert
        Assert.True(result.IsT0);
        var payment = result.AsT0;
        Assert.Equal(PaymentStatus.Authorized, payment.Status);
        Assert.Equal(request.Amount, payment.Amount);
        Assert.Equal(request.Currency, payment.Currency);
        Assert.Equal("3456", payment.CardNumberLastFour);
        Assert.Equal(request.ExpiryMonth, payment.ExpiryMonth);
        Assert.Equal(request.ExpiryYear, payment.ExpiryYear);
        _paymentRepository.Received(1).Add(Arg.Is<PostPaymentResponse>(p =>
            p.Status == PaymentStatus.Authorized &&
            p.CardNumberLastFour == "3456" &&
            p.Amount == request.Amount));
    }

    [Fact]
    public async Task ProcessPayment_WhenBankDeclines_ReturnsPostPaymentResponseWithDeclinedAndStoresInRepository()
    {
        // Arrange
        var request = new PostPaymentRequest
        {
            CardNumber = "2222405343248878",
            ExpiryMonth = 12,
            ExpiryYear = 2026,
            Currency = "USD",
            Amount = 500,
            Cvv = "456"
        };
        var bankResponse = new BankSimulatorPaymentResponse { Authorized = false, AuthorizationCode = "" };
        _bankSimulatorApi.ProcessPayment(Arg.Any<BankSimulatorPaymentRequest>())
            .Returns(OneOf<BankSimulatorPaymentResponse, PaymentStatus>.FromT0(bankResponse));

        // Act
        var result = await _sut.ProcessPayment(request);

        // Assert
        Assert.True(result.IsT0);
        var payment = result.AsT0;
        Assert.Equal(PaymentStatus.Declined, payment.Status);
        Assert.Equal("8878", payment.CardNumberLastFour);
        _paymentRepository.Received(1).Add(Arg.Is<PostPaymentResponse>(p => p.Status == PaymentStatus.Declined));
    }

    [Fact]
    public async Task ProcessPayment_WhenBankReturnsRejected_ReturnsBadGatewayAndDoesNotStore()
    {
        // Arrange
        var request = new PostPaymentRequest
        {
            CardNumber = "1234567890123450",
            ExpiryMonth = 3,
            ExpiryYear = 2027,
            Currency = "EUR",
            Amount = 100,
            Cvv = "789"
        };
        _bankSimulatorApi.ProcessPayment(Arg.Any<BankSimulatorPaymentRequest>())
            .Returns(OneOf<BankSimulatorPaymentResponse, PaymentStatus>.FromT1(PaymentStatus.Rejected));

        // Act
        var result = await _sut.ProcessPayment(request);

        // Assert
        Assert.True(result.IsT1);
        Assert.Equal(HttpStatusCode.BadGateway, result.AsT1);
        _paymentRepository.DidNotReceive().Add(Arg.Any<PostPaymentResponse>());
    }

    [Fact]
    public async Task ProcessPayment_BuildsBankRequestWithCorrectExpiryFormat()
    {
        // Arrange
        var request = new PostPaymentRequest
        {
            CardNumber = "1234567890123456",
            ExpiryMonth = 4,
            ExpiryYear = 2025,
            Currency = "GBP",
            Amount = 99,
            Cvv = "123"
        };
        _bankSimulatorApi.ProcessPayment(Arg.Any<BankSimulatorPaymentRequest>())
            .Returns(OneOf<BankSimulatorPaymentResponse, PaymentStatus>.FromT0(
                new BankSimulatorPaymentResponse { Authorized = true, AuthorizationCode = "auth-123" }));

        // Act
        await _sut.ProcessPayment(request);

        // Assert
        await _bankSimulatorApi.Received(1).ProcessPayment(Arg.Is<BankSimulatorPaymentRequest>(r =>
            r.ExpiryDate == "04/2025" &&
            r.CardNumber == request.CardNumber &&
            r.Currency == request.Currency &&
            r.Amount == request.Amount &&
            r.Cvv == request.Cvv));
    }
}

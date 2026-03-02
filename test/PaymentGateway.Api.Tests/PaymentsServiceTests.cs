using System.Net;

using AutoFixture;
using Microsoft.Extensions.Logging;
using NSubstitute;
using PaymentGateway.Api.DataRepository;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Services;

namespace PaymentGateway.Api.Tests;

public class PaymentsServiceTests
{
    private readonly Fixture _fixture = new();
    private readonly IPaymentsRepository _paymentRepository;
    private readonly ILogger<PaymentsService> _logger;
    private readonly PaymentsService _sut;

    public PaymentsServiceTests()
    {
        _paymentRepository = Substitute.For<IPaymentsRepository>();
        _logger = Substitute.For<ILogger<PaymentsService>>();
        _sut = new PaymentsService(_logger, _paymentRepository);
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
        Assert.Equal(payment.Id, result.Id);
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
}

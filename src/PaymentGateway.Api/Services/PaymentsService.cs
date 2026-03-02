using System.Net;

using OneOf;

using PaymentGateway.Api.DataRepository;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.ApiRepository;
using PaymentGateway.Api.Enums;

namespace PaymentGateway.Api.Services;

public class PaymentsService
{
    private readonly ILogger<PaymentsService> _logger;
    private readonly IPaymentsRepository _paymentsRepository;
    private readonly IBankSimulatorApi _bankSimulatorApi;

    public PaymentsService(ILogger<PaymentsService> logger, IPaymentsRepository paymentsRepository, IBankSimulatorApi bankSimulatorApi)
    {
        _logger = logger;
        _paymentsRepository = paymentsRepository;
        _bankSimulatorApi = bankSimulatorApi;
    }

    public PostPaymentResponse? GetPayment(Guid id)
    {
        _logger.LogInformation("Getting payment with id {PaymentId}", id);
        return _paymentsRepository.Get(id);
    }

    public async Task<OneOf<PostPaymentResponse, HttpStatusCode>> ProcessPayment(PostPaymentRequest request)
    {
        _logger.LogInformation("Processing payment with amount {Amount} and currency {Currency}", request.Amount, request.Currency);

        var paymentRequest = new BankSimulatorPaymentRequest
        {
            CardNumber = request.CardNumber,
            Currency = request.Currency,
            Amount = request.Amount,
            Cvv = request.Cvv,
            ExpiryDate = $"{request.ExpiryMonth:D2}/{request.ExpiryYear}"
        };

        var response = await _bankSimulatorApi.ProcessPayment(paymentRequest);

        return response.Match(
            success =>
            {
                var paymentResponse = new PostPaymentResponse
                {
                    Id = Guid.NewGuid(),
                    Amount = request.Amount,
                    Currency = request.Currency,
                    CardNumberLastFour = request.CardNumber[^4..],
                    ExpiryMonth = request.ExpiryMonth,
                    ExpiryYear = request.ExpiryYear,
                    Status = success.Authorized ? PaymentStatus.Authorized : PaymentStatus.Declined
                };
                _paymentsRepository.Add(paymentResponse);
                return OneOf<PostPaymentResponse, HttpStatusCode>.FromT0(paymentResponse);
            },
            rejected =>
            {
                _logger.LogWarning("Payment was rejected by Bank Simulator API (e.g. 503 Service Unavailable)");
                return OneOf<PostPaymentResponse, HttpStatusCode>.FromT1(HttpStatusCode.BadGateway);
            });
    }
}

using System.Net;

using OneOf;

using PaymentGateway.Api.DataRepository;
using PaymentGateway.Api.Models;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.ApiRepository;
using PaymentGateway.Api.Enums;

namespace PaymentGateway.Api.Services;

public class PaymentsService(ILogger<PaymentsService> logger, IPaymentsRepository paymentsRepository, IBankSimulatorApi bankSimulatorApi)
{
    public PaymentResponse? GetPayment(Guid id)
    {
        logger.LogInformation("Getting payment with id {PaymentId}", id);
        var payment = paymentsRepository.Get(id);
        return payment?.ToPostPaymentResponse();
    }

    public async Task<OneOf<PaymentResponse, HttpStatusCode>> ProcessPayment(PostPaymentRequest request)
    {
        logger.LogInformation("Processing payment with amount {Amount} and currency {Currency}", request.Amount, request.Currency);

        var paymentRequest = new BankSimulatorPaymentRequest
        {
            CardNumber = request.CardNumber,
            Currency = request.Currency,
            Amount = request.Amount,
            Cvv = request.Cvv,
            ExpiryDate = $"{request.ExpiryMonth:D2}/{request.ExpiryYear}"
        };

        var response = await bankSimulatorApi.ProcessPayment(paymentRequest);

        return response.Match(
            success =>
            {
                var payment = new Payment
                {
                    Id = Guid.NewGuid(),
                    Status = success.Authorized ? PaymentStatus.Authorized : PaymentStatus.Declined,
                    LastFourCardDigits = request.CardNumber[^4..],
                    ExpiryMonth = request.ExpiryMonth,
                    ExpiryYear = request.ExpiryYear,
                    Currency = request.Currency,
                    Amount = request.Amount,
                    AuthorizationCode = success.AuthorizationCode ?? string.Empty
                };
                paymentsRepository.Add(payment);
                return OneOf<PaymentResponse, HttpStatusCode>.FromT0(payment.ToPostPaymentResponse());
            },
            rejected =>
            {
                logger.LogWarning("Payment was rejected by Bank Simulator API (e.g. 503 Service Unavailable)");
                return OneOf<PaymentResponse, HttpStatusCode>.FromT1(HttpStatusCode.BadGateway);
            });
    }
}

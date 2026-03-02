
using OneOf;

using PaymentGateway.Api.Enums;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;

namespace PaymentGateway.Api.ApiRepository;

public interface IBankSimulatorApi
{
    Task<OneOf<BankSimulatorPaymentResponse, PaymentStatus>> ProcessPayment(BankSimulatorPaymentRequest request);
}
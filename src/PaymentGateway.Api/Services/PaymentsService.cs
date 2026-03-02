using System.Net;

using OneOf;

using PaymentGateway.Api.DataRepository;
using PaymentGateway.Api.Models.Responses;

namespace PaymentGateway.Api.Services;

public class PaymentsService
{
    private readonly ILogger<PaymentsService> _logger;
    private readonly IPaymentsRepository _paymentsRepository;

    public PaymentsService(ILogger<PaymentsService> logger, IPaymentsRepository paymentsRepository)
    {
        _logger = logger;
        _paymentsRepository = paymentsRepository;
    }

    public PostPaymentResponse? GetPayment(Guid id)
    {
        _logger.LogInformation("Getting payment with id {PaymentId}", id);
        return _paymentsRepository.Get(id);
    }
}

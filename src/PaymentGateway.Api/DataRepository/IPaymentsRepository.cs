using PaymentGateway.Api.Models;

namespace PaymentGateway.Api.DataRepository;

public interface IPaymentsRepository
{
    void Add(Payment payment);
    Payment? Get(Guid id);
}

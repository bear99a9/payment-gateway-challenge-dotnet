using PaymentGateway.Api.Enums;
using PaymentGateway.Api.Models.Responses;

namespace PaymentGateway.Api.Models
{
    public class Payment
    {
        public Guid Id { get; set; }
        public PaymentStatus Status { get; set; }
        public string LastFourCardDigits { get; set; } = string.Empty;
        public int ExpiryMonth { get; set; }
        public int ExpiryYear { get; set; }
        public string Currency { get; set; } = string.Empty;
        public int Amount { get; set; }
        public string AuthorizationCode { get; set; } = string.Empty;

        /// <summary>Maps to API response (excludes AuthorizationCode for PCI).</summary>
        public PostPaymentResponse ToPostPaymentResponse()
        {
            return new PostPaymentResponse
            {
                Id = Id,
                Status = Status,
                CardNumberLastFour = LastFourCardDigits,
                ExpiryMonth = ExpiryMonth,
                ExpiryYear = ExpiryYear,
                Currency = Currency,
                Amount = Amount
            };
        }
    }
}

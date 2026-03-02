using System.Text.Json.Serialization;

namespace PaymentGateway.Api.Models.Requests
{
    public class BankSimulatorPaymentRequest
    {
        [JsonPropertyName("card_number")]
        public string CardNumber { get; init; } = null!;

        [JsonPropertyName("expiry_date")]
        public string ExpiryDate { get; init; } = null!;

        [JsonPropertyName("cvv")]
        public string Cvv { get; init; } = null!;

        [JsonPropertyName("currency")]
        public string Currency { get; init; } = null!;

        [JsonPropertyName("amount")]
        public int Amount { get; init; }
    }
}

using System.Text.Json.Serialization;

namespace PaymentGateway.Api.Models.Responses;

public class BankSimulatorPaymentResponse
{
    [JsonPropertyName("authorized")]
    public bool Authorized { get; init; }

    [JsonPropertyName("authorization_code")]
    public string AuthorizationCode { get; init; } = null!;
}

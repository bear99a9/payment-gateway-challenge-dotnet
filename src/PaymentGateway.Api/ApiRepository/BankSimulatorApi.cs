using System.Net;
using OneOf;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Enums;
using RestSharp;

namespace PaymentGateway.Api.ApiRepository;

public class BankSimulatorApi(RestClient restClient, ILogger<BankSimulatorApi> logger) : IBankSimulatorApi
{
    public async Task<OneOf<BankSimulatorPaymentResponse, PaymentStatus>> ProcessPayment(BankSimulatorPaymentRequest request)
    {
        var cardLastFour = request.CardNumber.Length >= 4 ? request.CardNumber[^4..] : "****";
        try
        {
            logger.LogInformation("Processing payment with Bank Simulator API: Amount={Amount} {Currency}, card ending {CardLastFour}", request.Amount, request.Currency, cardLastFour);

            var restRequest = new RestRequest("payments")
                .AddJsonBody(request);

            var response = await restClient.ExecutePostAsync<BankSimulatorPaymentResponse>(restRequest);

            if (response.IsSuccessful && response.Data is not null)
            {
                return response.Data;
            }

            if (response.StatusCode == HttpStatusCode.ServiceUnavailable)
            {
                return PaymentStatus.Rejected;
            }

            logger.LogError("Bank Simulator API returned unexpected HTTP status {StatusCode}", response.StatusCode);
            throw new HttpRequestException($"Bank Simulator API returned {(int)response.StatusCode} {response.StatusCode}.");
        }
        catch (HttpRequestException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process payment with Bank Simulator API: Amount={Amount} {Currency}, card ending {CardLastFour}", request.Amount, request.Currency, cardLastFour);
            throw;
        }
    }
}

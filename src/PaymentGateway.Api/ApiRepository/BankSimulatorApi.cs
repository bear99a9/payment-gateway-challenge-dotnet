using System.Net;
using System.Text.Json;
using OneOf;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Enums;
using RestSharp;

namespace PaymentGateway.Api.ApiRepository;

public class BankSimulatorApi : IBankSimulatorApi
{
    private readonly RestClient _restClient;
    private readonly ILogger<BankSimulatorApi> _logger;

    public BankSimulatorApi(RestClient restClient, ILogger<BankSimulatorApi> logger)
    {
        _restClient = restClient;
        _logger = logger;
    }

    public async Task<OneOf<BankSimulatorPaymentResponse, PaymentStatus>> ProcessPayment(BankSimulatorPaymentRequest request)
    {
        try
        {
            _logger.LogInformation("Processing payment with Bank Simulator API: {Request}", JsonSerializer.Serialize(request));

            var restRequest = new RestRequest("payments")
                .AddJsonBody(request);

            var response = await _restClient.ExecutePostAsync<BankSimulatorPaymentResponse>(restRequest);

            if (response.IsSuccessful && response.Data is not null)
            {
                return response.Data;
            }

            if (response.StatusCode == HttpStatusCode.ServiceUnavailable)
            {
                return PaymentStatus.Rejected;
            }

            _logger.LogError("Bank Simulator API returned unexpected HTTP status {StatusCode}", response.StatusCode);
            throw new HttpRequestException($"Bank Simulator API returned {(int)response.StatusCode} {response.StatusCode}.");
        }
        catch (HttpRequestException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process payment with Bank Simulator API: {Request}", JsonSerializer.Serialize(request));
            throw;
        }
    }
}

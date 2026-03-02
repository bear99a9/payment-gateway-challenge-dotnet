using System.Net;
using System.Net.Http.Json;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

using PaymentGateway.Api.DataRepository;
using PaymentGateway.Api.Enums;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;

using AutoFixture;

namespace PaymentGateway.Api.Tests.IntegrationTests;

public class PaymentsControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string endpoint = "/api/Payments/";
    private readonly WebApplicationFactory<Program> _factory;
    private readonly Fixture _fixture = new();

    public PaymentsControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private HttpClient CreateClient() => _factory.CreateClient();

    private static PostPaymentRequest CreatePaymentRequest(
        string? cardNumber = null)
    {
        return new PostPaymentRequest
        {
            CardNumber = cardNumber ?? "1234567890123456",
            ExpiryMonth =  12,
            ExpiryYear = DateTime.UtcNow.Year + 1,
            Currency = "GBP",
            Amount = 100,
            Cvv = "123"
        };
    }

    [Fact]
    public async Task RetrievesAPaymentSuccessfully()
    {
        // Arrange
        var payment = _fixture.Create<PostPaymentResponse>();

        var paymentsRepository = new PaymentsRepository();
        paymentsRepository.Add(payment);

        var client = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services => ((ServiceCollection)services)
                .AddSingleton<IPaymentsRepository>(paymentsRepository)))
            .CreateClient();

        // Act
        var response = await client.GetAsync($"{endpoint}{payment.Id}");
        var paymentResponse = await response.Content.ReadFromJsonAsync<PostPaymentResponse>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(paymentResponse);
        Assert.Equal(payment.Id, paymentResponse!.Id);
        Assert.Equal(payment.Status, paymentResponse.Status);
        Assert.Equal(payment.CardNumberLastFour, paymentResponse.CardNumberLastFour);
        Assert.Equal(payment.ExpiryMonth, paymentResponse.ExpiryMonth);
        Assert.Equal(payment.ExpiryYear, paymentResponse.ExpiryYear);
        Assert.Equal(payment.Currency, paymentResponse.Currency);
        Assert.Equal(payment.Amount, paymentResponse.Amount);
    }

    [Fact]
    public async Task Returns404IfPaymentNotFound()
    {
        // Arrange
        var client = CreateClient();

        // Act
        var response = await client.GetAsync($"{endpoint}{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }


    [Fact]
    public async Task ProcessPayment_WhenCardEndsInOddNumber_Returns200AuthorizedAndPaymentCanBeRetrieved()
    {
        // Arrange
        var client = CreateClient();
        var request = CreatePaymentRequest(cardNumber: "1234567890123457");

        // Act
        var response = await client.PostAsJsonAsync(endpoint, request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payment = await response.Content.ReadFromJsonAsync<PostPaymentResponse>();
        Assert.NotNull(payment);
        Assert.Equal(PaymentStatus.Authorized, payment!.Status);
        Assert.Equal("3457", payment.CardNumberLastFour);
        Assert.Equal(request.Amount, payment.Amount);
        Assert.Equal(request.Currency, payment.Currency);

        var getResponse = await client.GetAsync($"{endpoint}{payment.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var retrieved = await getResponse.Content.ReadFromJsonAsync<PostPaymentResponse>();
        Assert.NotNull(retrieved);
        Assert.Equal(payment.Id, retrieved!.Id);
        Assert.Equal(PaymentStatus.Authorized, retrieved.Status);
    }

    [Fact]
    public async Task ProcessPayment_WhenCardEndsInEvenNumber_Returns200DeclinedAndPaymentCanBeRetrieved()
    {
        var client = CreateClient();
        var request = CreatePaymentRequest(cardNumber: "2222405343248878");

        var response = await client.PostAsJsonAsync(endpoint, request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payment = await response.Content.ReadFromJsonAsync<PostPaymentResponse>();
        Assert.NotNull(payment);
        Assert.Equal(PaymentStatus.Declined, payment!.Status);
        Assert.Equal("8878", payment.CardNumberLastFour);

        var getResponse = await client.GetAsync($"{endpoint}{payment!.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
    }

    [Fact]
    public async Task ProcessPayment_WhenCardEndsInZero_Returns502BadGateway()
    {
        // Arrange
        var client = CreateClient();
        var request = CreatePaymentRequest(cardNumber: "2222405343248870");

        // Act
        var response = await client.PostAsJsonAsync(endpoint, request);

        // Assert
        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
    }

    [Fact]
    public async Task ProcessPayment_WhenValidationFails_Returns400BadRequest()
    {
        // Arrange
        var client = CreateClient();
        var request = CreatePaymentRequest(cardNumber: "123");

        // Act
        var response = await client.PostAsJsonAsync(endpoint, request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
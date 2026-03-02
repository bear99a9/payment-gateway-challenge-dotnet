using System.Text.Json.Serialization;

using FluentValidation;

using Microsoft.Extensions.Options;

using PaymentGateway.Api.ApiRepository;
using PaymentGateway.Api.DataRepository;
using PaymentGateway.Api.Models;
using PaymentGateway.Api.Services;
using PaymentGateway.Api.Validators;

using RestSharp;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
        
builder.Services.AddValidatorsFromAssemblyContaining<PostPaymentRequestValidator>();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<ApiOptions>(
    builder.Configuration.GetSection(ApiOptions.SectionName));

builder.Services.AddSingleton<IPaymentsRepository, PaymentsRepository>();
builder.Services.AddSingleton<PaymentsService>();
builder.Services.AddSingleton<IBankSimulatorApi, BankSimulatorApi>();

builder.Services.AddSingleton<RestClient>(sp =>
{
    var options = sp.GetRequiredService<IOptions<ApiOptions>>().Value;
    return new RestClient(new RestClientOptions(options.BaseUrl));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

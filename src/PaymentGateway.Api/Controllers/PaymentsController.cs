using FluentValidation;

using Microsoft.AspNetCore.Mvc;

using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Services;

namespace PaymentGateway.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PaymentsController(PaymentsService paymentsService, IValidator<PostPaymentRequest> validator) : Controller
{
    [HttpPost]
    public async Task<ActionResult<PaymentResponse>> ProcessPaymentAsync([FromBody] PostPaymentRequest request)
    {
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }));
        }

        var result = await paymentsService.ProcessPayment(request);

        return result.Match<ActionResult<PaymentResponse>>(
            payment => Ok(payment),
            statusCode => statusCode switch
            {
                _ => StatusCode((int)statusCode)
            });
    }

    [HttpGet("{id:guid}")]
    public ActionResult<PaymentResponse> GetPaymentAsync(Guid id)
    {
        var payment = paymentsService.GetPayment(id);

        if (payment is null)
        {
            return NotFound();
        }

        return new OkObjectResult(payment);
    }
}
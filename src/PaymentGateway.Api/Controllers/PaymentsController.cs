using FluentValidation;

using Microsoft.AspNetCore.Mvc;

using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Services;

namespace PaymentGateway.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PaymentsController : Controller
{
    private readonly PaymentsService _paymentsService;
    private readonly IValidator<PostPaymentRequest> _validator;

    public PaymentsController(PaymentsService paymentsService, IValidator<PostPaymentRequest> validator)
    {
        _paymentsService = paymentsService;
        _validator = validator;
    }

    [HttpPost]
    public async Task<ActionResult<PostPaymentResponse>> ProcessPaymentAsync([FromBody] PostPaymentRequest request)
    {
        var validationResult = await _validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }));
        }

        var result = await _paymentsService.ProcessPayment(request);

        return result.Match<ActionResult<PostPaymentResponse>>(
            payment => Ok(payment),
            statusCode => statusCode switch
            {
                _ => StatusCode((int)statusCode)
            });
    }

    [HttpGet("{id:guid}")]
    public ActionResult<PostPaymentResponse> GetPaymentAsync(Guid id)
    {
        var payment = _paymentsService.GetPayment(id);

        if (payment is null)
        {
            return NotFound();
        }

        return new OkObjectResult(payment);
    }
}
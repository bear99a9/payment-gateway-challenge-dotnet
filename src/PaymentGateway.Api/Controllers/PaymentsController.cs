using Microsoft.AspNetCore.Mvc;

using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Services;

namespace PaymentGateway.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PaymentsController : Controller
{
    private readonly PaymentsService _paymentsService;

    public PaymentsController(PaymentsService paymentsService)
    {
        _paymentsService = paymentsService;
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
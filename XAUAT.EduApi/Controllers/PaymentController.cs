using Microsoft.AspNetCore.Mvc;
using XAUAT.EduApi.Services;

namespace XAUAT.EduApi.Controllers;

[ApiController]
[Route("[controller]")]
public class PaymentController(IPaymentService paymentService, ILogger<PaymentController> logger)
    : ControllerBase
{
    [HttpGet("{id}")]
    public async Task<IActionResult> Login(string id)
    {
        logger.LogInformation("Login with card number {id}", id);
        var result = await paymentService.Login(id);
        logger.LogInformation("Login result: {result}", result);
        return Ok(result);
    }

    [HttpGet("{id}/turnover")]
    public async Task<IActionResult> GetTurnover(string id)
    {
        logger.LogInformation("Get turnover with card number {id}", id);
        var result = await paymentService.GetTurnoverAsync(id);
        logger.LogInformation("Get turnover result: {result}", result);
        return Ok(result);
    }
}
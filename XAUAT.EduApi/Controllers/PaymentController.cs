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
        try
        {
            logger.LogInformation("Login with card number {id}", id);
            var result = await paymentService.Login(id);
            logger.LogInformation("Login result: {result}", result);
            return Ok(result);
        }
        catch (PaymentServiceException ex)
        {
            logger.LogError(ex, "Payment service error during login for card {id}", id);
            return StatusCode(503, new { error = "服务暂时不可用", message = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error during login for card {id}", id);
            return StatusCode(500, new { error = "服务器内部错误", message = "登录过程中发生未知错误" });
        }
    }

    [HttpGet("{id}/turnover")]
    public async Task<IActionResult> GetTurnover(string id)
    {
        try
        {
            logger.LogInformation("Get turnover with card number {id}", id);
            var result = await paymentService.GetTurnoverAsync(id);
            logger.LogInformation("Get turnover result: {result}", result);
            return Ok(result);
        }
        catch (PaymentServiceException ex)
        {
            logger.LogError(ex, "Payment service error during fetching turnover for card {id}", id);
            return StatusCode(503, new { error = "服务暂时不可用", message = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error during fetching turnover for card {id}", id);
            return StatusCode(500, new { error = "服务器内部错误", message = "获取消费记录过程中发生未知错误" });
        }
    }
}
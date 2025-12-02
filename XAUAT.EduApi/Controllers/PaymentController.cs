using Microsoft.AspNetCore.Mvc;
using XAUAT.EduApi.Services;

namespace XAUAT.EduApi.Controllers;

/// <summary>
/// 支付控制器
/// 提供校园卡支付系统的登录和消费记录查询功能
/// </summary>
[ApiController]
[Route("[controller]")]
[Produces("application/json")]
[Consumes("application/json")]
public class PaymentController(IPaymentService paymentService, ILogger<PaymentController> logger)
    : ControllerBase
{
    /// <summary>
    /// 校园卡登录
    /// 使用校园卡号登录支付系统
    /// </summary>
    /// <param name="id">校园卡号</param>
    /// <returns>登录结果</returns>
    /// <response code="200">登录成功</response>
    /// <response code="500">服务器内部错误</response>
    /// <response code="503">服务暂时不可用</response>
    /// <remarks>
    /// 示例请求：
    /// GET /Payment/123456789
    /// </remarks>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(typeof(object), StatusCodes.Status503ServiceUnavailable)]
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

    /// <summary>
    /// 获取消费记录
    /// 根据校园卡号获取消费记录
    /// </summary>
    /// <param name="id">校园卡号</param>
    /// <returns>消费记录列表</returns>
    /// <response code="200">成功获取消费记录</response>
    /// <response code="500">服务器内部错误</response>
    /// <response code="503">服务暂时不可用</response>
    /// <remarks>
    /// 示例请求：
    /// GET /Payment/123456789/turnover
    /// </remarks>
    [HttpGet("{id}/turnover")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(typeof(object), StatusCodes.Status503ServiceUnavailable)]
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
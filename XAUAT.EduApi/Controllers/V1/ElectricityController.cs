using System.ComponentModel.DataAnnotations;
using EduApi.Data.Models;
using Microsoft.AspNetCore.Mvc;
using XAUAT.EduApi.Localization;
using XAUAT.EduApi.Services;

namespace XAUAT.EduApi.Controllers.V1;

[ApiController]
[Route("v1/electricity")]
[Produces("application/json")]
public class ElectricityController(
    IElectricityService service,
    IElectricitySubscriptionService subscriptionService,
    ILogger<ElectricityController> logger,
    ILanguageResolver languageResolver,
    IApiMessageLocalizer messageLocalizer) : V1ControllerBase(languageResolver, messageLocalizer)
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<double>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<double>>> GetCurrentBalance([FromQuery] string? url = null)
    {
        try
        {
            logger.LogInformation("开始获取当前电费余额");
            var balance = await service.FetchCurrentBalanceAsync(url);

            if (!balance.HasValue)
            {
                return NotFound(ErrorResponse(ApiCodes.NotFound, "未找到电费余额数据"));
            }

            return Ok(SuccessResponse(balance.Value));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "获取当前电费余额时出错");
            return StatusCode(StatusCodes.Status500InternalServerError,
                ErrorResponse(ApiCodes.InternalError, "获取当前电费余额失败"));
        }
    }

    [HttpGet("WeeklyData")]
    [ProducesResponseType(typeof(ApiResponse<List<ElectricData>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<ElectricData>>>> GetWeeklyData([FromQuery] string? url = null)
    {
        try
        {
            logger.LogInformation("开始获取电费周明细");
            var data = await service.FetchWeeklyDataAsync(url);
            return Ok(SuccessListResponse(data));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "获取电费周明细时出错");
            return StatusCode(StatusCodes.Status500InternalServerError,
                ErrorResponse(ApiCodes.InternalError, "获取电费周明细失败"));
        }
    }

    [HttpGet("RechargeUrl")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<string>>> GetRechargeUrl([FromQuery] string? url = null)
    {
        try
        {
            logger.LogInformation("开始获取电费充值地址");
            var urlData = await service.GetRechargeUrlAsync(url);

            if (string.IsNullOrWhiteSpace(urlData))
            {
                return NotFound(ErrorResponse(ApiCodes.NotFound, "未找到电费充值地址"));
            }

            return Ok(SuccessResponse(urlData));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "获取电费充值地址时出错");
            return StatusCode(StatusCodes.Status500InternalServerError,
                ErrorResponse(ApiCodes.InternalError, "获取电费充值地址失败"));
        }
    }

    [HttpPost("Subscriptions")]
    [ProducesResponseType(typeof(ApiResponse<ElectricitySubscriptionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<ElectricitySubscriptionResponse>>> UpsertSubscription(
        [FromBody] CreateElectricitySubscriptionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("开始创建或更新电费订阅，Email: {Email}", request.Email);
            var subscription = await subscriptionService.UpsertAsync(request, cancellationToken);
            return Ok(SuccessResponse(subscription));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "创建或更新电费订阅时出错，Email: {Email}", request.Email);
            return StatusCode(StatusCodes.Status500InternalServerError,
                ErrorResponse(ApiCodes.InternalError, "创建或更新电费订阅失败"));
        }
    }

    [HttpGet("Subscriptions")]
    [ProducesResponseType(typeof(ApiResponse<ElectricitySubscriptionQueryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<ElectricitySubscriptionQueryResponse>>> QuerySubscriptionByEmail(
        [FromQuery] [Required(ErrorMessage = "邮箱不能为空")] [EmailAddress(ErrorMessage = "邮箱格式不正确")]
        string email,
        CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("开始根据邮箱查询电费订阅，Email: {Email}", email);
            var result = await subscriptionService.QueryByEmailAsync(email, cancellationToken);
            return Ok(SuccessResponse(result));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "根据邮箱查询电费订阅时出错，Email: {Email}", email);
            return StatusCode(StatusCodes.Status500InternalServerError,
                ErrorResponse(ApiCodes.InternalError, "查询电费订阅失败"));
        }
    }

    [HttpDelete("Subscriptions/{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteSubscription(string id, CancellationToken cancellationToken)
    {
        try
        {
            var deleted = await subscriptionService.DeleteAsync(id, cancellationToken);
            if (!deleted)
            {
                return NotFound(ErrorResponse(ApiCodes.NotFound, "未找到对应的电费订阅"));
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "删除电费订阅时出错，SubscriptionId: {SubscriptionId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                ErrorResponse(ApiCodes.InternalError, "删除电费订阅失败"));
        }
    }
}

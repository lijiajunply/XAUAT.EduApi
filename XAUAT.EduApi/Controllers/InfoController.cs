using Microsoft.AspNetCore.Mvc;
using XAUAT.EduApi.Services;

namespace XAUAT.EduApi.Controllers;

/// <summary>
/// 信息查询控制器
/// 提供学业进度查询和系统时间获取功能
/// </summary>
[ApiController]
[Route("[controller]")]
[Produces("application/json")]
public class InfoController(IHttpClientFactory httpClientFactory, ILogger<CourseController> logger, IInfoService info)
    : ControllerBase
{
    /// <summary>
    /// 获取学业进度
    /// 从教务处系统获取学生的学业完成情况预览
    /// </summary>
    /// <returns>学业进度数据，包含已完成学分、待修学分等信息</returns>
    /// <response code="200">成功获取学业进度数据</response>
    /// <response code="401">未授权，需要有效的身份认证Cookie</response>
    /// <response code="500">服务器内部错误，无法连接到教务处系统</response>
    /// <response code="502">教务处系统响应错误</response>
    /// <remarks>
    /// 示例请求：
    /// GET /Info/Completion
    /// Cookie: YOUR_AUTH_COOKIE
    /// 
    /// 或使用自定义请求头：
    /// GET /Info/Completion
    /// xauat: YOUR_AUTH_COOKIE
    /// </remarks>
    [HttpGet("Completion")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(typeof(string), StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> GetCompletion()
    {
        try
        {
            logger.LogInformation("开始抓取学业进度");
            var cookie = Request.Headers.Cookie.ToString();
            if (string.IsNullOrEmpty(cookie))
            {
                cookie = Request.Headers["xauat"].ToString(); // 从请求中获取 cookie
            }

            using var client = httpClientFactory.CreateClient();
            client.SetRealisticHeaders();
            client.Timeout = TimeSpan.FromSeconds(6); // 添加超时控制
            client.DefaultRequestHeaders.Add("Cookie", cookie);
            var response =
                await client.GetAsync("https://swjw.xauat.edu.cn/student/ws/student/home-page/programCompletionPreview");
            var content = await response.Content.ReadAsStringAsync();

            if (content.Contains("登入页面"))
            {
                throw new Exceptions.UnAuthenticationError();
            }

            return Content(content);
        }
        catch (Exceptions.UnAuthenticationError)
        {
            return Unauthorized("认证失败，请重新登录");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "获取学业进度时出错");
            return StatusCode(500, "获取学业进度失败");
        }
    }

    /// <summary>
    /// 获取系统时间
    /// 获取服务器当前时间信息
    /// </summary>
    /// <returns>系统时间信息，包含当前日期、时间、星期等</returns>
    /// <response code="200">成功获取系统时间</response>
    /// <remarks>
    /// 示例请求：
    /// GET /Info/Time
    /// </remarks>
    [HttpGet("Time")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public ActionResult GetTime()
    {
        return Ok(info.GetTime());
    }
}
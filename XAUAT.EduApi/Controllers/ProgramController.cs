using Microsoft.AspNetCore.Mvc;
using XAUAT.EduApi.Services;

namespace XAUAT.EduApi.Controllers;

/// <summary>
/// 培养方案控制器
/// 提供专业培养计划查询功能，获取学生所属专业的课程培养方案
/// </summary>
[ApiController]
[Route("[controller]")]
[Produces("application/json")]
public class ProgramController(
    ILogger<ProgramController> logger,
    IProgramService program) : ControllerBase
{
    /// <summary>
    /// 获取所有培养方案
    /// 从教务处系统获取指定专业的所有培养方案，并支持按名称过滤
    /// </summary>
    /// <param name="id">专业ID</param>
    /// <param name="name">可选，课程名称过滤条件</param>
    /// <returns>培养方案列表，包含课程名称、学分、课程类型等信息</returns>
    /// <response code="200">成功获取培养方案列表</response>
    /// <response code="401">未授权，需要有效的身份认证Cookie</response>
    /// <response code="500">服务器内部错误，无法获取培养方案</response>
    /// <remarks>
    /// 示例请求：
    /// GET /Program?id=123456
    /// 
    /// 按名称过滤：
    /// GET /Program?id=123456& name=计算机
    /// 
    /// 请求头：
    /// Cookie: YOUR_AUTH_COOKIE
    /// 
    /// 或使用自定义请求头：
    /// xauat: YOUR_AUTH_COOKIE
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(typeof(List<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAllTrainProgram(string id, string? name)
    {
        try
        {
            var cookie = Request.Headers.Cookie.ToString();
            if (string.IsNullOrEmpty(cookie))
            {
                cookie = Request.Headers["xauat"].ToString(); // 从请求中获取 cookie
            }

            var result = await program.GetAllTrainProgram(cookie, id);
            if (!string.IsNullOrEmpty(name))
            {
                result = result.Where(x => x.Name.Contains(name)).ToList();
            }

            return Ok(result);
        }
        catch (Exceptions.UnAuthenticationError)
        {
            return Unauthorized("认证失败，请重新登录");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get training program");
            return StatusCode(500, "获取培养方案失败");
        }
    }

    /// <summary>
    /// 获取培养方案字典
    /// 从教务处系统获取指定专业的培养方案字典数据
    /// </summary>
    /// <param name="id">专业ID</param>
    /// <returns>培养方案字典，包含课程分类和对应课程列表</returns>
    /// <response code="200">成功获取培养方案字典</response>
    /// <response code="401">未授权，需要有效的身份认证Cookie</response>
    /// <response code="500">服务器内部错误，无法获取培养方案字典</response>
    /// <remarks>
    /// 示例请求：
    /// GET /Program/GetDic?id=123456
    /// 
    /// 请求头：
    /// Cookie: YOUR_AUTH_COOKIE
    /// 
    /// 或使用自定义请求头：
    /// xauat: YOUR_AUTH_COOKIE
    /// </remarks>
    [HttpGet("GetDic")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAllTrainPrograms(string id)
    {
        try
        {
            var cookie = Request.Headers.Cookie.ToString();
            if (string.IsNullOrEmpty(cookie))
            {
                cookie = Request.Headers["xauat"].ToString(); // 从请求中获取 cookie
            }

            var result = await program.GetAllTrainPrograms(cookie, id);
            return Ok(result);
        }
        catch (Exceptions.UnAuthenticationError)
        {
            return Unauthorized("认证失败，请重新登录");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get training program");
            return StatusCode(500, "获取培养方案失败");
        }
    }
}
using Microsoft.AspNetCore.Mvc;
using XAUAT.EduApi.Services;

namespace XAUAT.EduApi.Controllers;

/// <summary>
/// 课程控制器
/// 提供学生课程信息查询功能
/// </summary>
[ApiController]
[Route("[controller]")]
[Produces("application/json")]
[Consumes("application/json")]
public class CourseController(ILogger<CourseController> logger, ICourseService courseService)
    : ControllerBase
{
    /// <summary>
    /// 获取学生课程信息
    /// 根据学生ID获取当前学期的课程列表
    /// </summary>
    /// <param name="studentId">学生ID，多个ID用逗号分隔</param>
    /// <returns>课程列表，包含课程名称、教师、上课时间等信息</returns>
    /// <response code="200">成功获取课程信息</response>
    /// <response code="400">参数错误</response>
    /// <response code="404">操作无效，未找到课程信息</response>
    /// <response code="500">服务器内部错误</response>
    /// <response code="502">HTTP请求错误，教务处系统访问失败</response>
    /// <remarks>
    /// 示例请求：
    /// GET /Course?studentId=123456
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(typeof(object), StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> GetCourse(string studentId)
    {
        try
        {
            logger.LogInformation("开始获取课程信息");
            var cookie = Request.Headers.Cookie.ToString();
            if (string.IsNullOrEmpty(cookie) || cookie.StartsWith("Rider"))
            {
                cookie = Request.Headers["xauat"].ToString(); // 从请求中获取 cookie
            }

            var courses = await courseService.GetCoursesAsync(studentId, cookie);

            // 返回处理后的课程数据
            return Ok(new
            {
                Success = true,
                Data = courses,
                ExpirationTime = DateTime.Now.AddDays(7)
            });
        }
        catch (Exceptions.UnAuthenticationError)
        {
            return Unauthorized(new { Success = false, Message = "认证失败，请重新登录" });
        }
        catch (ArgumentNullException ex)
        {
            logger.LogWarning(ex, "参数错误");
            return BadRequest(new { Success = false, ex.Message });
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "HTTP请求错误");
            return StatusCode(502, new { Success = false, ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "操作无效");
            return NotFound(new { Success = false, ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "获取课程时发生错误");
            return StatusCode(500, new { Success = false, Message = "服务器内部错误" });
        }
    }
}
using Microsoft.AspNetCore.Mvc;
using XAUAT.EduApi.Services;

namespace XAUAT.EduApi.Controllers;

[ApiController]
[Route("[controller]")]
public class CourseController(ILogger<CourseController> logger, ICourseService courseService)
    : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetCourse(string studentId)
    {
        try
        {
            logger.LogInformation("开始获取课程信息");
            var cookie = Request.Headers.Cookie.ToString();
            if (string.IsNullOrEmpty(cookie))
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
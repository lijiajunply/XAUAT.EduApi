using Microsoft.AspNetCore.Mvc;
using XAUAT.EduApi.Models;
using XAUAT.EduApi.Services;

namespace XAUAT.EduApi.Controllers;

[ApiController]
[Route("[controller]")]
public class ExamController(
    IExamService examService,
    ILogger<ExamController> logger) : ControllerBase
{
    /// <summary>
    /// 获取考试安排
    /// </summary>
    /// <info>
    /// 需要在Headers上加个xauat的cookie信息
    /// </info>
    /// <returns></returns>
    [HttpGet]
    public async Task<ActionResult<ExamResponse>> GetExamArrangements(string? studentId)
    {
        try
        {
            var cookie = Request.Headers.Cookie.ToString();
            if (string.IsNullOrEmpty(cookie))
            {
                cookie = Request.Headers["xauat"].ToString(); // 从请求中获取 cookie
            }

            var result = await examService.GetExamArrangementsAsync(cookie, studentId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get exam arrangements");
            return StatusCode(500, "获取考试安排失败");
        }
    }
}
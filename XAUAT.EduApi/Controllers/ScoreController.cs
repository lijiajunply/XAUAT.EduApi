using EduApi.Data.Models;
using Microsoft.AspNetCore.Mvc;
using XAUAT.EduApi.Services;

namespace XAUAT.EduApi.Controllers
{
    /// <summary>
    /// 成绩查询控制器
    /// 提供学生成绩查询、学期数据解析等功能
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    [Produces("application/json")]
    [Consumes("application/json")]
    public class ScoreController(ILogger<ScoreController> logger, IScoreService scoreService)
        : ControllerBase
    {
        /// <summary>
        /// 解析学期数据
        /// 获取学生可查询的学期列表
        /// </summary>
        /// <param name="studentId">学生ID，多个ID用逗号分隔</param>
        /// <returns>学期数据列表</returns>
        /// <response code="200">成功获取学期数据</response>
        /// <response code="500">服务器内部错误</response>
        /// <remarks>
        /// 示例请求：
        /// GET /Score/Semester?studentId=123456
        /// </remarks>
        [HttpGet("Semester")]
        [ProducesResponseType(typeof(SemesterResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<SemesterResult>> ParseSemester(string? studentId)
        {
            try
        {
            logger.LogInformation("开始解析学期数据");
            var cookie = Request.Headers.Cookie.ToString();
            if (string.IsNullOrEmpty(cookie))
            {
                cookie = Request.Headers["xauat"].ToString();
            }

            var result = await scoreService.ParseSemesterAsync(studentId, cookie);
            return result;
        }
        catch (Exceptions.UnAuthenticationError)
        {
            return Unauthorized("认证失败，请重新登录");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "解析学期数据时出错");
            return StatusCode(500, new { error = ex.Message });
        }
        }

        /// <summary>
        /// 获取当前学期
        /// 获取学生当前所在的学期信息
        /// </summary>
        /// <returns>当前学期信息</returns>
        /// <response code="200">成功获取当前学期</response>
        /// <response code="500">服务器内部错误</response>
        /// <remarks>
        /// 示例请求：
        /// GET /Score/ThisSemester
        /// </remarks>
        [HttpGet("ThisSemester")]
        [ProducesResponseType(typeof(SemesterItem), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<SemesterItem>> GetThisSemester()
        {
            try
        {
            var cookie = Request.Headers.Cookie.ToString();
            if (string.IsNullOrEmpty(cookie))
            {
                cookie = Request.Headers["xauat"].ToString();
            }

            return await scoreService.GetThisSemesterAsync(cookie);
        }
        catch (Exceptions.UnAuthenticationError)
        {
            return Unauthorized("认证失败，请重新登录");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "获取当前学期时出错");
            return StatusCode(500, new { error = ex.Message });
        }
        }

        /// <summary>
        /// 获取学生成绩
        /// 根据学生ID和学期获取学生的成绩列表
        /// </summary>
        /// <param name="studentId">学生ID，多个ID用逗号分隔</param>
        /// <param name="semester">学期代码，如2024-2025-2</param>
        /// <returns>成绩列表</returns>
        /// <response code="200">成功获取成绩列表</response>
        /// <response code="400">参数错误</response>
        /// <response code="500">服务器内部错误</response>
        /// <remarks>
        /// 示例请求：
        /// GET /Score?studentId=123456&semester=2024-2025-2
        /// </remarks>
        [HttpGet]
        [ProducesResponseType(typeof(List<ScoreResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<List<ScoreResponse>>> GetScore(string studentId, string semester)
        {
            try
        {
            logger.LogInformation("开始获取考试分数");
            var cookie = Request.Headers.Cookie.ToString();
            if (string.IsNullOrEmpty(cookie))
            {
                cookie = Request.Headers["xauat"].ToString();
            }

            var scores = await scoreService.GetScoresAsync(studentId, semester, cookie);
            return scores;
        }
        catch (ArgumentNullException ex)
        {
            logger.LogWarning(ex, "参数错误");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exceptions.UnAuthenticationError)
        {
            return Unauthorized("认证失败，请重新登录");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "获取成绩时出错");
            return StatusCode(500, new { error = ex.Message });
        }
        }
    }
}
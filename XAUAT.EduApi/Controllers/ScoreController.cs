using EduApi.Data.Models;
using Microsoft.AspNetCore.Mvc;
using XAUAT.EduApi.Services;

namespace XAUAT.EduApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ScoreController(ILogger<ScoreController> logger, IScoreService scoreService)
        : ControllerBase
    {
        [HttpGet("Semester")]
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
            catch (Exception ex)
            {
                logger.LogError(ex, "解析学期数据时出错");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("ThisSemester")]
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
            catch (Exception ex)
            {
                logger.LogError(ex, "获取当前学期时出错");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet]
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
            catch (Exception ex)
            {
                logger.LogError(ex, "获取成绩时出错");
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
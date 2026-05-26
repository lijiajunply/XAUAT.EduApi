using EduApi.Data.Models;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using XAUAT.EduApi.Extensions;
using XAUAT.EduApi.Filters;
using XAUAT.EduApi.Localization;
using XAUAT.EduApi.Services;

namespace XAUAT.EduApi.Controllers.V1;

[ApiController]
[Route("v1/score")]
[Produces("application/json")]
[Consumes("application/json")]
[ServiceFilter(typeof(EduCrawlerRateLimitFilter))]
[EnableRateLimiting("EduCrawler")]
public class ScoreController(
    ILogger<ScoreController> logger,
    IScoreService scoreService,
    ILanguageResolver languageResolver,
    IApiMessageLocalizer messageLocalizer)
    : V1ControllerBase(languageResolver, messageLocalizer)
{
    [HttpGet("Semester")]
    [ProducesResponseType(typeof(ApiResponse<SemesterResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<SemesterResult>>> ParseSemester(string? studentId)
    {
        try
        {
            logger.LogInformation("开始解析学期数据");
            var cookie = Request.GetEduAuthCookie();
            var result = await scoreService.ParseSemesterAsync(studentId, cookie, Language);
            return Ok(SuccessResponse(result));
        }
        catch (Exceptions.StudentCooldownException)
        {
            return RateLimited(ApiMessageKey.EduSystemRateLimited);
        }
        catch (Exceptions.UnAuthenticationError)
        {
            return Unauthorized(ErrorResponse(ApiCodes.AuthFailed,
                Message(ApiMessageKey.AuthenticationFailed)));
        }
        catch (Exceptions.RateLimitException)
        {
            return RateLimited(ApiMessageKey.EduSystemRateLimited);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "解析学期数据时出错");
            return StatusCode(StatusCodes.Status500InternalServerError,
                ErrorResponse(ApiCodes.InternalError, ex.Message));
        }
    }

    [HttpGet("ThisSemester")]
    [ProducesResponseType(typeof(ApiResponse<SemesterItem>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<SemesterItem>>> GetThisSemester()
    {
        try
        {
            var cookie = Request.GetEduAuthCookie();
            var resolvedStudentId = HttpContext.GetResolvedStudentIds()
                .FirstOrDefault(x => !x.StartsWith("cookie:", StringComparison.Ordinal));
            var result = await scoreService.GetThisSemesterAsync(cookie, Language, resolvedStudentId);
            return Ok(SuccessResponse(result));
        }
        catch (Exceptions.StudentCooldownException)
        {
            return RateLimited(ApiMessageKey.EduSystemRateLimited);
        }
        catch (Exceptions.UnAuthenticationError)
        {
            return Unauthorized(ErrorResponse(ApiCodes.AuthFailed,
                Message(ApiMessageKey.AuthenticationFailed)));
        }
        catch (Exceptions.RateLimitException)
        {
            return RateLimited(ApiMessageKey.EduSystemRateLimited);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "获取当前学期时出错");
            return StatusCode(StatusCodes.Status500InternalServerError,
                ErrorResponse(ApiCodes.InternalError, ex.Message));
        }
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<ScoreItem>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<ScoreItem>>>> GetScore(string studentId, string semester)
    {
        try
        {
            logger.LogInformation("开始获取考试分数");
            var cookie = Request.GetEduAuthCookie();
            var scores = await scoreService.GetScoresAsync(studentId, semester, cookie, Language);
            var items = scores.Select(s => new ScoreItem
            {
                Name = s.Name,
                LessonCode = s.LessonCode,
                LessonName = s.LessonName,
                Grade = s.Grade,
                Gpa = s.Gpa,
                GradeDetail = s.GradeDetail,
                Credit = s.Credit,
                IsMinor = s.IsMinor
            }).ToList();
            return Ok(SuccessListResponse(items));
        }
        catch (Exceptions.StudentCooldownException)
        {
            return RateLimited(ApiMessageKey.EduSystemRateLimited);
        }
        catch (ArgumentNullException ex)
        {
            logger.LogWarning(ex, "参数错误");
            return BadRequest(ErrorResponse(ApiCodes.ParamError, ex.Message));
        }
        catch (Exceptions.UnAuthenticationError)
        {
            return Unauthorized(ErrorResponse(ApiCodes.AuthFailed,
                Message(ApiMessageKey.AuthenticationFailed)));
        }
        catch (Exceptions.RateLimitException)
        {
            return RateLimited(ApiMessageKey.EduSystemRateLimited);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "获取成绩时出错");
            return StatusCode(StatusCodes.Status500InternalServerError,
                ErrorResponse(ApiCodes.InternalError, ex.Message));
        }
    }
}

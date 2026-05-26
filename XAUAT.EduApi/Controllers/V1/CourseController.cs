using EduApi.Data.Models;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using XAUAT.EduApi.Extensions;
using XAUAT.EduApi.Filters;
using XAUAT.EduApi.Localization;
using XAUAT.EduApi.Services;

namespace XAUAT.EduApi.Controllers.V1;

[ApiController]
[Route("v1/course")]
[Produces("application/json")]
[Consumes("application/json")]
[ServiceFilter(typeof(EduCrawlerRateLimitFilter))]
[EnableRateLimiting("EduCrawler")]
public class CourseController(
    ILogger<CourseController> logger,
    ICourseService courseService,
    ILanguageResolver languageResolver,
    IApiMessageLocalizer messageLocalizer)
    : V1ControllerBase(languageResolver, messageLocalizer)
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<CourseActivity>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<ApiResponse<List<CourseActivity>>>> GetCourse(string studentId)
    {
        try
        {
            logger.LogInformation("开始获取课程信息");
            var cookie = Request.GetEduAuthCookie();
            var courses = await courseService.GetCoursesAsync(studentId, cookie, Language);
            return Ok(SuccessListResponse(courses));
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
        catch (ArgumentNullException ex)
        {
            logger.LogWarning(ex, "参数错误");
            return BadRequest(ErrorResponse(ApiCodes.ParamError, ex.Message));
        }
        catch (Exceptions.RateLimitException)
        {
            return RateLimited(ApiMessageKey.EduSystemRateLimited);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "HTTP请求错误");
            return StatusCode(StatusCodes.Status502BadGateway,
                ErrorResponse(ApiCodes.UpstreamError, ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "操作无效");
            return NotFound(ErrorResponse(ApiCodes.NotFound, ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "获取课程时发生错误");
            return StatusCode(StatusCodes.Status500InternalServerError,
                ErrorResponse(ApiCodes.InternalError, Message(ApiMessageKey.InternalServerError)));
        }
    }
}

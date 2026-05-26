using EduApi.Data.Models;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using XAUAT.EduApi.Extensions;
using XAUAT.EduApi.Filters;
using XAUAT.EduApi.Localization;
using XAUAT.EduApi.Services;

namespace XAUAT.EduApi.Controllers.V1;

[ApiController]
[Route("v1/exam")]
[Produces("application/json")]
[Consumes("application/json")]
[ServiceFilter(typeof(EduCrawlerRateLimitFilter))]
[EnableRateLimiting("EduCrawler")]
public class ExamController(
    IExamService examService,
    ILogger<ExamController> logger,
    ILanguageResolver languageResolver,
    IApiMessageLocalizer messageLocalizer)
    : V1ControllerBase(languageResolver, messageLocalizer)
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<ExamInfo>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<ExamInfo>>>> GetExamArrangements(string? studentId)
    {
        try
        {
            var cookie = Request.GetEduAuthCookie();
            var requestStudentIds = HttpContext.GetResolvedStudentIds();
            var result = await examService.GetExamArrangementsAsync(cookie, studentId, Language, requestStudentIds);
            if (result.Error != null)
                return Ok(ErrorResponse(ApiCodes.DataFetchFailed, result.Error));
            return Ok(SuccessListResponse(result.Exams));
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
            logger.LogError(ex, "Failed to get exam arrangements");
            return StatusCode(StatusCodes.Status500InternalServerError,
                ErrorResponse(ApiCodes.DataFetchFailed, Message(ApiMessageKey.ExamFetchFailed)));
        }
    }
}

using EduApi.Data.Models;
using Microsoft.AspNetCore.Mvc;
using XAUAT.EduApi.Localization;
using XAUAT.EduApi.Services;

namespace XAUAT.EduApi.Controllers.V1;

[ApiController]
[Route("v1/schoolnav")]
[Produces("application/json")]
public class SchoolNavV1Controller(
    ISchoolNavService service,
    ILanguageResolver languageResolver,
    IApiMessageLocalizer messageLocalizer) : V1ControllerBase(languageResolver, messageLocalizer)
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<CategoryModel>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<List<CategoryModel>>>> GetCategoriesAsync()
    {
        try
        {
            var result = await service.GetCategoriesAsync();
            return Ok(SuccessListResponse(result));
        }
        catch (Exception)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                ErrorResponse(ApiCodes.InternalError, "获取校园导航数据失败"));
        }
    }
}

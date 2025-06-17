using Microsoft.AspNetCore.Mvc;
using XAUAT.EduApi.Services;

namespace XAUAT.EduApi.Controllers;

[ApiController]
[Route("[controller]")]
public class ProgramController(
    ILogger<ProgramController> logger,
    IProgramService program) : ControllerBase
{
    [HttpGet]
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
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get training program");
            return StatusCode(500, "获取培养方案失败");
        }
    }

    [HttpGet("GetDic")]
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
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get training program");
            return StatusCode(500, "获取培养方案失败");
        }
    }
}
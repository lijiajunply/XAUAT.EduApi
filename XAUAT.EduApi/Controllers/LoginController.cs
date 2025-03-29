using Microsoft.AspNetCore.Mvc;
using XAUAT.EduApi.Models;
using XAUAT.EduApi.Services;

namespace XAUAT.EduApi.Controllers;

[ApiController]
[Route("[controller]")]
public class LoginController(ILoginService loginService, ILogger<LoginController> logger)
    : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            return Ok(await loginService.LoginAsync(request.Username, request.Password));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Login failed");
            return StatusCode(500, "教务处系统访问失败，请联系管理员");
        }
    }
}
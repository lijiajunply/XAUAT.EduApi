using EduApi.Data.Models;
using Microsoft.AspNetCore.Mvc;
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
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest("用户名或密码不能为空");
            }
            
            var result = await loginService.LoginAsync(request.Username, request.Password);
            logger.LogInformation("用户 {Username} 登录成功", request.Username);
            return Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "用户 {Username} 登录失败", request.Username);
            return StatusCode(500, "教务处系统访问失败，请联系管理员");
        }
    }
}
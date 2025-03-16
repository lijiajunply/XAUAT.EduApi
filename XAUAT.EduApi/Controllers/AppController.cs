using Microsoft.AspNetCore.Mvc;

namespace XAUAT.EduApi.Controllers;

[ApiController]
[Route("[controller]")]
public class AppController(IHttpClientFactory httpClientFactory)
    : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetTag(string? token)
    {
        if (string.IsNullOrEmpty(token))
        {
            token = Environment.GetEnvironmentVariable("GITEE_ACCESS_TOKEN", EnvironmentVariableTarget.Process);
        }

        using var client = httpClientFactory.CreateClient();
        var response = await client.GetAsync(
            $"https://gitee.com/api/v5/repos/luckyfishisdashen/iOSClub.AppMobile/releases?access_token={token}&page=1&per_page=1&direction=desc");

        if (!response.IsSuccessStatusCode)
        {
            return StatusCode((int)response.StatusCode, "获取标签失败");
        }

        var jsonString = await response.Content.ReadAsStringAsync();
        return Ok(jsonString);
    }
}
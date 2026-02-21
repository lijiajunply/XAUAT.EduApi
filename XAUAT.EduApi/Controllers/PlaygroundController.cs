using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using XAUAT.EduApi.Services;

namespace XAUAT.EduApi.Controllers;

[ApiController]
[Route("[controller]")]
[Produces("application/json")]
[Consumes("application/json")]
public class PlaygroundController(IHttpClientFactory httpClientFactory) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var cookie = Request.Headers.Cookie.ToString();
        if (string.IsNullOrEmpty(cookie) || cookie.StartsWith("Rider"))
        {
            cookie = Request.Headers["xauat"].ToString(); // 从请求中获取 cookie
        }
        
        using var client = httpClientFactory.CreateClient();
        client.SetRealisticHeaders();
        client.Timeout = TimeSpan.FromSeconds(6); // 添加超时控制
        client.DefaultRequestHeaders.Add("Cookie", cookie);
        var response = await client.GetAsync("https://swjw.xauat.edu.cn/student/for-std/credit-certification-apply/other_apply/get-all-course-module?programId=3241");
        var content = await response.Content.ReadAsStringAsync();
        return Ok(JObject.Parse(content));
    }
}
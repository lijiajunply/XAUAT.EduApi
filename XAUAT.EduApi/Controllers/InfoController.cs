using Microsoft.AspNetCore.Mvc;
using XAUAT.EduApi.Services;

namespace XAUAT.EduApi.Controllers;

[ApiController]
[Route("[controller]")]
public class InfoController(IHttpClientFactory httpClientFactory, ILogger<CourseController> logger, IInfoService info)
    : ControllerBase
{
    //https://swjw.xauat.edu.cn/student/ws/student/home-page/programCompletionPreview
    [HttpGet("Completion")]
    public async Task<IActionResult> GetCompletion()
    {
        logger.LogInformation("开始抓取学业进度");
        var cookie = Request.Headers.Cookie.ToString();
        if (string.IsNullOrEmpty(cookie))
        {
            cookie = Request.Headers["xauat"].ToString(); // 从请求中获取 cookie
        }

        using var client = httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(6); // 添加超时控制
        client.DefaultRequestHeaders.Add("Cookie", cookie);
        var response =
            await client.GetAsync("https://swjw.xauat.edu.cn/student/ws/student/home-page/programCompletionPreview");
        var content = await response.Content.ReadAsStringAsync();
        return Content(content);
    }

    [HttpGet("Time")]
    public ActionResult GetTime()
    {
        return Ok(info.GetTime());
    }
}
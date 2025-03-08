using Microsoft.AspNetCore.Mvc;

namespace XAUAT.EduApi.Controllers;

[ApiController]
[Route("[controller]")]
public class InfoController(IHttpClientFactory httpClientFactory, ILogger<CourseController> logger)
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

        var client = httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("Cookie", cookie);
        var response =
            await client.GetAsync("https://swjw.xauat.edu.cn/student/ws/student/home-page/programCompletionPreview");
        var content = await response.Content.ReadAsStringAsync();
        return Content(content);
    }

    [HttpGet("Time")]
    public ActionResult GetTime()
    {
        return Ok(new { StartTime = "2025-02-23", EndTime = "2025-07-19" });
    }
}
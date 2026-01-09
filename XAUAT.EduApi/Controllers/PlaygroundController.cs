using Microsoft.AspNetCore.Mvc;

namespace XAUAT.EduApi.Controllers;

[ApiController]
[Route("[controller]")]
[Produces("application/json")]
[Consumes("application/json")]
public class PlaygroundController(IHttpClientFactory httpClientFactory) : ControllerBase
{
    [HttpGet("{url}")]
    public async Task<IActionResult> Get(string url)
    {
        var cookie = Request.Headers.Cookie.ToString();
        if (string.IsNullOrEmpty(cookie))
        {
            cookie = Request.Headers["xauat"].ToString(); // 从请求中获取 cookie
        }
        
        using var client = httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("Cookie", cookie);
        var response = await client.GetAsync(url);
        return Ok(await response.Content.ReadAsStringAsync());
    }
}
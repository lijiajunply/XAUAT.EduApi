using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using XAUAT.EduApi.Extensions;
using XAUAT.EduApi.Filters;
using XAUAT.EduApi.Services;

namespace XAUAT.EduApi.Controllers;

[ApiController]
[Route("[controller]")]
[Produces("application/json")]
[Consumes("application/json")]
public class PlaygroundController(IHttpClientFactory httpClientFactory) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(JObject), StatusCodes.Status200OK)]
    [ServiceFilter(typeof(EduCrawlerRateLimitFilter))]
    [EnableRateLimiting("EduCrawler")]
    public async Task<ActionResult<JObject>> Get()
    {
        var cookie = Request.GetEduAuthCookie();
        var studentIds = HttpContext.GetResolvedStudentIds();

        var services = HttpContext.RequestServices;
        var rateLimitExecutor = (services is null ? null : services.GetService<IStudentRateLimitExecutor>())
                                ?? NoOpStudentRateLimitExecutor.Instance;

        var result = await rateLimitExecutor
            .ExecuteAsync(studentIds, async () =>
            {
                using var client = httpClientFactory.CreateClient();
                client.SetRealisticHeaders();
                client.Timeout = TimeSpan.FromSeconds(6);
                client.DefaultRequestHeaders.Add("Cookie", cookie);
                var response = await client.GetAsync(
                    "https://swjw.xauat.edu.cn/student/for-std/credit-certification-apply/other_apply/get-all-course-module?programId=3241");
                var content = await response.Content.ReadAsStringAsync();
                content.ThrowIfRateLimited();
                return JObject.Parse(content);
            });

        return Ok(result);
    }
}

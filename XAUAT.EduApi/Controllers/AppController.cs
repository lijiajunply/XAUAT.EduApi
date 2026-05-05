using EduApi.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace XAUAT.EduApi.Controllers;

/// <summary>
/// 应用更新控制器
/// 提供移动端应用版本检测和更新功能
/// </summary>
[ApiController]
[Route("[controller]")]
[Produces("application/json")]
public class AppController(IHttpClientFactory httpClientFactory)
    : ControllerBase
{
    /// <summary>
    /// 获取最新应用版本
    /// 从Gitee仓库获取移动端应用的最新发布版本信息
    /// </summary>
    /// <param name="token">可选，Gitee API访问令牌</param>
    /// <returns>最新版本信息，包含版本号、发布时间、更新日志等</returns>
    /// <response code="200">成功获取最新版本信息</response>
    /// <response code="401">未授权，Gitee API令牌无效</response>
    /// <response code="500">服务器内部错误，无法连接到Gitee API</response>
    /// <remarks>
    /// 示例请求：
    /// GET /App?token=YOUR_GITEE_TOKEN
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
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

    /// <summary>
    /// 获取最新应用版本
    /// 从Gitee仓库获取移动端应用的最新发布版本信息
    /// </summary>
    /// <returns>最新版本信息，包含版本号、发布时间、更新日志等</returns>
    /// <response code="200">成功获取最新版本信息</response>
    /// <response code="401">未授权，Gitee API令牌无效</response>
    /// <response code="500">服务器内部错误，无法连接到Gitee API</response>
    /// <remarks>
    /// 示例请求：
    /// GET /App?token=YOUR_GITEE_TOKEN
    /// </remarks>
    [HttpGet("GetTag")]
    [ProducesResponseType(typeof(List<ReleaseInfo>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetTagModel()
    {
        using var client = httpClientFactory.CreateClient();
        var response = await client.GetAsync(
            "https://appapi.xauat.site/api/App/5f278ffc-5a70-4805-a6bf-0543040981a8/latest?channelId=9e1a198a-a0c2-4017-b492-f2d0e5bee437");

        if (!response.IsSuccessStatusCode)
        {
            return StatusCode((int)response.StatusCode, "获取标签失败");
        }

        var jsonString = await response.Content.ReadAsStringAsync();

        var obj = JObject.Parse(jsonString);

        var result = new List<ReleaseInfo>
        {
            new()
            {
                TagName = obj["releaseId"]?.ToObject<string>(),
                Body = obj["context"]?.ToObject<string>(),
                Assets =
                [
                    new AssetInfo()
                    {
                        Name = obj["softs"]?[0]?["name"]?.ToObject<string>(),
                        BrowserDownloadUrl = obj["softs"]?[0]?["url"]?.ToObject<string>()
                    }
                ]
            }
        };


        return Ok(result);
    }
}
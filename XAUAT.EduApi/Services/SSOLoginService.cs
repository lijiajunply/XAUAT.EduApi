using Newtonsoft.Json.Linq;

namespace XAUAT.EduApi.Services;

public class SSOLoginService(IHttpClientFactory httpClientFactory, CookieCodeService cookieCode) : ILoginService
{
    public async Task<object> LoginAsync(string username, string password)
    {
        using var httpClient = httpClientFactory.CreateClient();

        var response = await httpClient.GetAsync($"https://xauatlogin.zeabur.app/login/{username}/{password}");
        var content = await response.Content.ReadAsStringAsync();
        var json = JObject.Parse(content);
        if (!json["success"]!.ToObject<bool>()) throw new Exception("Login failed");
        var cookies = json["cookies"]!.ToObject<string>() ?? "";
        var studentId = await cookieCode.GetCode(cookies);
        return new { Success = true, StudentId = studentId, Cookie = cookies };
    }
}
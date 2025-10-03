using EduApi.Data;
using EduApi.Data.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;

namespace XAUAT.EduApi.Services;

public class SSOLoginService(
    IHttpClientFactory httpClientFactory,
    CookieCodeService cookieCode,
    EduContext context) : ILoginService
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

        if (await context.Users.AnyAsync(u => u.Id == studentId))
            return new { Success = true, StudentId = studentId, Cookie = cookies };
        context.Users.Add(new UserModel
        {
            Id = studentId,
            Username = username,
            Password = DataTool.StringToHash(password)
        });

        await context.SaveChangesAsync();

        return new { Success = true, StudentId = studentId, Cookie = cookies };
    }
}
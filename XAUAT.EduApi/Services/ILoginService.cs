using System.Text;
using System.Text.Json;
using EduApi.Data;
using EduApi.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace XAUAT.EduApi.Services;

public interface ILoginService
{
    Task<object> LoginAsync(string username, string password);
}

public class LoginService(
    IHttpClientFactory httpClientFactory,
    ICodeService codeService,
    CookieCodeService cookieCode,
    EduContext context)
    : ILoginService
{
    public async Task<object> LoginAsync(string username, string password)
    {
        using var httpClient = httpClientFactory.CreateClient();
        // 获取 salt
        var saltResponse = await httpClient.GetAsync("https://swjw.xauat.edu.cn/student/login-salt");
        if (!saltResponse.IsSuccessStatusCode)
            throw new Exception("Failed to get salt");

        var salt = await saltResponse.Content.ReadAsStringAsync();
        var cookies = cookieCode.ParseCookie(saltResponse.Headers.GetValues("Set-Cookie"));

        // 准备登录参数
        var loginParams = new
        {
            salt,
            username,
            password
        };

        var encodedParams = codeService.Encode(loginParams); // 需要实现对应的加密方法

        // 发送登录请求
        var request = new HttpRequestMessage(HttpMethod.Post, "https://swjw.xauat.edu.cn/student/login");
        request.Headers.Add("Cookie", cookies);

        request.Content = new StringContent(
            JsonSerializer.Serialize(encodedParams),
            Encoding.UTF8,
            "application/json");

        var loginResponse = await httpClient.SendAsync(request);

        if (!loginResponse.IsSuccessStatusCode) throw new Exception("Login failed");
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
using System.Text;
using System.Text.Json;
using EduApi.Data;
using EduApi.Data.Models;
using Microsoft.EntityFrameworkCore;
using Polly;

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
        var retryPolicy = Policy
            .Handle<HttpRequestException>()
            .Or<TaskCanceledException>()
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

        return await retryPolicy.ExecuteAsync(async () =>
        {
            using var httpClient = httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(5); // 5秒超时

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

            var user = await context.Users.FirstOrDefaultAsync(u => u.Id == studentId);

            if (user != null)
            {
                user.Username = username;
                user.Password = DataTool.StringToHash(password);
                return new { Success = true, StudentId = studentId, Cookie = cookies };
            }

            context.Users.Add(new UserModel
            {
                Id = studentId,
                Username = username,
                Password = DataTool.StringToHash(password)
            });

            await context.SaveChangesAsync();

            return new { Success = true, StudentId = studentId, Cookie = cookies };
        });
    }
}
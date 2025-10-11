using System.Net;
using EduApi.Data;
using EduApi.Data.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using Polly;

namespace XAUAT.EduApi.Services;

public class SSOLoginService(
    IHttpClientFactory httpClientFactory,
    CookieCodeService cookieCode,
    EduContext context,
    ILogger<SSOLoginService> logger) : ILoginService
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

            var response = await httpClient.GetAsync($"https://schedule.xauat.site/login/{username}/{password}");
            var content = await response.Content.ReadAsStringAsync();

            // 检查响应状态
            if (response.StatusCode != HttpStatusCode.OK)
            {
                logger.LogError("登录请求失败，状态码: {StatusCode}", response.StatusCode);
                throw new Exception($"登录请求失败，状态码: {response.StatusCode}");
            }

            var json = JObject.Parse(content);
            if (!json["success"]!.ToObject<bool>())
            {
                logger.LogWarning("用户 {Username} 登录失败，教务系统返回失败", username);
                throw new Exception("Login failed");
            }

            var cookies = json["cookies"]!.ToObject<string>() ?? "";

            string studentId;

            // 检查用户是否已存在
            var existingUser = await context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (existingUser != null)
            {
                if (string.IsNullOrEmpty(existingUser.Id))
                    studentId = await cookieCode.GetCode(cookies);
                else studentId = existingUser.Id;
                return new { Success = true, StudentId = studentId, Cookie = cookies };
            }

            studentId = await cookieCode.GetCode(cookies);

            // 创建新用户
            var newUser = new UserModel
            {
                Id = studentId,
                Username = username
            };

            context.Users.Add(newUser);
            await context.SaveChangesAsync();

            logger.LogInformation("新用户 {Username} 创建成功，ID: {StudentId}", username, studentId);

            return new { Success = true, StudentId = studentId, Cookie = cookies };
        });
    }
}
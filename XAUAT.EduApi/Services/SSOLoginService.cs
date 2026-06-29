using System.Net;
using EduApi.Data.Models;
using Newtonsoft.Json.Linq;
using Polly;
using XAUAT.EduApi.Exceptions;
using XAUAT.EduApi.Interfaces;

namespace XAUAT.EduApi.Services;

// ReSharper disable once InconsistentNaming
public class SSOLoginService(
    IHttpClientFactory httpClientFactory,
    ICookieCodeService cookieCode,
    ILogger<SSOLoginService> logger,
    ITestAccountResolver? testAccountResolver = null) : ILoginService
{
    public async Task<LoginResponse> LoginAsync(string username, string password, string language = "zh")
    {
        if (testAccountResolver?.IsTestLogin(username, password) == true)
        {
            logger.LogInformation("用户 {Username} 命中测试账号登录", username);
            return testAccountResolver.CreateLoginResponse();
        }

        var retryPolicy = Policy
            .Handle<HttpRequestException>()
            .Or<TaskCanceledException>()
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

        return await retryPolicy.ExecuteAsync(async () =>
        {
            using var httpClient = httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(15); // 5秒超时

            var encodedUsername = Uri.EscapeDataString(username);
            var encodedPassword = Uri.EscapeDataString(password);
            var response = await httpClient.GetAsync($"https://schedule.xauat.site/login/{encodedUsername}/{encodedPassword}");
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
                throw new LoginFailedException();
            }

            var cookies = json["cookies"]!.ToObject<string>() ?? "";

            var studentId = await cookieCode.GetCode(cookies);
            if (string.IsNullOrEmpty(studentId) || studentId == "/student/login")
            {
                logger.LogWarning("用户 {Username} 登录失败，用户 Id {StudentId}", username, studentId);
                throw new LoginFailedException();
            }

            
            logger.LogInformation("用户 {Username} 登录成功", username);
            return new LoginResponse
            {
                Success = true,
                StudentId = studentId,
                Cookie = cookies
            };
        });
    }
}

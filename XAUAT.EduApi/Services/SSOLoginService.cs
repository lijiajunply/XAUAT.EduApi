using EduApi.Data;
using EduApi.Data.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using Polly;

namespace XAUAT.EduApi.Services;

public class SSOLoginService(
    IHttpClientFactory httpClientFactory,
    CookieCodeService cookieCode,
    EduContext context) : ILoginService
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
        });
    }
}
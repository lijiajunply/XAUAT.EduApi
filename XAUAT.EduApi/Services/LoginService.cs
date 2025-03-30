using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace XAUAT.EduApi.Services;

public partial class LoginService(HttpClient httpClient, ICodeService codeService) : ILoginService
{
    public async Task<object> LoginAsync(string username, string password)
    {
        // 获取 salt
        var saltResponse = await httpClient.GetAsync("https://swjw.xauat.edu.cn/student/login-salt");
        if (!saltResponse.IsSuccessStatusCode)
            throw new Exception("Failed to get salt");

        var salt = await saltResponse.Content.ReadAsStringAsync();
        var cookies = ParseCookie(saltResponse.Headers.GetValues("Set-Cookie"));

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
        var studentId = await GetCode(cookies);
        return new { Success = true, StudentId = studentId, Cookie = cookies };
    }

    private async Task<string> GetCode(string cookies)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://swjw.xauat.edu.cn/student/for-std/precaution");
        request.Headers.Add("Cookie", cookies);

        var response = await httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            return "";
        }

        var a = request.RequestUri!.LocalPath.Replace("/student/precaution/index/", "");

        if (a != "/student/for-std/precaution") return a;
        var Content = await response.Content.ReadAsStringAsync();

        var matches = MyRegex().Matches(Content);
        return matches.Count >= 1 ? string.Join(';', matches.Select(m => m.Groups[1].Value)) : "";
    }

    private static string ParseCookie(IEnumerable<string> cookies)
    {
        var result = new StringBuilder();

        foreach (var cookie in cookies)
        {
            if (cookie.Contains("__pstsid__"))
            {
                result.Append(cookie).Append(';');
            }
            else if (cookie.Contains("SESSION"))
            {
                var sessionValue = cookie.Split('=')[1].Split(';')[0];
                result.Append("SESSION=").Append(sessionValue).Append(';');
            }
        }

        return result.ToString();
    }

    [GeneratedRegex("""value="(.*?)">""")]
    private static partial Regex MyRegex();
}
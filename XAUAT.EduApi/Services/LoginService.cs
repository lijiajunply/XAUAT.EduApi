using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace XAUAT.EduApi.Services;

public class LoginService(HttpClient httpClient, ICodeService codeService) : ILoginService
{
    public async Task<object> LoginAsync(string username, string password)
    {
        // 获取 salt
        var user = RandomBrowserUa();
        var headers = new HttpClient().DefaultRequestHeaders;
        headers.TryAddWithoutValidation("User-Agent", user);
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
        request.Headers.Add("User-Agent", user);
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
        var request = new HttpRequestMessage(HttpMethod.Get, "https://swjw.xauat.edu.cn/student/for-std/program/");
        request.Headers.Add("Cookie", cookies);

        var response = await httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            return "";

        var content = await response.Content.ReadAsStringAsync();
        var match = Regex.Match(content, @"/student/for-std/program/info-en/(.*?)'");

        return match.Success ? match.Groups[1].Value : "";
    }

    private string ParseCookie(IEnumerable<string> cookies)
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

    private static string RandomBrowserUa()
    {
        string[] ua =
        [
            "Mozilla/5.0 (Windows NT 6.1; rv,2.0.1) Gecko/20100101 Firefox/4.0.1",
            "Opera/9.80 (Windows NT 6.1; U; en) Presto/2.8.131 Version/11.11",
            "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/70.0.3538.25 Safari/537.36 Core/1.70.3704.400 QQBrowser/10.4.3587.400",
            "Mozilla/4.0 (compatible; MSIE 7.0; Windows NT 5.1; 360SE)",
            "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/55.0.2883.87 UBrowser/6.2.4094.1 Safari/537.36",
            "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/72.0.3626.119 Safari/537.36"
        ];
        var rd = new Random();
        var index = rd.Next(0, ua.Length);
        return ua[index];
    }
}
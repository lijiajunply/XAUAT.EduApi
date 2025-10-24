using System.Text;
using System.Text.RegularExpressions;
using Polly;

namespace XAUAT.EduApi.Services;

public partial class CookieCodeService(IHttpClientFactory httpClientFactory)
{
    public async Task<string> GetCode(string cookies)
    {
        var retryPolicy = Policy
            .Handle<HttpRequestException>()
            .Or<TaskCanceledException>()
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

        return await retryPolicy.ExecuteAsync(async () =>
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "https://swjw.xauat.edu.cn/student/for-std/precaution");
            request.Headers.Add("Cookie", cookies);

            using var httpClient = httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(5); // 5秒超时
            var response = await httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                return "";
            }

            var a = request.RequestUri!.LocalPath.Replace("/student/precaution/index/", "");

            if (a != "/student/for-std/precaution") return a;
            var content = await response.Content.ReadAsStringAsync();

            var matches = MyRegex().Matches(content);
            return matches.Count >= 1 ? string.Join(',', matches.Select(m => m.Groups[1].Value)) : "";
        });
    }

    public string ParseCookie(IEnumerable<string> cookies)
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
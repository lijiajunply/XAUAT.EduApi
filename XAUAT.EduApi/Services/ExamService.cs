
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using XAUAT.EduApi.Models;

namespace XAUAT.EduApi.Services;

public class ExamService(HttpClient httpClient, ILogger<ExamService> logger) : IExamService
{
    private const string _baseUrl = "https://swjw.xauat.edu.cn";

    public async Task<ExamResponse> GetExamArrangementsAsync(string cookie)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/student/for-std/exam-arrange");
            request.Headers.Add("Cookie", cookie);

            var response = await httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            // 检查是否重定向到登录页面
            if (content.Contains("登入页面"))
            {
                return new ExamResponse
                {
                    Exams = new List<ExamInfo>(),
                    CanClick = false
                };
            }

            // 解析数据
            var match = Regex.Match(content, @"var studentExamInfoVms = (.*?)\];", RegexOptions.Singleline);
            if (!match.Success)
            {
                logger.LogWarning("Failed to match exam data pattern");
                return new ExamResponse
                {
                    Exams = [],
                    CanClick = true
                };
            }

            var jsonData = match.Groups[1].Value + "]";
            // 替换单引号为双引号
            jsonData = jsonData.Replace("'", "\"");

            var examData = JsonConvert.DeserializeObject<List<ExamDataRaw>>(jsonData);
            if (examData == null)
            {
                logger.LogWarning("Failed to deserialize exam data");
                return new ExamResponse
                {
                    Exams = [],
                    CanClick = true
                };
            }

            var result = new ExamResponse
            {
                Exams = examData.Select(d => new ExamInfo
                {
                    Name = d.Course.NameZh,
                    Time = d.ExamTime,
                    Location = d.Room,
                    Seat = d.SeatNo
                }).ToList(),
                CanClick = examData.Count == 0
            };

            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting exam arrangements");
            throw;
        }
    }
}
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using XAUAT.EduApi.DataModels;
using XAUAT.EduApi.Models;
using XAUAT.EduApi.Services;

namespace XAUAT.EduApi.Controllers;

[ApiController]
[Route("[controller]")]
public class ScoreController(IHttpClientFactory httpClientFactory, ILogger<CourseController> logger, IExamService exam)
    : ControllerBase
{
    [HttpGet("Semester")]
    public async Task<ActionResult<SemesterResult>> ParseSemester(string? studentId)
    {
        try
        {
            logger.LogInformation("开始抓取学期数据");
            var cookie = Request.Headers.Cookie.ToString();
            if (string.IsNullOrEmpty(cookie))
            {
                cookie = Request.Headers["xauat"].ToString(); // 从请求中获取 cookie
            }

            var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("Cookie", cookie);
            var url = "https://swjw.xauat.edu.cn/student/for-std/grade/sheet";
            if (!string.IsNullOrEmpty(studentId))
            {
                var split = studentId.Split(',');
                studentId = split.FirstOrDefault();
                url += $"/semester-index/{studentId}";
            }

            var html = await client.GetStringAsync(url);
            var result = new SemesterResult();
            result.Parse(html);
            return result;
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet("ThisSemester")]
    public async Task<ActionResult<SemesterItem>> GetThisSemester()
    {
        try
        {
            var cookie = Request.Headers.Cookie.ToString();
            if (string.IsNullOrEmpty(cookie))
            {
                cookie = Request.Headers["xauat"].ToString(); // 从请求中获取 cookie
            }

            return await exam.GetThisSemester(cookie, httpClientFactory);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpGet]
    public async Task<ActionResult<List<ScoreResponse>>> GetScore(string studentId, string semester)
    {
        try
        {
            logger.LogInformation("开始抓取考试分数");
            var cookie = Request.Headers.Cookie.ToString();
            if (string.IsNullOrEmpty(cookie))
            {
                cookie = Request.Headers["xauat"].ToString(); // 从请求中获取 cookie
            }

            if (string.IsNullOrEmpty(studentId) || string.IsNullOrEmpty(cookie))
            {
                return BadRequest("学号或Cookie不能为空");
            }

            var split = studentId.Split(',');
            var result = new List<ScoreResponse>();

            foreach (var s in split)
            {
                var scoreResponse = await GetScoreResponse(s, semester, cookie);
                result.AddRange(scoreResponse);
            }

            return result;
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// 获取单学期成绩
    /// </summary>
    /// <param name="studentId"></param>
    /// <param name="semester"></param>
    /// <param name="cookie"></param>
    /// <returns></returns>
    private async Task<List<ScoreResponse>> GetScoreResponse(string studentId, string semester, string cookie)
    {
        var client = httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("Cookie", cookie);

        var response = await client.GetAsync(
            $"https://swjw.xauat.edu.cn/student/for-std/grade/sheet/info/{studentId}?semester={semester}");

        if (!response.IsSuccessStatusCode)
        {
            return [];
        }

        var stream = await response.Content.ReadAsStringAsync();

        var json = JObject.Parse(stream);


        // 结果会是：

        // string[] { "期末成绩:70", "过程考核成绩:86.5(慕课成绩:75.96;作业:84.79;实验:99)" }

        return (json["semesterId2studentGrades"]?[semester]!).Select(item => new ScoreResponse()
            {
                Name = item["course"]!["nameZh"]!.ToString(),
                Credit = item["course"]!["credits"]!.ToString(),
                LessonCode = item["lessonCode"]!.ToString(),
                LessonName = item["lessonNameZh"]!.ToString(),
                Grade = item["gaGrade"]!.ToString(),
                Gpa = item["gp"]!.ToString(),
                GradeDetail = string.Join("; ",
                    Regex.Matches(item["gradeDetail"]!.ToString(), @"<span[^>]*>([^<]+)<\/span>")
                        .Select(m => m.Groups[1].Value.Trim()))
            })
            .ToList();
    }
}
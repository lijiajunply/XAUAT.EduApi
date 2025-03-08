using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using XAUAT.EduApi.DataModels;
using XAUAT.EduApi.Models;

namespace XAUAT.EduApi.Controllers;

[ApiController]
[Route("[controller]")]
public class ScoreController(IHttpClientFactory httpClientFactory, ILogger<CourseController> logger)
    : ControllerBase
{
    [HttpGet("Semester")]
    public async Task<ActionResult<SemesterResult>> ParseSemester()
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
            var html = await client.GetStringAsync("https://swjw.xauat.edu.cn/student/for-std/grade/sheet");
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
            logger.LogInformation("开始抓取学期数据");
            var cookie = Request.Headers.Cookie.ToString();
            if (string.IsNullOrEmpty(cookie))
            {
                cookie = Request.Headers["xauat"].ToString(); // 从请求中获取 cookie
            }

            var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("Cookie", cookie);
            var html = await client.GetStringAsync("https://swjw.xauat.edu.cn/student/for-std/course-table");
            var result = new SemesterResult();
            return Ok(result.ParseNow(html));
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

            var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("Cookie", cookie);

            var response = await client.GetAsync(
                $"https://swjw.xauat.edu.cn/student/for-std/grade/sheet/info/{studentId}?semester={semester}");

            if (!response.IsSuccessStatusCode)
            {
                return StatusCode((int)response.StatusCode, "获取分数数据失败");
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
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
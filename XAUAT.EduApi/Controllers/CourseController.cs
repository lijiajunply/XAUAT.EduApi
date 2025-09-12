using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using XAUAT.EduApi.Models;
using XAUAT.EduApi.Services;

namespace XAUAT.EduApi.Controllers;

[ApiController]
[Route("[controller]")]
public class CourseController(IHttpClientFactory httpClientFactory, ILogger<CourseController> logger, IExamService exam)
    : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetCourse(string studentId)
    {
        try
        {
            logger.LogInformation("开始抓取课程");
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

            var courses = new List<CourseActivity>();

            using var client = httpClientFactory.CreateClient();
            client.SetRealisticHeaders();
            client.DefaultRequestHeaders.Add("Cookie", cookie);

            var semester = await exam.GetThisSemester(cookie);

            if (string.IsNullOrEmpty(semester.Value))
            {
                return NotFound();
            }
            
            foreach (var a in split)
            {
                var response = await client.GetAsync(
                    $"https://swjw.xauat.edu.cn/student/for-std/course-table/semester/{semester.Value}/print-data/{a}");

                if (!response.IsSuccessStatusCode)
                {
                    return StatusCode((int)response.StatusCode, "获取课程数据失败");
                }

                var jsonString = await response.Content.ReadAsStringAsync();

                var jsonResponse = JsonConvert.DeserializeObject<CourseResponse>(jsonString);

                if (jsonResponse?.StudentTableVm == null)
                {
                    return NotFound("未找到课程数据");
                }

                courses.AddRange(jsonResponse.StudentTableVm.Activities);
            }

            if (courses == null! || courses.Count == 0) return NotFound(new { Success = false, Message = "未找到课程数据" });
            foreach (var item in courses)
            {
                item.WeekIndexes = item.WeekIndexes.OrderBy(x => x).ToList();
                item.Room = string.IsNullOrEmpty(item.Room) ? "未知" : item.Room.Replace("*", "");
            }

            // 返回处理后的课程数据
            return Ok(new
            {
                Success = true,
                Data = courses,
                ExpirationTime = DateTime.Now.AddDays(7)
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "获取课程时发生错误");
            return StatusCode(500, new { Success = false, Message = "服务器内部错误" });
        }
    }
}
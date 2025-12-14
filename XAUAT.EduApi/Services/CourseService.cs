using EduApi.Data.Models;
using Newtonsoft.Json;

namespace XAUAT.EduApi.Services;

public interface ICourseService
{
    Task<List<CourseActivity>> GetCoursesAsync(string studentId, string cookie);
}

public class CourseService(
    IHttpClientFactory httpClientFactory,
    ILogger<CourseService> logger,
    IExamService examService)
    : ICourseService
{
    public async Task<List<CourseActivity>> GetCoursesAsync(string studentId, string cookie)
    {
        logger.LogInformation("开始抓取课程");

        if (string.IsNullOrEmpty(studentId) || string.IsNullOrEmpty(cookie))
        {
            throw new Exception("学号或Cookie不能为空");
        }

        var split = studentId.Split(',');
        var courses = new List<CourseActivity>();

        using var client = httpClientFactory.CreateClient();
        client.SetRealisticHeaders();
        client.DefaultRequestHeaders.Add("Cookie", cookie);

        var semester = await examService.GetThisSemester(cookie);

        if (string.IsNullOrEmpty(semester.Value))
        {
            throw new InvalidOperationException("无法获取当前学期信息");
        }

        foreach (var a in split)
        {
            var response = await client.GetAsync(
                $"https://swjw.xauat.edu.cn/student/for-std/course-table/semester/{semester.Value}/print-data/{a}");

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"获取课程数据失败，状态码: {response.StatusCode}");
            }

            var jsonString = await response.Content.ReadAsStringAsync();
            if (jsonString.Contains("登入页面"))
            {
                throw new Exceptions.UnAuthenticationError();
            }
            var jsonResponse = JsonConvert.DeserializeObject<CourseResponse>(jsonString);

            if (jsonResponse?.StudentTableVm == null)
            {
                throw new InvalidOperationException("未找到课程数据");
            }

            courses.AddRange(jsonResponse.StudentTableVm.Activities);
        }

        if (courses == null! || courses.Count == 0)
        {
            throw new InvalidOperationException("未找到课程数据");
        }

        foreach (var item in courses)
        {
            item.WeekIndexes = item.WeekIndexes.OrderBy(x => x).ToList();
            item.Room = string.IsNullOrEmpty(item.Room) ? "未知" : item.Room.Replace("*", "");
        }

        return courses;
    }
}
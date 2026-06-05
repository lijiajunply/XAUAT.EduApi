using System.ComponentModel;
using ModelContextProtocol.Server;
using XAUAT.EduApi.Services;

namespace XAUAT.EduApi.Mcp.Tools;

[McpServerToolType]
public class CourseTool(ICourseService courseService)
{
    [McpServerTool, Description("获取学生课程表。需要提供学生学号和教务系统 Cookie。")]
    public async Task<object> GetCourse(
        [Description("学生学号")] string studentId,
        [Description("教务系统认证 Cookie（__pstsid__ 开头）")] string cookie)
    {
        var courses = await courseService.GetCoursesAsync(studentId, cookie);
        return courses;
    }
}

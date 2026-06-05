using System.ComponentModel;
using ModelContextProtocol.Server;
using XAUAT.EduApi.Services;

namespace XAUAT.EduApi.Mcp.Tools;

[McpServerToolType]
public class ExamTool(IExamService examService)
{
    [McpServerTool, Description("获取学生考试安排。需要提供教务系统 Cookie，可选学号。")]
    public async Task<object> GetExamArrangements(
        [Description("教务系统认证 Cookie")] string cookie,
        [Description("学生学号（可选）")] string? studentId = null)
    {
        var exams = await examService.GetExamArrangementsAsync(cookie, studentId);
        return exams;
    }
}

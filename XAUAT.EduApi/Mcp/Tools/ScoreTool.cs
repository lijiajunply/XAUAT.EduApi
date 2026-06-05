using System.ComponentModel;
using ModelContextProtocol.Server;
using XAUAT.EduApi.Services;

namespace XAUAT.EduApi.Mcp.Tools;

[McpServerToolType]
public class ScoreTool(IScoreService scoreService)
{
    [McpServerTool, Description("获取学生成绩列表。需要提供学生学号、学期和教务系统 Cookie。")]
    public async Task<object> GetScores(
        [Description("学生学号")] string studentId,
        [Description("学期，如 2024-2025-2")] string semester,
        [Description("教务系统认证 Cookie")] string cookie)
    {
        var scores = await scoreService.GetScoresAsync(studentId, semester, cookie);
        return scores;
    }

    [McpServerTool, Description("获取当前学期信息。")]
    public async Task<object> GetCurrentSemester(
        [Description("教务系统认证 Cookie")] string cookie)
    {
        var semester = await scoreService.GetThisSemesterAsync(cookie);
        return semester;
    }
}

using System.ComponentModel;
using ModelContextProtocol.Server;
using XAUAT.EduApi.Services;

namespace XAUAT.EduApi.Mcp.Tools;

[McpServerToolType]
public class ElectricityTool(IElectricityService electricityService)
{
    [McpServerTool, Description("查询宿舍电费余额。可传入充值页面 URL，留空则使用默认配置。")]
    public async Task<object> GetElectricityBalance(
        [Description("充值页面 URL（可选）")] string? url = null)
    {
        var balance = await electricityService.FetchCurrentBalanceAsync(url);
        var weekly = await electricityService.FetchWeeklyDataAsync(url);
        return new { balance, weekly };
    }
}

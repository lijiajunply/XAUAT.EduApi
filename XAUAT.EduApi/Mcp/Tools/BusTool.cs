using System.ComponentModel;
using ModelContextProtocol.Server;
using XAUAT.EduApi.Services;

namespace XAUAT.EduApi.Mcp.Tools;

[McpServerToolType]
public class BusTool(IBusService busService)
{
    [McpServerTool, Description("查询西建大校车时刻表（新数据平台）。loc 为出发地，如 'main'（主校区）或 'jinhua'（金花校区）。")]
    public async Task<object> GetBus(
        [Description("出发地点标识，如 main / jinhua")] string loc,
        [Description("查询时间，格式 HH:mm，留空则为当前时间")] string? time = null)
    {
        var result = await busService.GetBusFromNewDataAsync(time, loc, "zh");
        return result;
    }
}

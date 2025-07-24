namespace XAUAT.EduApi.Services;

public class InfoService : IInfoService
{
    public TimeModel GetTime()
    {
        var start = Environment.GetEnvironmentVariable("START", EnvironmentVariableTarget.Process);
        var end = Environment.GetEnvironmentVariable("END", EnvironmentVariableTarget.Process);
        return new TimeModel() { StartTime = start ?? "2025-02-23", EndTime = end ?? "2025-07-19" };
    }

    public bool IsInSchool()
    {
        var time = GetTime();
        return DateTime.Now >= DateTime.Parse(time.StartTime) && DateTime.Now <= DateTime.Parse(time.EndTime);
    }
}
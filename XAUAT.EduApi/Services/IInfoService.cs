namespace XAUAT.EduApi.Services;

public interface IInfoService
{
    TimeModel GetTime();
    bool IsInSchool();
}

[Serializable]
public class TimeModel
{
    public string StartTime { get; set; } = "";
    public string EndTime { get; set; } = "";
}
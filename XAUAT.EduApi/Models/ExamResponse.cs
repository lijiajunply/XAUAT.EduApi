namespace XAUAT.EduApi.Models;

[Serializable]
public class ExamInfo
{
    public string Name { get; set; } = "";
    public string Time { get; set; } = "";
    public string Location { get; set; } = "";
    public string Seat { get; set; } = "";
}

public class ExamResponse
{
    public List<ExamInfo> Exams { get; set; } = [];
    public bool CanClick { get; set; }
}
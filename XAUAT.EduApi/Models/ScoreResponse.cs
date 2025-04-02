namespace XAUAT.EduApi.Models;

public class ScoreResponse
{
    public string Name { get; set; } = "";
    public string LessonCode { get; set; } = "";
    public string LessonName { get; set; } = "";
    public string Grade { get; set; } = "";
    public string Gpa { get; set; } = "";
    public string GradeDetail { get; set; } = "";
    public string Credit { get; set; } = "";
    public bool IsMinor { get; set; }
}
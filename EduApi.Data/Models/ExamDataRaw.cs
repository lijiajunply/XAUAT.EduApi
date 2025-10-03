using System.Text.Json.Serialization;

namespace EduApi.Data.Models;

[Serializable]
public class ExamDataRaw
{
    [JsonPropertyName("course")] public Course Course { get; set; } = new();

    [JsonPropertyName("examTime")] public string ExamTime { get; set; } = "";

    [JsonPropertyName("room")] public string Room { get; set; } = "";

    [JsonPropertyName("seatNo")] public string SeatNo { get; set; } = "";
}

[Serializable]
public class Course
{
    [JsonPropertyName("nameZh")] public string NameZh { get; set; } = "";
}
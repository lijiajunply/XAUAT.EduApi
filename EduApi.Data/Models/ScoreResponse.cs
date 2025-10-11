using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace EduApi.Data.Models;

[Table("scores")]
public class ScoreResponse : DataModel
{
    [JsonIgnore] [Key] [MaxLength(64)] public string Key { get; set; } = Guid.NewGuid().ToString();
    [MaxLength(64)] public string Name { get; set; } = "";
    [MaxLength(64)] public string LessonCode { get; set; } = "";
    [MaxLength(64)] public string LessonName { get; set; } = "";
    [MaxLength(64)] public string Grade { get; set; } = "";
    [MaxLength(64)] public string Gpa { get; set; } = "";
    [MaxLength(64)] public string GradeDetail { get; set; } = "";
    [MaxLength(64)] public string Credit { get; set; } = "";
    public bool IsMinor { get; set; }
    
    [JsonIgnore] [MaxLength(64)] public string UserId { get; set; } = "";
    [JsonIgnore] [MaxLength(64)] public string Semester { get; set; } = "";
}
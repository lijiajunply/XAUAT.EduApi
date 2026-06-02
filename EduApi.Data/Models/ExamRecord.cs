using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace EduApi.Data.Models;

[Table("exam_records")]
public class ExamRecord : DataModel
{
    [JsonIgnore] [Key] [MaxLength(64)] public string Key { get; set; } = Guid.NewGuid().ToString();
    [JsonIgnore] [MaxLength(64)] public string StudentId { get; set; } = "";
    [MaxLength(256)] public string Name { get; set; } = "";
    [MaxLength(256)] public string Time { get; set; } = "";
    public DateTime ExamTime { get; set; }
    [MaxLength(256)] public string Location { get; set; } = "";
    [MaxLength(64)] public string Seat { get; set; } = "";
}

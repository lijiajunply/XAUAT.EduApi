using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EduApi.Data.Models;

[Table("users")]
public class UserModel : DataModel
{
    [MaxLength(10)] public string Username { get; set; } = "";
    [MaxLength(64)] public string Password { get; set; } = "";
    [Key] [MaxLength(64)] public string Id { get; set; } = "";
    public List<ScoreResponse> ScoreResponses { get; set; } = [];
    public List<string> Semesters { get; set; } = [];
    [MaxLength(64)] public string SemesterUpdateTime { get; set; } = "";
    [MaxLength(64)] public string ScoreResponsesUpdateTime { get; set; } = "";
}
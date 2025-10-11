using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EduApi.Data.Models;

[Table("users")]
public class UserModel : DataModel
{
    [Key] [MaxLength(10)] public string Username { get; set; } = "";
    [MaxLength(64)] public string Id { get; set; } = "";
    public List<ScoreResponse> ScoreResponses { get; set; } = [];
    [MaxLength(64)] public string ScoreResponsesUpdateTime { get; set; } = "";
}
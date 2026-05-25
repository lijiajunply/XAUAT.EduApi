using System.Text.Json.Serialization;

namespace EduApi.Data.Models;

public class ApiResponse<T>
{
    public T? Data { get; set; }
    public int Code { get; set; }
    public string Message { get; set; } = "";

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Total { get; set; }
}

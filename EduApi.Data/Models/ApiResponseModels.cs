namespace EduApi.Data.Models;

[Serializable]
public class LoginResponse
{
    public bool Success { get; set; }
    public string StudentId { get; set; } = "";
    public string Cookie { get; set; } = "";
}

[Serializable]
public class CourseResultResponse
{
    public bool Success { get; set; }
    public List<CourseActivity> Data { get; set; } = [];
    public DateTime ExpirationTime { get; set; }
}

[Serializable]
public class CourseErrorResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
}

[Serializable]
public class ErrorResponse
{
    public string error { get; set; } = "";
}

[Serializable]
public class ErrorWithMessageResponse : ErrorResponse
{
    public string message { get; set; } = "";
}

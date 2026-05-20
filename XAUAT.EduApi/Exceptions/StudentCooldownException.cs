namespace XAUAT.EduApi.Exceptions;

public class StudentCooldownException(string studentId, DateTimeOffset blockedUntil)
    : Exception($"学生 {studentId} 的请求正处于限流冷却期")
{
    public string StudentId { get; } = studentId;
    public DateTimeOffset BlockedUntil { get; } = blockedUntil;
}

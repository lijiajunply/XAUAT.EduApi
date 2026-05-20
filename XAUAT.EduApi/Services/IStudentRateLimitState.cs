namespace XAUAT.EduApi.Services;

public interface IStudentRateLimitState
{
    bool TryGetBlockedUntil(string studentId, out DateTimeOffset blockedUntil);
    TimeSpan MarkRateLimited(string studentId);
    void MarkSuccess(string studentId);
}

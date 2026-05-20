using XAUAT.EduApi.Exceptions;

namespace XAUAT.EduApi.Services;

public class StudentRateLimitExecutor(IStudentRateLimitState rateLimitState) : IStudentRateLimitExecutor
{
    public async Task<T> ExecuteAsync<T>(IEnumerable<string?> studentIds, Func<Task<T>> action)
    {
        var normalizedIds = NormalizeStudentIds(studentIds);

        foreach (var studentId in normalizedIds)
        {
            if (rateLimitState.TryGetBlockedUntil(studentId, out var blockedUntil))
            {
                throw new StudentCooldownException(studentId, blockedUntil);
            }
        }

        try
        {
            var result = await action();

            foreach (var studentId in normalizedIds)
            {
                rateLimitState.MarkSuccess(studentId);
            }

            return result;
        }
        catch (RateLimitException)
        {
            foreach (var studentId in normalizedIds)
            {
                rateLimitState.MarkRateLimited(studentId);
            }

            throw;
        }
    }

    public async Task ExecuteAsync(IEnumerable<string?> studentIds, Func<Task> action)
    {
        await ExecuteAsync(studentIds, async () =>
        {
            await action();
            return true;
        });
    }

    private static string[] NormalizeStudentIds(IEnumerable<string?> studentIds)
    {
        return studentIds
            .Where(studentId => !string.IsNullOrWhiteSpace(studentId))
            .Select(studentId => studentId!.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }
}

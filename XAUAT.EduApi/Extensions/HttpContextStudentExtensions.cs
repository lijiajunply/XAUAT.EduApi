using System.Security.Cryptography;
using System.Text;
using XAUAT.EduApi.Services;

namespace XAUAT.EduApi.Extensions;

public static class HttpContextStudentExtensions
{
    private const string ResolvedStudentIdsKey = "__ResolvedStudentIds";

    public static void SetResolvedStudentIds(this HttpContext context, IReadOnlyCollection<string> studentIds)
    {
        context.Items[ResolvedStudentIdsKey] = studentIds;
    }

    public static IReadOnlyCollection<string> GetResolvedStudentIds(this HttpContext context)
    {
        return context.Items.TryGetValue(ResolvedStudentIdsKey, out var value) &&
               value is IReadOnlyCollection<string> studentIds
            ? studentIds
            : [];
    }

    public static int? GetRetryAfterSeconds(this HttpContext context, IStudentRateLimitState rateLimitState)
    {
        var blockedUntil = context.GetResolvedStudentIds()
            .Select(studentId =>
                rateLimitState.TryGetBlockedUntil(studentId, out var currentBlockedUntil)
                    ? currentBlockedUntil
                    : (DateTimeOffset?)null)
            .Where(value => value.HasValue)
            .Select(value => value!.Value)
            .DefaultIfEmpty()
            .Max();

        if (blockedUntil == default)
        {
            return null;
        }

        return Math.Max(1, (int)Math.Ceiling((blockedUntil - DateTimeOffset.UtcNow).TotalSeconds));
    }

    public static string[] ParseStudentIds(string? rawStudentIds)
    {
        if (string.IsNullOrWhiteSpace(rawStudentIds))
        {
            return [];
        }

        return rawStudentIds
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(studentId => !string.IsNullOrWhiteSpace(studentId))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    public static string? CreateCookieRateLimitIdentity(string? cookie)
    {
        if (string.IsNullOrWhiteSpace(cookie))
        {
            return null;
        }

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(cookie.Trim()));
        return $"cookie:{Convert.ToHexString(bytes)}";
    }
}

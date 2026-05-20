using Microsoft.AspNetCore.Http;
using XAUAT.EduApi.Extensions;
using XAUAT.EduApi.Services;

namespace XAUAT.EduApi.Tests.Services;

public class StudentRateLimitStateTests
{
    private const string KeyA = "/score|student:A|cookie:none";
    private const string KeyB = "/course|student:B|cookie:none";

    [Fact]
    public void MarkRateLimited_ShouldEscalateCooldown_AndCapAtFourMinutes()
    {
        var state = new StudentRateLimitState();

        var first = state.MarkRateLimited(KeyA);
        var second = state.MarkRateLimited(KeyA);
        var third = state.MarkRateLimited(KeyA);
        var fourth = state.MarkRateLimited(KeyA);

        Assert.Equal(TimeSpan.FromMinutes(1), first);
        Assert.Equal(TimeSpan.FromMinutes(2), second);
        Assert.Equal(TimeSpan.FromMinutes(4), third);
        Assert.Equal(TimeSpan.FromMinutes(4), fourth);
    }

    [Fact]
    public void MarkSuccess_ShouldResetStudentCooldown()
    {
        var state = new StudentRateLimitState();

        state.MarkRateLimited(KeyA);
        Assert.True(state.TryGetBlockedUntil(KeyA, out _));

        state.MarkSuccess(KeyA);

        Assert.False(state.TryGetBlockedUntil(KeyA, out _));
        Assert.Equal(TimeSpan.FromMinutes(1), state.MarkRateLimited(KeyA));
    }

    [Fact]
    public void TryGetBlockedUntil_ShouldOnlyAffectCurrentStudent()
    {
        var state = new StudentRateLimitState();

        state.MarkRateLimited(KeyA);

        Assert.True(state.TryGetBlockedUntil(KeyA, out _));
        Assert.False(state.TryGetBlockedUntil(KeyB, out _));
    }

    [Fact]
    public async Task StudentRateLimitExecutor_ShouldShortCircuit_WhenStudentIsBlocked()
    {
        var state = new StudentRateLimitState();
        var httpContextAccessor = new HttpContextAccessor
        {
            HttpContext = BuildHttpContext("/score", "test-cookie")
        };
        var blockedKey = HttpContextStudentExtensions.CreateRateLimitStateKeys(["A"], "test-cookie", "/score").Single();
        state.MarkRateLimited(blockedKey);
        var executor = new StudentRateLimitExecutor(state, httpContextAccessor);
        var invoked = false;

        await Assert.ThrowsAsync<XAUAT.EduApi.Exceptions.StudentCooldownException>(() =>
            executor.ExecuteAsync(["A"], () =>
            {
                invoked = true;
                return Task.FromResult(1);
            }));

        Assert.False(invoked);
    }

    [Fact]
    public void RateLimitKeys_ShouldBeScopedByPath()
    {
        var scoreKey = HttpContextStudentExtensions.CreateRateLimitStateKeys(["20230001"], "test-cookie", "/score").Single();
        var courseKey = HttpContextStudentExtensions.CreateRateLimitStateKeys(["20230001"], "test-cookie", "/course").Single();

        Assert.NotEqual(scoreKey, courseKey);
    }

    private static DefaultHttpContext BuildHttpContext(string path, string cookie)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Request.Headers["xauat"] = cookie;
        return context;
    }
}

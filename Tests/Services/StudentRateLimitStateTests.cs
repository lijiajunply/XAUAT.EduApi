using XAUAT.EduApi.Extensions;
using XAUAT.EduApi.Services;

namespace XAUAT.EduApi.Tests.Services;

public class StudentRateLimitStateTests
{
    private const string KeyA = "rate_limit:A";
    private const string KeyB = "rate_limit:B";

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
        var blockedKey = HttpContextStudentExtensions.CreateRateLimitStateKeys(["A"]).Single();
        state.MarkRateLimited(blockedKey);
        var executor = new StudentRateLimitExecutor(state);
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
    public void RateLimitKeys_ShouldBePerStudentId()
    {
        var key1 = HttpContextStudentExtensions.CreateRateLimitStateKeys(["20230001"]).Single();
        var key2 = HttpContextStudentExtensions.CreateRateLimitStateKeys(["20230002"]).Single();

        Assert.NotEqual(key1, key2);
        Assert.Equal("rate_limit:20230001", key1);
        Assert.Equal("rate_limit:20230002", key2);
    }
}

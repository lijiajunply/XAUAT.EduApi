using XAUAT.EduApi.Services;

namespace XAUAT.EduApi.Tests.Services;

public class StudentRateLimitStateTests
{
    [Fact]
    public void MarkRateLimited_ShouldEscalateCooldown_AndCapAtFourMinutes()
    {
        var state = new StudentRateLimitState();

        var first = state.MarkRateLimited("A");
        var second = state.MarkRateLimited("A");
        var third = state.MarkRateLimited("A");
        var fourth = state.MarkRateLimited("A");

        Assert.Equal(TimeSpan.FromMinutes(1), first);
        Assert.Equal(TimeSpan.FromMinutes(2), second);
        Assert.Equal(TimeSpan.FromMinutes(4), third);
        Assert.Equal(TimeSpan.FromMinutes(4), fourth);
    }

    [Fact]
    public void MarkSuccess_ShouldResetStudentCooldown()
    {
        var state = new StudentRateLimitState();

        state.MarkRateLimited("A");
        Assert.True(state.TryGetBlockedUntil("A", out _));

        state.MarkSuccess("A");

        Assert.False(state.TryGetBlockedUntil("A", out _));
        Assert.Equal(TimeSpan.FromMinutes(1), state.MarkRateLimited("A"));
    }

    [Fact]
    public void TryGetBlockedUntil_ShouldOnlyAffectCurrentStudent()
    {
        var state = new StudentRateLimitState();

        state.MarkRateLimited("A");

        Assert.True(state.TryGetBlockedUntil("A", out _));
        Assert.False(state.TryGetBlockedUntil("B", out _));
    }

    [Fact]
    public async Task StudentRateLimitExecutor_ShouldShortCircuit_WhenStudentIsBlocked()
    {
        var state = new StudentRateLimitState();
        state.MarkRateLimited("A");
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
}

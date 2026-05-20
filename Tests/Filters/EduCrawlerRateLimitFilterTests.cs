using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Moq;
using XAUAT.EduApi.Exceptions;
using XAUAT.EduApi.Extensions;
using XAUAT.EduApi.Filters;
using XAUAT.EduApi.Interfaces;
using XAUAT.EduApi.Services;

namespace XAUAT.EduApi.Tests.Filters;

public class EduCrawlerRateLimitFilterTests
{
    [Fact]
    public async Task OnActionExecutionAsync_ShouldThrowCooldownException_WhenStudentIdIsBlocked()
    {
        var state = new StudentRateLimitState();
        state.MarkRateLimited("20230001");
        var filter = new EduCrawlerRateLimitFilter(state, Mock.Of<ICookieCodeService>());
        var context = BuildContext(
            controller: new object(),
            arguments: new Dictionary<string, object?> { ["studentId"] = "20230001" });

        await Assert.ThrowsAsync<StudentCooldownException>(() =>
            filter.OnActionExecutionAsync(context, () => throw new NotImplementedException()));
    }

    [Fact]
    public async Task OnActionExecutionAsync_ShouldResolveStudentIdFromCookie_WhenActionHasNoStudentId()
    {
        var state = new StudentRateLimitState();
        state.MarkRateLimited("20230001");
        var cookieCodeService = new Mock<ICookieCodeService>();
        cookieCodeService.Setup(x => x.GetCode("test-cookie")).ReturnsAsync("20230001");
        var filter = new EduCrawlerRateLimitFilter(state, cookieCodeService.Object);
        var context = BuildContext(controller: new object());
        context.HttpContext.Request.Headers["xauat"] = "test-cookie";

        await Assert.ThrowsAsync<StudentCooldownException>(() =>
            filter.OnActionExecutionAsync(context, () => throw new NotImplementedException()));
    }

    [Fact]
    public async Task OnActionExecutionAsync_ShouldContinue_WhenCookieParsingFails()
    {
        var cookieCodeService = new Mock<ICookieCodeService>();
        cookieCodeService.Setup(x => x.GetCode(It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("parse failed"));
        var filter = new EduCrawlerRateLimitFilter(new StudentRateLimitState(), cookieCodeService.Object);
        var context = BuildContext(controller: new object());
        context.HttpContext.Request.Headers["xauat"] = "test-cookie";
        var executed = false;

        await filter.OnActionExecutionAsync(context, () =>
        {
            executed = true;
            return Task.FromResult<ActionExecutedContext>(
                new ActionExecutedContext(context, [], context.Controller));
        });

        Assert.True(executed);
        var identities = context.HttpContext.GetResolvedStudentIds();
        Assert.Single(identities);
        Assert.StartsWith("cookie:", identities.Single());
    }

    [Fact]
    public async Task OnActionExecutionAsync_ShouldBlockByCookieIdentity_WhenStudentIdCannotBeResolved()
    {
        var cookie = "test-cookie";
        var state = new StudentRateLimitState();
        var cookieIdentity = HttpContextStudentExtensions.CreateCookieRateLimitIdentity(cookie)!;
        state.MarkRateLimited(cookieIdentity);

        var cookieCodeService = new Mock<ICookieCodeService>();
        cookieCodeService.Setup(x => x.GetCode(cookie))
            .ThrowsAsync(new InvalidOperationException("parse failed"));

        var filter = new EduCrawlerRateLimitFilter(state, cookieCodeService.Object);
        var context = BuildContext(controller: new object());
        context.HttpContext.Request.Headers["xauat"] = cookie;

        await Assert.ThrowsAsync<StudentCooldownException>(() =>
            filter.OnActionExecutionAsync(context, () => throw new NotImplementedException()));
    }

    private static ActionExecutingContext BuildContext(object controller, Dictionary<string, object?>? arguments = null)
    {
        var httpContext = new DefaultHttpContext();

        return new ActionExecutingContext(
            new ActionContext(httpContext, new RouteData(), new ActionDescriptor()),
            [],
            arguments ?? new Dictionary<string, object?>(),
            controller);
    }
}

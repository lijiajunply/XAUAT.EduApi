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
        state.MarkRateLimited(HttpContextStudentExtensions.CreateRateLimitStateKeys(["20230001"], null, "/score").Single());
        var filter = new EduCrawlerRateLimitFilter(state, Mock.Of<ICookieCodeService>());
        var context = BuildContext(
            controller: new object(),
            arguments: new Dictionary<string, object?> { ["studentId"] = "20230001" });
        context.HttpContext.Request.Path = "/score";

        await Assert.ThrowsAsync<StudentCooldownException>(() =>
            filter.OnActionExecutionAsync(context, () => throw new NotImplementedException()));
    }

    [Fact]
    public async Task OnActionExecutionAsync_ShouldResolveStudentIdFromCookie_WhenActionHasNoStudentId()
    {
        var state = new StudentRateLimitState();
        state.MarkRateLimited(HttpContextStudentExtensions.CreateRateLimitStateKeys(["20230001"], "test-cookie", "/score").Single());
        var cookieCodeService = new Mock<ICookieCodeService>();
        cookieCodeService.Setup(x => x.GetCode("test-cookie")).ReturnsAsync("20230001");
        var filter = new EduCrawlerRateLimitFilter(state, cookieCodeService.Object);
        var context = BuildContext(controller: new object());
        context.HttpContext.Request.Path = "/score";
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
        context.HttpContext.Request.Path = "/score";
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
        var rateLimitKeys = context.HttpContext.GetResolvedRateLimitKeys();
        Assert.Single(identities);
        Assert.StartsWith("cookie:", identities.Single());
        Assert.Single(rateLimitKeys);
        Assert.Contains("/score|student:none|cookie:", rateLimitKeys.Single());
    }

    [Fact]
    public async Task OnActionExecutionAsync_ShouldBlockByCookieIdentity_WhenStudentIdCannotBeResolved()
    {
        var cookie = "test-cookie";
        var state = new StudentRateLimitState();
        var rateLimitKey = HttpContextStudentExtensions.CreateRateLimitStateKeys([], cookie, "/score").Single();
        state.MarkRateLimited(rateLimitKey);

        var cookieCodeService = new Mock<ICookieCodeService>();
        cookieCodeService.Setup(x => x.GetCode(cookie))
            .ThrowsAsync(new InvalidOperationException("parse failed"));

        var filter = new EduCrawlerRateLimitFilter(state, cookieCodeService.Object);
        var context = BuildContext(controller: new object());
        context.HttpContext.Request.Path = "/score";
        context.HttpContext.Request.Headers["xauat"] = cookie;

        await Assert.ThrowsAsync<StudentCooldownException>(() =>
            filter.OnActionExecutionAsync(context, () => throw new NotImplementedException()));
    }

    [Fact]
    public async Task OnActionExecutionAsync_ShouldNotBlockDifferentPath()
    {
        var cookie = "test-cookie";
        var state = new StudentRateLimitState();
        state.MarkRateLimited(HttpContextStudentExtensions.CreateRateLimitStateKeys(["20230001"], cookie, "/score").Single());

        var cookieCodeService = new Mock<ICookieCodeService>();
        cookieCodeService.Setup(x => x.GetCode(cookie)).ReturnsAsync("20230001");

        var filter = new EduCrawlerRateLimitFilter(state, cookieCodeService.Object);
        var context = BuildContext(controller: new object());
        context.HttpContext.Request.Path = "/course";
        context.HttpContext.Request.Headers["xauat"] = cookie;
        var executed = false;

        await filter.OnActionExecutionAsync(context, () =>
        {
            executed = true;
            return Task.FromResult<ActionExecutedContext>(
                new ActionExecutedContext(context, [], context.Controller));
        });

        Assert.True(executed);
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

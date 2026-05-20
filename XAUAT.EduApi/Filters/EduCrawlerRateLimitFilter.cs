using Microsoft.AspNetCore.Mvc.Filters;
using XAUAT.EduApi.Exceptions;
using XAUAT.EduApi.Extensions;
using XAUAT.EduApi.Interfaces;
using XAUAT.EduApi.Services;

namespace XAUAT.EduApi.Filters;

public class EduCrawlerRateLimitFilter(
    IStudentRateLimitState rateLimitState,
    ICookieCodeService cookieCodeService) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var studentIds = await ResolveStudentIdsAsync(context);
        context.HttpContext.SetResolvedStudentIds(studentIds);

        foreach (var studentId in studentIds)
        {
            if (!rateLimitState.TryGetBlockedUntil(studentId, out var blockedUntil))
            {
                continue;
            }

            throw new StudentCooldownException(studentId, blockedUntil);
        }

        await next();
    }

    private async Task<string[]> ResolveStudentIdsAsync(ActionExecutingContext context)
    {
        var controllerName = context.Controller.GetType().Name;

        if (context.ActionArguments.TryGetValue("studentId", out var studentIdArg))
        {
            return HttpContextStudentExtensions.ParseStudentIds(studentIdArg as string);
        }

        if (controllerName == "ProgramController" &&
            context.ActionArguments.TryGetValue("id", out var idArg))
        {
            return HttpContextStudentExtensions.ParseStudentIds(idArg as string);
        }

        var cookie = context.HttpContext.Request.GetEduAuthCookie();
        if (string.IsNullOrWhiteSpace(cookie))
        {
            return [];
        }

        try
        {
            var resolvedStudentIds = await cookieCodeService.GetCode(cookie);
            return HttpContextStudentExtensions.ParseStudentIds(resolvedStudentIds);
        }
        catch
        {
            return [];
        }
    }
}

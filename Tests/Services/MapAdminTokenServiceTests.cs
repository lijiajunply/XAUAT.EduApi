using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using XAUAT.EduApi.Configuration;
using XAUAT.EduApi.Services;

namespace XAUAT.EduApi.Tests.Services;

public class MapAdminTokenServiceTests
{
    [Fact]
    public void IsAuthorized_ShouldReturnTrue_WhenTokenHeaderMatches()
    {
        var service = CreateService("test-token");
        var context = new DefaultHttpContext();
        context.Request.Headers["Token"] = "test-token";

        var result = service.IsAuthorized(context.Request);

        Assert.True(result);
    }

    [Fact]
    public void IsAuthorized_ShouldReturnTrue_WhenAuthorizationBearerMatches()
    {
        var service = CreateService("test-token");
        var context = new DefaultHttpContext();
        context.Request.Headers.Authorization = "Bearer test-token";

        var result = service.IsAuthorized(context.Request);

        Assert.True(result);
    }

    [Fact]
    public void IsAuthorized_ShouldReturnFalse_WhenTokenHeaderIsMissing()
    {
        var service = CreateService("test-token");
        var context = new DefaultHttpContext();

        var result = service.IsAuthorized(context.Request);

        Assert.False(result);
    }

    [Fact]
    public void IsAuthorized_ShouldReturnFalse_WhenConfiguredTokenIsEmpty()
    {
        var service = CreateService("");
        var context = new DefaultHttpContext();
        context.Request.Headers["Token"] = "test-token";

        var result = service.IsAuthorized(context.Request);

        Assert.False(result);
    }

    private static IMapAdminTokenService CreateService(string token)
    {
        return new MapAdminTokenService(Options.Create(new MapAdminOptions
        {
            Token = token
        }));
    }
}

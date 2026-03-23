using System.Text.Json;
using EduApi.Data.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json.Linq;
using XAUAT.EduApi.Controllers;
using XAUAT.EduApi.Exceptions;
using XAUAT.EduApi.Interfaces;
using XAUAT.EduApi.Services;

namespace XAUAT.EduApi.Tests.Controllers;

public class ControllerResponseModelTests
{
    [Fact]
    public async Task LoginController_ShouldReturnTypedSuccessResponse()
    {
        var loginService = new Mock<ILoginService>();
        loginService.Setup(x => x.LoginAsync("20230001", "pwd"))
            .ReturnsAsync(new LoginResponse
            {
                Success = true,
                StudentId = "20230001",
                Cookie = "foo=bar"
            });

        var controller = new LoginController(loginService.Object, Mock.Of<ILogger<LoginController>>());

        var result = await controller.Login(new LoginRequest
        {
            Username = "20230001",
            Password = "pwd"
        });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<LoginResponse>(ok.Value);
        Assert.True(payload.Success);
        Assert.Equal("20230001", payload.StudentId);
        Assert.Equal("foo=bar", payload.Cookie);
    }

    [Fact]
    public async Task CourseController_ShouldPreserveSuccessJsonShape()
    {
        var courseService = new Mock<ICourseService>();
        courseService.Setup(x => x.GetCoursesAsync("20230001", "test-cookie"))
            .ReturnsAsync([
                new CourseActivity
                {
                    CourseName = "高等数学",
                    CourseCode = "MATH001"
                }
            ]);

        var controller = new CourseController(Mock.Of<ILogger<CourseController>>(), courseService.Object)
        {
            ControllerContext = BuildControllerContext("test-cookie")
        };

        var result = await controller.GetCourse("20230001");

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<CourseResultResponse>(ok.Value);
        Assert.True(payload.Success);
        Assert.Single(payload.Data);

        var json = JsonSerializer.Serialize(payload);
        Assert.Contains("\"Success\":true", json);
        Assert.Contains("\"Data\":[", json);
        Assert.Contains("\"ExpirationTime\":", json);
    }

    [Fact]
    public async Task CourseController_ShouldReturnTypedUnauthorizedError()
    {
        var courseService = new Mock<ICourseService>();
        courseService.Setup(x => x.GetCoursesAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new UnAuthenticationError());

        var controller = new CourseController(Mock.Of<ILogger<CourseController>>(), courseService.Object)
        {
            ControllerContext = BuildControllerContext("test-cookie")
        };

        var result = await controller.GetCourse("20230001");

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        var payload = Assert.IsType<CourseErrorResponse>(unauthorized.Value);
        Assert.False(payload.Success);
        Assert.Equal("认证失败，请重新登录", payload.Message);
    }

    [Fact]
    public async Task PaymentController_ShouldReturnTypedServiceUnavailableError()
    {
        var paymentService = new Mock<IPaymentService>();
        paymentService.Setup(x => x.Login("123456"))
            .ThrowsAsync(new PaymentServiceException("远端失败"));

        var controller = new PaymentController(paymentService.Object, Mock.Of<ILogger<PaymentController>>());

        var result = await controller.Login("123456");

        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, objectResult.StatusCode);
        var payload = Assert.IsType<ErrorWithMessageResponse>(objectResult.Value);
        Assert.Equal("服务暂时不可用", payload.error);
        Assert.Equal("远端失败", payload.message);
    }

    [Fact]
    public async Task ScoreController_ShouldReturnTypedBadRequestError()
    {
        var scoreService = new Mock<IScoreService>();
        scoreService.Setup(x => x.GetScoresAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new ArgumentNullException("semester", "semester is required"));

        var controller = new ScoreController(Mock.Of<ILogger<ScoreController>>(), scoreService.Object)
        {
            ControllerContext = BuildControllerContext("test-cookie")
        };

        var result = await controller.GetScore("20230001", "");

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        var payload = Assert.IsType<ErrorResponse>(badRequest.Value);
        Assert.Contains("semester is required", payload.error);
    }

    [Fact]
    public async Task ProgramController_ShouldReturnTypedProgramList()
    {
        var programService = new Mock<IProgramService>();
        programService.Setup(x => x.GetAllTrainProgram("test-cookie", "3241"))
            .ReturnsAsync([
                new PlanCourse
                {
                    Name = "线性代数",
                    TermStr = "1"
                }
            ]);

        var controller = new ProgramController(Mock.Of<ILogger<ProgramController>>(), programService.Object)
        {
            ControllerContext = BuildControllerContext("test-cookie")
        };

        var result = await controller.GetAllTrainProgram("3241", null);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<List<PlanCourse>>(ok.Value);
        Assert.Single(payload);
    }

    [Fact]
    public void InfoController_ShouldReturnTypedTimeModel()
    {
        var infoService = new Mock<IInfoService>();
        infoService.Setup(x => x.GetTime()).Returns(new TimeModel
        {
            StartTime = "2026-03-01",
            EndTime = "2026-07-18"
        });

        var controller = new InfoController(
            Mock.Of<IHttpClientFactory>(),
            Mock.Of<ILogger<CourseController>>(),
            infoService.Object);

        var result = controller.GetTime();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<TimeModel>(ok.Value);
        Assert.Equal("2026-03-01", payload.StartTime);
        Assert.Equal("2026-07-18", payload.EndTime);
    }

    [Fact]
    public async Task PlaygroundController_ShouldReturnTypedJObject()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage
        {
            StatusCode = System.Net.HttpStatusCode.OK,
            Content = new StringContent("{\"ok\":true}")
        });
        var client = new HttpClient(handler);
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(client);

        var controller = new PlaygroundController(factory.Object)
        {
            ControllerContext = BuildControllerContext("test-cookie")
        };

        var result = await controller.Get();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<JObject>(ok.Value);
        Assert.True(payload.Value<bool>("ok"));
    }

    private static ControllerContext BuildControllerContext(string cookie)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["xauat"] = cookie;

        return new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(handler(request));
        }
    }
}

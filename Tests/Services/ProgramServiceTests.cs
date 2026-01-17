using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using StackExchange.Redis;
using System.Net;
using System.Text.Json;
using XAUAT.EduApi.Services;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace XAUAT.EduApi.Tests.Services;

public class ProgramServiceTests
{
    private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
    private readonly Mock<IConnectionMultiplexer> _mockMuxer;
    private readonly Mock<IDatabase> _mockDatabase;
    private readonly ProgramService _service;

    public ProgramServiceTests()
    {
        _mockHttpClientFactory = new Mock<IHttpClientFactory>();
        _mockMuxer = new Mock<IConnectionMultiplexer>();
        _mockDatabase = new Mock<IDatabase>();
        
        _mockMuxer.Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(_mockDatabase.Object);
        
        _service = new ProgramService(_mockHttpClientFactory.Object, _mockMuxer.Object);
    }

    [Fact]
    public async Task GetAllTrainProgram_ShouldReturnCachedData_WhenCacheExists()
    {
        // Arrange
        var id = "123";
        var cookie = "cookie";
        var cachedData = new List<PlanCourse> { new PlanCourse { Name = "Test Course" } };
        var cachedJson = JsonConvert.SerializeObject(cachedData);

        _mockDatabase.Setup(x => x.StringGetAsync($"train-program-{id}", It.IsAny<CommandFlags>()))
            .ReturnsAsync(cachedJson);

        // Act
        var result = await _service.GetAllTrainProgram(cookie, id);

        // Assert
        Assert.Single(result);
        Assert.Equal("Test Course", result[0].Name);
        _mockHttpClientFactory.Verify(x => x.CreateClient(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GetAllTrainProgram_ShouldFetchFromApi_WhenCacheMiss()
    {
        // Arrange
        var id = "123";
        var cookie = "cookie";
        
        _mockDatabase.Setup(x => x.StringGetAsync($"train-program-{id}", It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);

        var apiResponse = new ProgramModel
        {
            Children = new List<ProgramModel>
            {
                new ProgramModel
                {
                    PlanCourses = new List<PlanCourses>
                    {
                        new PlanCourses
                        {
                            Course = new CourseItem { NameZh = "API Course" },
                            ReadableTerms = new[] { "Term1" }
                        }
                    }
                }
            }
        };
        var apiJson = JsonSerializer.Serialize(apiResponse);

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
           .Protected()
           .Setup<Task<HttpResponseMessage>>(
              "SendAsync",
              ItExpr.IsAny<HttpRequestMessage>(),
              ItExpr.IsAny<CancellationToken>()
           )
           .ReturnsAsync(new HttpResponseMessage
           {
               StatusCode = HttpStatusCode.OK,
               Content = new StringContent(apiJson)
           });

        var client = new HttpClient(handlerMock.Object);
        _mockHttpClientFactory.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(client);

        // Act
        var result = await _service.GetAllTrainProgram(cookie, id);

        // Assert
        Assert.Single(result);
        Assert.Equal("API Course", result[0].Name);
        
        // Verify cache set
        _mockDatabase.Verify(x => x.StringSetAsync(
            $"train-program-{id}", 
            It.IsAny<RedisValue>(), 
            It.IsAny<TimeSpan?>(), 
            It.IsAny<bool>(), 
            It.IsAny<When>(), 
            It.IsAny<CommandFlags>()), Times.Once);
    }
    
    [Fact]
    public async Task GetAllTrainProgram_ShouldThrowUnAuthenticationError_WhenSessionExpired()
    {
        // Arrange
        var id = "123";
        var cookie = "cookie";
        
        _mockDatabase.Setup(x => x.StringGetAsync($"train-program-{id}", It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
           .Protected()
           .Setup<Task<HttpResponseMessage>>(
              "SendAsync",
              ItExpr.IsAny<HttpRequestMessage>(),
              ItExpr.IsAny<CancellationToken>()
           )
           .ReturnsAsync(new HttpResponseMessage
           {
               StatusCode = HttpStatusCode.OK,
               Content = new StringContent("<html>...登入页面...</html>")
           });

        var client = new HttpClient(handlerMock.Object);
        _mockHttpClientFactory.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(client);

        // Act & Assert
        await Assert.ThrowsAsync<XAUAT.EduApi.Exceptions.UnAuthenticationError>(() => _service.GetAllTrainProgram(cookie, id));
    }
}

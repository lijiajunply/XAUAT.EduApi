using System.Net;
using EduApi.Data.Models;
using Moq;
using Moq.Protected;
using XAUAT.EduApi.Caching;
using XAUAT.EduApi.Localization;
using XAUAT.EduApi.Services;

namespace XAUAT.EduApi.Tests.Services;

public class BusServiceTests
{
    [Theory]
    [InlineData(RequestLanguage.French, "Ancient Platform", " (données issues de l'ancienne plateforme)")]
    [InlineData(RequestLanguage.TraditionalChinese, "舊平台", " (呼叫的是舊平台資料)")]
    public async Task GetBusFromOldDataAsync_ShouldAppendLocalizedLegacySuffix_WhenIsShowIsTrue(
        string language,
        string description,
        string suffix)
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(
                    $$"""
                    {
                      "data": {
                        "total": 1,
                        "records": [
                          {
                            "lineName": "1号线",
                            "descr": "{{description}}",
                            "departureStation": "草堂校区",
                            "arrivalStation": "雁塔校区",
                            "runTime": "08:00",
                            "arrivalStationTime": "09:30",
                            "wayStation": ""
                          }
                        ]
                      }
                    }
                    """)
            });

        var clientFactory = new Mock<IHttpClientFactory>();
        clientFactory.Setup(x => x.CreateClient("BusClient"))
            .Returns(new HttpClient(handler.Object, disposeHandler: false));

        var cacheService = new Mock<ICacheService>();
        cacheService.Setup(x => x.GetOrCreateAsync(
                It.IsAny<string>(),
                It.IsAny<Func<Task<BusModel>>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CacheLevel>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<bool>()))
            .Returns(async (
                string cacheKey,
                Func<Task<BusModel>> factory,
                TimeSpan? expiration,
                CacheLevel level,
                int priority,
                CancellationToken cancellationToken,
                bool isUse) => await factory());

        var service = new BusService(clientFactory.Object, cacheService.Object, new ApiMessageLocalizer());

        var result = await service.GetBusFromOldDataAsync("2026-05-20", language, isShow: true);

        Assert.Single(result.Records);
        Assert.Equal(description + suffix, result.Records[0].Description);
    }
}

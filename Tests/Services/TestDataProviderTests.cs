using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using XAUAT.EduApi.Configuration;
using XAUAT.EduApi.Services;

namespace XAUAT.EduApi.Tests.Services;

public class TestDataProviderTests : IDisposable
{
    private readonly string _tempDir;

    public TestDataProviderTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"xauat-test-fixtures-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task GetScoresAsync_ShouldFilterBySemester()
    {
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "scores.json"), """
        [
          {
            "Semester": "301",
            "Name": "测试数据结构",
            "LessonCode": "TEST101",
            "LessonName": "测试数据结构",
            "Grade": "92",
            "Gpa": "4.2",
            "GradeDetail": "平时成绩: 90; 期末成绩: 94",
            "Credit": "3.0",
            "IsMinor": false
          },
          {
            "Semester": "300",
            "Name": "测试操作系统",
            "LessonCode": "TEST303",
            "LessonName": "测试操作系统",
            "Grade": "90",
            "Gpa": "4.0",
            "GradeDetail": "平时成绩: 88; 期末成绩: 92",
            "Credit": "3.5",
            "IsMinor": false
          }
        ]
        """);

        var provider = CreateProvider(_tempDir);

        var result = await provider.GetScoresAsync("301");

        Assert.Single(result);
        Assert.Equal("测试数据结构", result[0].Name);
    }

    [Fact]
    public async Task GetCoursesAsync_ShouldThrow_WhenFixtureMissing()
    {
        var provider = CreateProvider(_tempDir);

        await Assert.ThrowsAsync<FileNotFoundException>(() => provider.GetCoursesAsync());
    }

    private TestDataProvider CreateProvider(string fixturePath)
    {
        var options = Options.Create(new TestAccountOptions
        {
            Enabled = true,
            StudentId = "20239999",
            CookieMarker = "frontend-test-marker",
            FixturePath = fixturePath
        });

        var environment = new Mock<IHostEnvironment>();
        environment.SetupGet(x => x.ContentRootPath).Returns(_tempDir);

        return new TestDataProvider(
            options,
            environment.Object,
            Mock.Of<ILogger<TestDataProvider>>());
    }
}

using System.Text;
using Newtonsoft.Json;
using System.Text.Json.Serialization;
using StackExchange.Redis;
using JsonSerializer = System.Text.Json.JsonSerializer;
using Polly;

namespace XAUAT.EduApi.Services;

public interface IProgramService
{
    public Task<List<PlanCourse>> GetAllTrainProgram(string cookie, string id);

    public Task<Dictionary<string, List<PlanCourse>>> GetAllTrainPrograms(string cookie, string id);
}

public class ProgramService(IHttpClientFactory httpClientFactory, IConnectionMultiplexer muxer) : IProgramService
{
    private const string BaseUrl = "https://swjw.xauat.edu.cn";
    private readonly IDatabase _redis = muxer.GetDatabase();

    public async Task<List<PlanCourse>> GetAllTrainProgram(string cookie, string id)
    {
        var key = $"train-program-{id}";
        var trainProgramValue = await _redis.StringGetAsync(key);

        if (trainProgramValue.HasValue)
        {
            return JsonConvert.DeserializeObject<List<PlanCourse>>(trainProgramValue.ToString()) ?? [];
        }

        var retryPolicy = Policy
            .Handle<HttpRequestException>()
            .Or<TaskCanceledException>()
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

        return await retryPolicy.ExecuteAsync(async () =>
        {
            var request =
                new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/student/for-std/program/root-module-json/{id}");
            request.Headers.Add("Cookie", cookie);

            using var httpClient = httpClientFactory.CreateClient(); // 使用命名客户端
            httpClient.Timeout = TimeSpan.FromSeconds(5); // 5秒超时

            var response = await httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode) return [];
            var content = await response.Content.ReadAsStringAsync();

        if (content.Contains("登入页面"))
        {
            throw new Exceptions.UnAuthenticationError();
        }

            var result = JsonSerializer.Deserialize<ProgramModel>(content) ?? new ProgramModel();

            var list = GetPlanCourses(result);

            if (list.Count != 0)
            {
                _redis.StringSet(key, JsonSerializer.Serialize(list), new TimeSpan(1, 0, 0, 0));
            }

            return list;
        });
    }

    private static List<PlanCourse> GetPlanCourses(ProgramModel result)
    {
        var list = new List<PlanCourse>();

        foreach (var item in result.Children)
        {
            list.AddRange(item.PlanCourses.Select(x => x.To()));
            list.AddRange(GetPlanCourses(item));
        }

        return list;
    }

    public async Task<Dictionary<string, List<PlanCourse>>> GetAllTrainPrograms(string cookie, string id)
    {
        var result = await GetAllTrainProgram(cookie, id);

        return result.GroupBy(x => x.TermStr.Contains(',') ? "特殊分组" : x.TermStr)
            .OrderBy(x => x.Key)
            .ToDictionary(x => x.Key, x => x.ToList());
    }
}

public class ProgramModel
{
    [JsonPropertyName("children")] public List<ProgramModel> Children { get; init; } = [];
    [JsonPropertyName("planCourses")] public List<PlanCourses> PlanCourses { get; init; } = [];
}

[Serializable]
public class PlanCourses
{
    [JsonPropertyName("readableTerms")] public string[] ReadableTerms { get; set; } = [];
    [JsonPropertyName("course")] public CourseItem Course { get; set; } = new();

    public PlanCourse To()
    {
        var termStr = new StringBuilder();
        var termList = ReadableTerms;
        for (var index = 0; index < termList.Length; index++)
        {
            termStr.Append(termList[index]);
            if (index != termList.Length - 1)
            {
                termStr.Append(',');
            }

            if (index != 0 && index % 3 == 0)
            {
                termStr.Append('\n');
            }
        }

        return new PlanCourse()
        {
            Name = Course.NameZh,
            LessonType = Course.LessonType,
            ExamMode = Course.DefaultExamMode.Name,
            CourseTypeName = Course.CourseType.Name,
            Credits = Course.Credits,
            TermStr = termStr.ToString()
        };
    }
}

[Serializable]
public class CourseItem
{
    [JsonPropertyName("nameZh")] public string NameZh { get; set; } = "";
    [JsonPropertyName("lessonType")] public string LessonType { get; set; } = "";
    [JsonPropertyName("defaultExamMode")] public DefaultExamMode DefaultExamMode { get; set; } = new();

    [JsonPropertyName("courseType")] public CourseType CourseType { get; set; } = new();
    [JsonPropertyName("credits")] public double Credits { get; set; }
}

[Serializable]
public class DefaultExamMode
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
}

[Serializable]
public class CourseType
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
}

[Serializable]
public class PlanCourse
{
    public string Name { get; init; } = "";
    public string LessonType { get; set; } = "";
    public string ExamMode { get; set; } = "";
    public string CourseTypeName { get; set; } = "";
    public double Credits { get; set; }
    public string TermStr { get; init; } = "";
}
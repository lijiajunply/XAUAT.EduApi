using System.Text;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using StackExchange.Redis;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace XAUAT.EduApi.Services;

public class ProgramService(IHttpClientFactory httpClientFactory, IConnectionMultiplexer muxer) : IProgramService
{
    private const string _baseUrl = "https://swjw.xauat.edu.cn";
    private readonly IDatabase _redis = muxer.GetDatabase();

    public async Task<List<PlanCourse>> GetAllTrainProgram(string cookie, string id)
    {
        var key = $"train-program-{id}";
        var trainProgramValue = await _redis.StringGetAsync(key);

        if (trainProgramValue.HasValue)
        {
            return JsonConvert.DeserializeObject<List<PlanCourse>>(trainProgramValue.ToString()) ?? [];
        }

        var request =
            new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/student/for-std/program/root-module-json/{id}");
        request.Headers.Add("Cookie", cookie);

        using var httpClient = httpClientFactory.CreateClient(); // 使用命名客户端

        var response = await httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode) return [];
        var content = await response.Content.ReadAsStringAsync();

        var result = JsonSerializer.Deserialize<ProgramModel>(content) ?? new ProgramModel();

        var list = GetPlanCourses(result);

        if (list.Count != 0)
        {
            _redis.StringSet(key, JsonSerializer.Serialize(list), new TimeSpan(1, 0, 0, 0));
        }

        return list;
    }

    private static List<PlanCourse> GetPlanCourses(ProgramModel result)
    {
        var list = new List<PlanCourse>();

        foreach (var item in result.Children)
        {
            list.AddRange(item.planCourses.Select(x => x.To()));
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
    public List<PlanCourses> planCourses { get; init; } = [];
}

[Serializable]
public class PlanCourses
{
    public string[] readableTerms { get; set; } = [];
    public CourseItem course { get; set; } = new();

    public PlanCourse To()
    {
        var termStr = new StringBuilder();
        var termList = readableTerms;
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
            Name = course.nameZh,
            LessonType = course.lessonType,
            ExamMode = course.defaultExamMode.name,
            CourseTypeName = course.courseType.name,
            Credits = course.credits,
            TermStr = termStr.ToString()
        };
    }
}

[Serializable]
public class CourseItem
{
    public string nameZh { get; set; } = "";
    public string lessonType { get; set; } = "";
    public DefaultExamMode defaultExamMode { get; set; } = new();

    public CourseType courseType { get; set; } = new();
    public double credits { get; set; }
}

[Serializable]
public class DefaultExamMode
{
    public string name { get; set; } = "";
}

[Serializable]
public class CourseType
{
    public string name { get; set; } = "";
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
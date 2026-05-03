using System.Text.Json.Serialization;

namespace EduApi.Data.Models;

public class CategoryModel
{
    [JsonPropertyName("key")] public string Key { get; init; } = "";
    [JsonPropertyName("name")] public string Name { get; init; } = "";
    [JsonPropertyName("description")] public string? Description { get; init; }
    [JsonPropertyName("icon")] public string Icon { get; init; } = "";
    [JsonPropertyName("index")] public int Index { get; init; }
    [JsonPropertyName("links")] public List<LinkModel> Links { get; init; } = [];
}

public class LinkModel
{
    [JsonPropertyName("key")] public string Key { get; init; } = "";

    [JsonPropertyName("name")] public string Name { get; init; } = "";

    [JsonPropertyName("icon")] public string? Icon { get; init; }

    [JsonPropertyName("url")] public string Url { get; init; } = "";

    [JsonPropertyName("description")] public string? Description { get; init; }

    [JsonPropertyName("index")] public int Index { get; init; }
}
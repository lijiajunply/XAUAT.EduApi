using System.Text.Json.Serialization;

namespace EduApi.Data.Models;

[Serializable]
public class ReleaseInfo
{
    [JsonPropertyName("id")] public long Id { get; set; }

    [JsonPropertyName("tag_name")] public string? TagName { get; set; }

    [JsonPropertyName("name")] public string? Name { get; set; }

    [JsonPropertyName("body")] public string? Body { get; set; }

    [JsonPropertyName("author")] public AuthorInfo? Author { get; set; }

    [JsonPropertyName("created_at")] public DateTimeOffset CreatedAt { get; set; }

    [JsonPropertyName("assets")] public List<AssetInfo>? Assets { get; set; }
}

[Serializable]
public class AuthorInfo
{
    [JsonPropertyName("id")] public long Id { get; set; }

    [JsonPropertyName("name")] public string? Name { get; set; }
}

[Serializable]
public class AssetInfo
{
    [JsonPropertyName("browser_download_url")]
    public string? BrowserDownloadUrl { get; set; }

    [JsonPropertyName("name")] public string? Name { get; set; }
}
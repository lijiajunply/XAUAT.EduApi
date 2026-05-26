using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace XAUAT.EduApi.OpenApi;

public class CookieSecurityDocumentTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        var scheme = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.ApiKey,
            In = ParameterLocation.Header,
            Name = "Cookie",
            Description = "直接粘贴完整的 Cookie 字符串，例如：__pstsid__=xxx; SESSION=xxx"
        };

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes["cookie"] = scheme;

        return Task.CompletedTask;
    }
}

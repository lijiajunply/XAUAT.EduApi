using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using System.Text.Json.Nodes;
using XAUAT.EduApi.Localization;

namespace XAUAT.EduApi.OpenApi;

public class LanguageHeaderOperationTransformer : IOpenApiOperationTransformer
{
    public const string DefaultLanguage = RequestLanguage.SimplifiedChinese;
    public const string Description =
        "Optional response language. Supported values: de, ru, fr, ja, ko, en, zh-CN, zh-TW. Legacy aliases: zh -> zh-CN, en -> en. Defaults to zh-CN.";

    private static readonly HashSet<string> SupportedTags =
    [
        "Login",
        "Bus",
        "Course",
        "Score",
        "Exam",
        "Program",
        "Info",
        "Payment"
    ];

    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        var controllerName = context.Description.ActionDescriptor.RouteValues["controller"];
        if (string.IsNullOrWhiteSpace(controllerName) || !SupportedTags.Contains(controllerName))
        {
            return Task.CompletedTask;
        }

        operation.Parameters ??= [];

        if (operation.Parameters.Any(parameter =>
                parameter.In == ParameterLocation.Header &&
                string.Equals(parameter.Name, "x-language", StringComparison.OrdinalIgnoreCase)))
        {
            return Task.CompletedTask;
        }

        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "x-language",
            In = ParameterLocation.Header,
            Required = false,
            Description = Description,
            Schema = new OpenApiSchema
            {
                Type = JsonSchemaType.String,
                Default = JsonValue.Create(DefaultLanguage),
                Enum = RequestLanguage.SupportedLanguages
                    .Select(language => (JsonNode)JsonValue.Create(language)!)
                    .ToList()
            }
        });

        return Task.CompletedTask;
    }
}

using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using System.Text.Json.Nodes;

namespace XAUAT.EduApi.OpenApi;

public class LanguageHeaderOperationTransformer : IOpenApiOperationTransformer
{
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
            Description = "Optional response language. Supported values: zh, en. Defaults to zh.",
            Schema = new OpenApiSchema
            {
                Type = JsonSchemaType.String,
                Default = JsonValue.Create("zh"),
                Enum =
                [
                    JsonValue.Create("zh"),
                    JsonValue.Create("en")
                ]
            }
        });

        return Task.CompletedTask;
    }
}

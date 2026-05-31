using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Api.Swagger;

/// <summary>
/// Applies the Bearer security requirement only to operations decorated with [Authorize]
/// that are not explicitly marked [AllowAnonymous].
/// </summary>
public sealed class AuthorizeCheckOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var actionAttributes = context.MethodInfo.GetCustomAttributes(inherit: true);
        var controllerAttributes = context.MethodInfo.DeclaringType?.GetCustomAttributes(inherit: true) ?? [];

        var hasAuthorize = actionAttributes.OfType<AuthorizeAttribute>().Any()
            || controllerAttributes.OfType<AuthorizeAttribute>().Any();

        var hasAllowAnonymous = actionAttributes.OfType<AllowAnonymousAttribute>().Any()
            || controllerAttributes.OfType<AllowAnonymousAttribute>().Any();

        if (!hasAuthorize || hasAllowAnonymous)
        {
            return;
        }

        operation.Security =
        [
            new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = SwaggerAuthSchemes.Bearer
                        }
                    },
                    Array.Empty<string>()
                }
            }
        ];

        operation.Responses.TryAdd("401", new OpenApiResponse { Description = "Unauthorized — missing or invalid JWT." });
        operation.Responses.TryAdd("403", new OpenApiResponse { Description = "Forbidden — authenticated but insufficient role." });
    }
}

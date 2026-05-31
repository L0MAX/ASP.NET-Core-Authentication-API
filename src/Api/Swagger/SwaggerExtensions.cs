using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace Api.Swagger;

public static class SwaggerExtensions
{
    private const string ApiTitle = "Clean Architecture API";
    private const string ApiVersion = "v1";

    public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc(ApiVersion, CreateOpenApiInfo());
            options.AddJwtBearerSecurity();
            options.OperationFilter<AuthorizeCheckOperationFilter>();
        });

        return services;
    }

    public static WebApplication UseSwaggerDocumentation(this WebApplication app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint($"/swagger/{ApiVersion}/swagger.json", $"{ApiTitle} {ApiVersion}");
            options.RoutePrefix = "swagger";
            options.DocumentTitle = ApiTitle;
            options.DisplayRequestDuration();
            options.EnablePersistAuthorization();
            options.DocExpansion(DocExpansion.List);
            options.DefaultModelsExpandDepth(2);
        });

        return app;
    }

    private static OpenApiInfo CreateOpenApiInfo() =>
        new()
        {
            Title = ApiTitle,
            Version = ApiVersion,
            Description = """
                ASP.NET Core 9 Web API with Clean Architecture, JWT authentication, and role-based authorization.

                ## Testing secured endpoints

                1. Register via `POST /api/auth/register`, then verify email with `POST /api/auth/verify-email`.
                2. Login via `POST /api/auth/login` and copy `data.accessToken` from the response.
                3. Click the **Authorize** button (top right), paste the access token, and click **Authorize**.
                4. Call secured endpoints — for example `GET /api/auth/me`, `GET /api/user/dashboard`, or `GET /api/admin/dashboard`.

                Endpoints marked with a lock icon require a valid Bearer token. Admin routes additionally require the `Admin` role.
                """
        };

    private static void AddJwtBearerSecurity(this SwaggerGenOptions options)
    {
        options.AddSecurityDefinition(SwaggerAuthSchemes.Bearer, new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Description = """
                JWT Authorization header using the Bearer scheme.

                Enter your access token only (Swagger adds the `Bearer` prefix automatically).

                Example: if your token is `eyJhbG...`, paste `eyJhbG...` into the value field.
                """,
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT"
        });
    }
}

using Api.Configuration;
using Api.Extensions;
using Api.Filters;
using Api.Middleware;
using Api.Swagger;
using Application;
using Infrastructure;
using Serilog;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Api.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
        app.UseMiddleware<SecurityHeadersMiddleware>();
        app.UseMiddleware<CorrelationIdMiddleware>();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwaggerDocumentation();
        }
        else
        {
            app.UseHsts();
        }

        app.UseSerilogRequestLogging();
        app.UseHttpsRedirection();
        app.UseCors(CorsSettings.DefaultPolicyName);
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        return app;
    }

    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers(options =>
            {
                options.Filters.Add<ApiResponseEnrichmentFilter>();
            })
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            });

        services.AddEndpointsApiExplorer();
        services.AddSwaggerDocumentation();
        services.AddAuthRateLimiting();
        services.AddCorsPolicy(configuration);

        services.AddApplication();
        services.AddInfrastructure(configuration);

        services.AddHealthChecks();

        return services;
    }
}

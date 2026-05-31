using Application.Common.Constants;
using Domain.Constants;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Authentication;

public static class AuthorizationConfiguration
{
    public static IServiceCollection AddAuthorizationPolicies(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy(AuthorizationPolicies.AdminOnly, policy =>
                policy.RequireRole(RoleNames.Admin));

            options.AddPolicy(AuthorizationPolicies.UserOrAdmin, policy =>
                policy.RequireRole(RoleNames.User, RoleNames.Admin));
        });

        return services;
    }
}

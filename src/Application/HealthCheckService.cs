using Application.Common.Interfaces;

namespace Application;

public sealed class HealthCheckService : IHealthCheckService
{
    public string GetStatus() => "Healthy";
}

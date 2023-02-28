using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Bitwarden.SecretOperator.CliWrapping;

public class BasicHealthcheck : IHealthCheck
{

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = new CancellationToken())
    {
        return Task.FromResult(HealthCheckResult.Healthy());
    }
}
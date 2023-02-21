using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Bitwarden.SecretOperator.CliWrapping;

public class ReadyHealthcheck : IHealthCheck
{
    private readonly BitwardenCliWrapper _cliWrapper;

    public ReadyHealthcheck(BitwardenCliWrapper cliWrapper) => _cliWrapper = cliWrapper;

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = new CancellationToken())
    {
        if (!_cliWrapper.IsReady)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy());
        }
        return Task.FromResult(HealthCheckResult.Healthy());
    }
}
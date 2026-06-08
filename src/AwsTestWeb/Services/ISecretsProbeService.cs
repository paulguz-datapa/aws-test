using AwsTestWeb.Models;

namespace AwsTestWeb.Services;

public interface ISecretsProbeService
{
    Task<ProbeResult> RunAsync(CancellationToken cancellationToken);
}

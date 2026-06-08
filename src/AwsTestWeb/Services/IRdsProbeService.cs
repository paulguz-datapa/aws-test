using AwsTestWeb.Models;

namespace AwsTestWeb.Services;

public interface IRdsProbeService
{
    Task<ProbeResult> RunAsync(CancellationToken cancellationToken);
}

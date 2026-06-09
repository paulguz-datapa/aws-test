using AwsTestWeb.Models;
using AwsTestWeb.Options;
using AwsTestWeb.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AwsTestWeb.Pages;

public class IndexModel(
    IRdsProbeService rdsProbeService,
    ISecretsProbeService secretsProbeService,
    IConfiguration configuration,
    RdsOptions rdsOptions,
    SecretOptions secretOptions) : PageModel
{
    public sealed record ConfigValue(string Name, string Value);

    public ProbeResult? RdsResult { get; private set; }

    public ProbeResult? SecretResult { get; private set; }

    public IReadOnlyList<ConfigValue> RequiredEnvironmentValues { get; } = BuildRequiredEnvironmentValues(configuration, rdsOptions, secretOptions);

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostTestRdsAsync(CancellationToken cancellationToken)
    {
        RdsResult = await rdsProbeService.RunAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostTestSecretAsync(CancellationToken cancellationToken)
    {
        SecretResult = await secretsProbeService.RunAsync(cancellationToken);
        return Page();
    }

    private static IReadOnlyList<ConfigValue> BuildRequiredEnvironmentValues(
        IConfiguration configuration,
        RdsOptions rdsOptions,
        SecretOptions secretOptions)
    {
        var regionSource = GetRegionSource(configuration);
        var regionValue = string.IsNullOrWhiteSpace(rdsOptions.Region)
            ? "(not set)"
            : $"{rdsOptions.Region} (from {regionSource})";

        return
        [
            new("Region", regionValue),
            new("RDS_HOST", rdsOptions.Host ?? "(not set)"),
            new("RDS_PORT", rdsOptions.Port.ToString()),
            new("RDS_DATABASE", rdsOptions.Database ?? "(not set)"),
            new("RDS_USERNAME", rdsOptions.Username ?? "(not set)"),
            new("RDS_QUERY", rdsOptions.Query),
            new("RDS_SSL_MODE", rdsOptions.SslMode),
            new("RDS_ROOT_CERTIFICATE", string.IsNullOrWhiteSpace(rdsOptions.RootCertificatePath) ? "(not set)" : rdsOptions.RootCertificatePath),
            new("SECRET_ID", secretOptions.SecretId ?? "(not set)"),
            new("SECRET_JSON_KEY", string.IsNullOrWhiteSpace(secretOptions.KeyName) ? "(not set)" : secretOptions.KeyName)
        ];
    }

    private static string GetRegionSource(IConfiguration configuration)
    {
        if (!string.IsNullOrWhiteSpace(configuration["RDS_REGION"]))
        {
            return "RDS_REGION";
        }

        if (!string.IsNullOrWhiteSpace(configuration["AWS_REGION"]))
        {
            return "AWS_REGION";
        }

        if (!string.IsNullOrWhiteSpace(configuration["AWS_DEFAULT_REGION"]))
        {
            return "AWS_DEFAULT_REGION";
        }

        return "no region variable";
    }
}

using AwsTestWeb.Models;
using AwsTestWeb.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AwsTestWeb.Pages;

public class IndexModel(
    IRdsProbeService rdsProbeService,
    ISecretsProbeService secretsProbeService) : PageModel
{
    public ProbeResult? RdsResult { get; private set; }

    public ProbeResult? SecretResult { get; private set; }

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
}

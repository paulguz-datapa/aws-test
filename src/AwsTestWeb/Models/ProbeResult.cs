namespace AwsTestWeb.Models;

public sealed record ProbeResult(bool Success, string Heading, string Message)
{
    public string AlertCssClass => Success ? "alert-success" : "alert-danger";
}

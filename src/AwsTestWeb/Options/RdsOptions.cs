namespace AwsTestWeb.Options;

public sealed class RdsOptions
{
    public string? Region { get; init; }

    public string? Host { get; init; }

    public int Port { get; init; } = 5432;

    public string? Database { get; init; }

    public string? Username { get; init; }

    public string Query { get; init; } = "select id from dev.dashboards order by id limit 1";

    public string SslMode { get; init; } = "Require";

    public string? RootCertificatePath { get; init; }
}

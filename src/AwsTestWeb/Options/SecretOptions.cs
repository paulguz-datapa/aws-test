namespace AwsTestWeb.Options;

public sealed class SecretOptions
{
    public string? SecretId { get; init; }

    public string KeyName { get; init; } = "iPhone passcode";
}

using AwsTestWeb.Options;

namespace AwsTestWeb.Services;

public static class ProbeConfigurationValidator
{
    public static IReadOnlyList<string> GetMissingRdsSettings(RdsOptions options)
    {
        var missing = new List<string>();

        if (string.IsNullOrWhiteSpace(options.Region))
        {
            missing.Add("RDS_REGION, AWS_REGION, or AWS_DEFAULT_REGION");
        }

        if (string.IsNullOrWhiteSpace(options.Host))
        {
            missing.Add("RDS_HOST");
        }

        if (string.IsNullOrWhiteSpace(options.Database))
        {
            missing.Add("RDS_DATABASE");
        }

        if (string.IsNullOrWhiteSpace(options.Username))
        {
            missing.Add("RDS_USERNAME");
        }

        return missing;
    }

    public static IReadOnlyList<string> GetMissingSecretSettings(SecretOptions options)
    {
        var missing = new List<string>();

        if (string.IsNullOrWhiteSpace(options.SecretId))
        {
            missing.Add("SECRET_ID");
        }

        if (string.IsNullOrWhiteSpace(options.KeyName))
        {
            missing.Add("SECRET_JSON_KEY");
        }

        return missing;
    }
}

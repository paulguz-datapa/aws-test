using AwsTestWeb.Options;
using AwsTestWeb.Services;

namespace AwsTestWeb.Tests;

public class ProbeConfigurationValidatorTests
{
    [Fact]
    public void GetMissingRdsSettings_ReturnsAllRequiredKeys_WhenUnset()
    {
        var options = new RdsOptions();

        var missingSettings = ProbeConfigurationValidator.GetMissingRdsSettings(options);

        Assert.Equal(
            new[] { "RDS_REGION, AWS_REGION, or AWS_DEFAULT_REGION", "RDS_HOST", "RDS_DATABASE", "RDS_USERNAME" },
            missingSettings);
    }

    [Fact]
    public void GetMissingRdsSettings_ReturnsEmpty_WhenRequiredValuesExist()
    {
        var options = new RdsOptions
        {
            Region = "eu-west-2",
            Host = "example.cluster-123.eu-west-2.rds.amazonaws.com",
            Database = "appdb",
            Username = "app_user"
        };

        var missingSettings = ProbeConfigurationValidator.GetMissingRdsSettings(options);

        Assert.Empty(missingSettings);
    }

    [Fact]
    public void GetMissingSecretSettings_ReturnsSecretId_WhenMissing()
    {
        var options = new SecretOptions
        {
            KeyName = "iPhone passcode"
        };

        var missingSettings = ProbeConfigurationValidator.GetMissingSecretSettings(options);

        Assert.Equal(new[] { "SECRET_ID" }, missingSettings);
    }
}

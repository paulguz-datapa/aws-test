using System.Text.Json;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using AwsTestWeb.Models;
using AwsTestWeb.Options;

namespace AwsTestWeb.Services;

public sealed class SecretsProbeService(
    IAmazonSecretsManager secretsManager,
    SecretOptions options,
    ILogger<SecretsProbeService> logger) : ISecretsProbeService
{
    public async Task<ProbeResult> RunAsync(CancellationToken cancellationToken)
    {
        var missingSettings = ProbeConfigurationValidator.GetMissingSecretSettings(options);
        if (missingSettings.Count > 0)
        {
            return new ProbeResult(
                false,
                "Secrets test failed",
                $"Missing configuration: {string.Join(", ", missingSettings)}.");
        }

        try
        {
            var response = await secretsManager.GetSecretValueAsync(
                new GetSecretValueRequest
                {
                    SecretId = options.SecretId
                },
                cancellationToken);

            if (string.IsNullOrWhiteSpace(response.SecretString))
            {
                return new ProbeResult(
                    false,
                    "Secrets test failed",
                    "The secret was found, but it did not return a SecretString payload to inspect.");
            }

            var containsKey = SecretPayloadInspector.ContainsKey(response.SecretString, options.KeyName);
            if (!containsKey)
            {
                return new ProbeResult(
                    false,
                    "Secrets Manager key not found",
                    $"The secret was read successfully, but the \"{options.KeyName}\" key was not present.");
            }

            return new ProbeResult(
                true,
                "Secrets Manager access succeeded",
                $"The secret was read successfully and contains the \"{options.KeyName}\" key.");
        }
        catch (ResourceNotFoundException ex)
        {
            logger.LogError(ex, "Secret {SecretId} was not found.", options.SecretId);
            return new ProbeResult(
                false,
                "Secrets test failed",
                "The configured secret could not be found.",
                ex.ToString());
        }
        catch (DecryptionFailureException ex)
        {
            logger.LogError(ex, "Failed to decrypt secret {SecretId}.", options.SecretId);
            return new ProbeResult(
                false,
                "Secrets test failed",
                "The secret could not be decrypted. Check the runtime role and KMS permissions.",
                ex.ToString());
        }
        catch (InvalidRequestException ex)
        {
            logger.LogError(ex, "Invalid request for secret {SecretId}.", options.SecretId);
            return new ProbeResult(
                false,
                "Secrets test failed",
                "AWS rejected the secret read request. Check the secret configuration and ECS service region.",
                ex.ToString());
        }
        catch (InvalidParameterException ex)
        {
            logger.LogError(ex, "Invalid parameter for secret probe.");
            return new ProbeResult(
                false,
                "Secrets test failed",
                "The configured secret id or key name is invalid.",
                ex.ToString());
        }
        catch (AmazonSecretsManagerException ex)
        {
            logger.LogError(ex, "Secrets Manager probe failed.");
            return new ProbeResult(
                false,
                "Secrets test failed",
                "Could not read the secret from Secrets Manager. Check the runtime role permissions and region.",
                ex.ToString());
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Secret payload for {SecretId} is not valid JSON.", options.SecretId);
            return new ProbeResult(
                false,
                "Secrets test failed",
                "The secret was read, but its value is not valid JSON.",
                ex.ToString());
        }
    }
}

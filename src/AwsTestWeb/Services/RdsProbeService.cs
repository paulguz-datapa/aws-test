using Amazon.Runtime;
using AwsTestWeb.Models;
using AwsTestWeb.Options;
using Npgsql;

namespace AwsTestWeb.Services;

public sealed class RdsProbeService(
    RdsOptions options,
    IRdsAuthTokenProvider authTokenProvider,
    ILogger<RdsProbeService> logger) : IRdsProbeService
{
    public async Task<ProbeResult> RunAsync(CancellationToken cancellationToken)
    {
        var missingSettings = ProbeConfigurationValidator.GetMissingRdsSettings(options);
        if (missingSettings.Count > 0)
        {
            return new ProbeResult(
                false,
                "RDS test failed",
                $"Missing configuration: {string.Join(", ", missingSettings)}.");
        }

        try
        {
            var authToken = authTokenProvider.Generate(
                options.Region!,
                options.Host!,
                options.Port,
                options.Username!);

            var connectionStringBuilder = new NpgsqlConnectionStringBuilder
            {
                Host = options.Host,
                Port = options.Port,
                Database = options.Database,
                Username = options.Username,
                Password = authToken,
                SslMode = ParseSslMode(options.SslMode),
                Pooling = false,
                Timeout = 15,
                CommandTimeout = 15
            };

            if (!string.IsNullOrWhiteSpace(options.RootCertificatePath))
            {
                connectionStringBuilder.RootCertificate = options.RootCertificatePath;
            }

            await using var connection = new NpgsqlConnection(connectionStringBuilder.ConnectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = new NpgsqlCommand(options.Query, connection);
            var result = await command.ExecuteScalarAsync(cancellationToken);

            if (result is null || result == DBNull.Value)
            {
                return new ProbeResult(
                    false,
                    "RDS query returned no rows",
                    "Connected successfully, but dev.dashboards did not return an id.");
            }

            return new ProbeResult(
                true,
                "RDS access succeeded",
                $"First dashboard id: {result}");
        }
        catch (ArgumentException ex)
        {
            logger.LogError(ex, "Invalid RDS configuration for probe.");
            return new ProbeResult(
                false,
                "RDS test failed",
                "The RDS configuration is invalid. Check the host name, region, and connection settings.");
        }
        catch (AmazonClientException ex)
        {
            logger.LogError(ex, "Failed to generate an RDS IAM auth token.");
            return new ProbeResult(
                false,
                "RDS test failed",
                "Could not generate an IAM authentication token. Check the app role and AWS region configuration.");
        }
        catch (NpgsqlException ex)
        {
            logger.LogError(ex, "PostgreSQL connectivity probe failed.");
            return new ProbeResult(
                false,
                "RDS test failed",
                "Could not connect to PostgreSQL with IAM authentication. Check network access, IAM DB auth, TLS, and database permissions.");
        }
        catch (TimeoutException ex)
        {
            logger.LogError(ex, "Timed out connecting to PostgreSQL.");
            return new ProbeResult(
                false,
                "RDS test timed out",
                "Connecting to PostgreSQL timed out. Check the ECS Express networking, security groups, and database endpoint.");
        }
    }

    private static SslMode ParseSslMode(string? sslMode)
    {
        return Enum.TryParse<SslMode>(sslMode, true, out var parsedSslMode)
            ? parsedSslMode
            : SslMode.Require;
    }
}

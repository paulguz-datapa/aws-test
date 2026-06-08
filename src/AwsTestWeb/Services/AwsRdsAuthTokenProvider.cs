using Amazon.RDS.Util;

namespace AwsTestWeb.Services;

public sealed class AwsRdsAuthTokenProvider : IRdsAuthTokenProvider
{
    public string Generate(string region, string host, int port, string username)
    {
        return RDSAuthTokenGenerator.GenerateAuthToken(host, port, username);
    }
}

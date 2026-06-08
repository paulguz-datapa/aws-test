namespace AwsTestWeb.Services;

public interface IRdsAuthTokenProvider
{
    string Generate(string region, string host, int port, string username);
}

using System.Text.Json;
using AwsTestWeb.Services;

namespace AwsTestWeb.Tests;

public class SecretPayloadInspectorTests
{
    [Fact]
    public void ContainsKey_ReturnsTrue_WhenKeyExists()
    {
        const string payload = """
            {
              "iPhone passcode": "1234",
              "other": "value"
            }
            """;

        var containsKey = SecretPayloadInspector.ContainsKey(payload, "iPhone passcode");

        Assert.True(containsKey);
    }

    [Fact]
    public void ContainsKey_ReturnsFalse_WhenKeyIsMissing()
    {
        const string payload = """
            {
              "other": "value"
            }
            """;

        var containsKey = SecretPayloadInspector.ContainsKey(payload, "iPhone passcode");

        Assert.False(containsKey);
    }

    [Fact]
    public void ContainsKey_ThrowsJsonException_WhenPayloadIsInvalid()
    {
        const string payload = "not-json";

        Assert.ThrowsAny<JsonException>(() => SecretPayloadInspector.ContainsKey(payload, "iPhone passcode"));
    }
}

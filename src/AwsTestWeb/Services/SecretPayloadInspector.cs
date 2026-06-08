using System.Text.Json;

namespace AwsTestWeb.Services;

public static class SecretPayloadInspector
{
    public static bool ContainsKey(string secretPayload, string keyName)
    {
        using var document = JsonDocument.Parse(secretPayload);

        return document.RootElement.ValueKind == JsonValueKind.Object
            && document.RootElement.TryGetProperty(keyName, out _);
    }
}

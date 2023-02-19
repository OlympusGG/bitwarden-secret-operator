using System.Text.Json.Serialization;

namespace Bitwarden.SecretOperator.CliWrapping;

public class ItemField
{
    [JsonPropertyName("name")]
    public string Name { get; set; }
    [JsonPropertyName("value")]
    public string Value { get; set; }
}
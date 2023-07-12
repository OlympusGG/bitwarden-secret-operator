using System.Text.Json.Serialization;

namespace Bitwarden.SecretOperator.CliWrapping;

public class BitwardenCliResponse<T>
{
    [JsonPropertyName("data")]
    public T Data { get; set; } = default!;

    [JsonPropertyName("success")]
    public bool Success { get; set; }
}
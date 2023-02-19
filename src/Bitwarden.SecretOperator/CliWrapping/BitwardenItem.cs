using System.Text.Json.Serialization;

namespace Bitwarden.SecretOperator.CliWrapping;

public class BitwardenItem
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("note")]
    public string? Note { get; set; }

    [JsonPropertyName("fields")]
    public List<ItemField> Fields { get; set; }
}
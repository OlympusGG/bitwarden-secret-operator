namespace Bitwarden.SecretOperator;

public class BitwardenOperatorOptions
{
    public TimeSpan RefreshRate { get; set; } = TimeSpan.FromSeconds(15);
    public TimeSpan DelayAfterFailedWebhook { get; set; } = TimeSpan.FromSeconds(30);
}
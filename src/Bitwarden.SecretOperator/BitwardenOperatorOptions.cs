namespace Bitwarden.SecretOperator;

public class BitwardenOperatorOptions
{
    public TimeSpan RefreshRate { get; set; } = TimeSpan.FromSeconds(300); // every 5 minutes
    public TimeSpan? DelayAfterFailedWebhook { get; set; }
}
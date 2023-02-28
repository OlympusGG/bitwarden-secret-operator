using KubeOps.Operator.Entities.Annotations;

namespace Bitwarden.SecretOperator.CRDs;

public class ElementSpec
{
    [Description("Name of the Bitwarden `id` field")]
    [Pattern(@"^(\{{0,1}([0-9a-fA-F]){8}-([0-9a-fA-F]){4}-([0-9a-fA-F]){4}-([0-9a-fA-F]){4}-([0-9a-fA-F]){12}\}{0,1})$")]
    public string? BitwardenId { get; set; }

    [Description("Name of the Bitwarden `field` to use")]
    public string? BitwardenSecretField { get; set; }
    
    [Description("Tells whether or not to use `note` instead of `fields`")]
    public bool? BitwardenUseNote { get; set; }

    [Description("Name of the Kubernetes Secret key")]
    [Required]
    public string KubernetesSecretKey { get; set; }
    
    
    [Description("Name of the Kubernetes Secret Value")]
    public string? KubernetesSecretValue { get; set; }
}
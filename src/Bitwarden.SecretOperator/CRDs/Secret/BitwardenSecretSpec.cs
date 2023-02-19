using KubeOps.Operator.Entities.Annotations;

namespace Bitwarden.SecretOperator.CRDs.Secret;

public class BitwardenSecretSpec
{
    [Description("Name of the Kubernetes Secret, defaults to the same name of the CRD")]
    public string? Name { get; set; }

    [Description("Name of the Kubernetes Secret Namespace, defaults to the same namespace of the CRD")]
    public string? Namespace { get; set; }
    
    [Description("Name of the Bitwarden Secret, optional and can be overriden by fields in `content.bitwardenId`")]
    public string? BitwardenId { get; set; }
    
    [Description("A set of labels to put to the secret resource")]
    public Dictionary<string, string>? Labels { get; set; }

    [Description("Content of secret")]
    [Required]
    public List<ElementSpec> Content { get; set; }
    
    [Description("A set of string data to put to the secret")]
    public Dictionary<string, string>? StringData { get; set; }
}
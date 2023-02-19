using Bitwarden.SecretOperator.CRDs.Secret;
using k8s.Models;
using KubeOps.Operator.Entities;

namespace Bitwarden.SecretOperator.CRDs;

[KubernetesEntity(Kind = "BitwardenSecret", Group = "bitwarden-secret-operator.io", ApiVersion = "v1beta1")]
public class BitwardenSecretCrd : CustomKubernetesEntity<BitwardenSecretSpec>
{
}
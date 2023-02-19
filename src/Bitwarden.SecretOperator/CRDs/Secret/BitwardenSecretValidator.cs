using KubeOps.Operator.Webhooks;

namespace Bitwarden.SecretOperator.CRDs.Secret;

// public class BitwardenSecretValidator : IValidationWebhook<BitwardenSecretCrd>
// {
//     public AdmissionOperations Operations => AdmissionOperations.Create | AdmissionOperations.Update;
//
//     public ValidationResult Create(BitwardenSecretCrd newEntity, bool dryRun)
//     {
//         BitwardenSecretSpec spec = newEntity.Spec;
//
//         // global
//         if (spec.BitwardenId is not null)
//         {
//             foreach (ElementSpec elementSpec in spec.Content.Where(s => s.BitwardenId == null))
//             {
//                 elementSpec.BitwardenId = spec.BitwardenId;
//             }
//         }
//
//         // bitwarden ID not specified
//         if (spec.Content.Any(s => s.BitwardenId == null && s.KubernetesSecretValue == null))
//         {
//             IEnumerable<string> invalidSpecs = spec.Content.Where(s => s.BitwardenId == null).Select(s => s.KubernetesSecretKey);
//             return ValidationResult.Fail(StatusCodes.Status400BadRequest, $@"KubernetesSecretKey have no bitwardenId: {string.Join(',', invalidSpecs)}");
//         }
//
//         return ValidationResult.Success("");
//     }
//
//     public ValidationResult Update(BitwardenSecretCrd newEntity, bool dryRun)
//     {
//         return ValidationResult.Success("");
//     }
// }
using Bitwarden.SecretOperator.CliWrapping;
using k8s.Models;
using KubeOps.KubernetesClient;
using KubeOps.Operator.Controller;
using KubeOps.Operator.Controller.Results;
using KubeOps.Operator.Rbac;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Bitwarden.SecretOperator.CRDs.Secret;

[EntityRbac(typeof(BitwardenSecretCrd), Verbs = RbacVerb.All)]
[EntityRbac(typeof(V1Secret), Verbs = RbacVerb.All)]
public class BitwardenSecretController : ControllerBase, IResourceController<BitwardenSecretCrd>
{
    private readonly BitwardenOperatorOptions _operatorOptions;
    private readonly ILogger<BitwardenSecretController> _logger;
    private readonly KubernetesClient _kubernetesClient;
    private readonly BitwardenCliWrapper _cliWrapper;

    public BitwardenSecretController(ILogger<BitwardenSecretController> logger, KubernetesClient kubernetesClient, BitwardenCliWrapper cliWrapper, IOptions<BitwardenOperatorOptions> operatorOptions)
    {
        _logger = logger;
        _kubernetesClient = kubernetesClient;
        _cliWrapper = cliWrapper;
        _operatorOptions = operatorOptions.Value;
    }

    public async Task<ResourceControllerResult?> ReconcileAsync(BitwardenSecretCrd entity)
    {
        try
        {
            BitwardenSecretSpec spec = entity.Spec;


            string? destinationName = spec.Name ?? entity.Name();
            string? destinationNamespace = spec.Namespace ?? entity.Namespace();
            var secret = await _kubernetesClient.Get<V1Secret>(destinationName, destinationNamespace);
            if (secret == null)
            {
                _logger.LogInformation("Secret: {SecretName} in namespace: {Namespace} couldn't be found, creating it", destinationName, destinationNamespace);
                // create
                secret = await entity.GetSecretAsync(_cliWrapper);
                secret = await _kubernetesClient.Create<V1Secret>(secret);
                _logger.LogInformation("Secret: {SecretName} in namespace: {Namespace} created", destinationName, destinationNamespace);
            }
            else
            {
                _logger.LogInformation("Secret: {SecretName} in namespace: {Namespace} exists, updating it", destinationName, destinationNamespace);
                secret = await entity.GetSecretAsync(_cliWrapper);
                await _kubernetesClient.Update<V1Secret>(secret);
                _logger.LogInformation("Secret: {SecretName} in namespace: {Namespace} updated", destinationName, destinationNamespace);
            }

            // success
            return null;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[{Method}] Failed", nameof(ReconcileAsync));
            // requeue the event in 15 seconds
            if (_operatorOptions.DelayAfterFailedWebhook is null)
            {
                throw;
            }
                
            return ResourceControllerResult.RequeueEvent(_operatorOptions.DelayAfterFailedWebhook.Value);
        }
    }

    public async Task StatusModifiedAsync(BitwardenSecretCrd entity)
    {
        return;
    }

    public async Task DeletedAsync(BitwardenSecretCrd entity)
    {
        try
        {
            await _kubernetesClient.Delete<V1Secret>(entity.Spec.Name, entity.Spec.Namespace);
        }
        catch (Exception e)
        {
            _logger.LogError(e, nameof(DeletedAsync));
        }
    }
}
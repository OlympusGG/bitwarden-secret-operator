using Bitwarden.SecretOperator.CliWrapping;
using k8s.Models;
using KubeOps.KubernetesClient;
using KubeOps.Operator.Controller;
using KubeOps.Operator.Controller.Results;
using KubeOps.Operator.Events;
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
    private readonly IEventManager _eventManager;

    public BitwardenSecretController(ILogger<BitwardenSecretController> logger, KubernetesClient kubernetesClient, BitwardenCliWrapper cliWrapper, IOptions<BitwardenOperatorOptions> operatorOptions, IEventManager eventManager)
    {
        _logger = logger;
        _kubernetesClient = kubernetesClient;
        _cliWrapper = cliWrapper;
        _eventManager = eventManager;
        _operatorOptions = operatorOptions.Value;
    }

    public async Task<ResourceControllerResult?> ReconcileAsync(BitwardenSecretCrd entity)
    {
            BitwardenSecretSpec spec = entity.Spec;

            string? destinationName = spec.Name ?? entity.Name();
            string? destinationNamespace = spec.Namespace ?? entity.Namespace();
        try
        {
            await _eventManager.PublishAsync(entity, "bitwarden-secret-operator", "Received reconcile");
            var secret = await _kubernetesClient.Get<V1Secret>(destinationName, destinationNamespace);
            if (secret == null)
            {
                await _eventManager.PublishAsync(entity, "bitwarden-secret-operator", $"Couldn't find secret {destinationName} in namespace {destinationNamespace}, creating it...");
                _logger.LogInformation("Secret: {SecretName} in namespace: {Namespace} couldn't be found, creating it", destinationName, destinationNamespace);
                // create
                secret = await entity.GetSecretAsync(_cliWrapper);
                secret = await _kubernetesClient.Create<V1Secret>(secret);
                
                // created events
                await _eventManager.PublishAsync(secret, "bitwarden-secret-operator", $"Secret {destinationName} in namespace {destinationNamespace}, created");
                await _eventManager.PublishAsync(entity, "bitwarden-secret-operator", $"Secret {destinationName} in namespace {destinationNamespace}, creation successful");
                _logger.LogInformation("Secret: {SecretName} in namespace: {Namespace} created", destinationName, destinationNamespace);
            }
            else
            {
                await _eventManager.PublishAsync(entity, "bitwarden-secret-operator", $"Secret {destinationName} in namespace {destinationNamespace}, updating it...");
                _logger.LogInformation("Secret: {SecretName} in namespace: {Namespace} exists, updating it", destinationName, destinationNamespace);
                secret = await entity.GetSecretAsync(_cliWrapper);
                await _kubernetesClient.Update<V1Secret>(secret);
                
                // updated events
                await _eventManager.PublishAsync(entity, "bitwarden-secret-operator", $"Secret {destinationName} in namespace {destinationNamespace}, updated!");
                await _eventManager.PublishAsync(secret, "bitwarden-secret-operator", $"Secret {destinationName} in namespace {destinationNamespace}, updated");
                
                _logger.LogInformation("Secret: {SecretName} in namespace: {Namespace} updated", destinationName, destinationNamespace);
            }

            // success
            return null;
        }
        catch (Exception e)
        {
            await _eventManager.PublishAsync(entity, "bitwarden-secret-operator", $"Secret {destinationName} in namespace {destinationNamespace}, failed to create, check operator logs");
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
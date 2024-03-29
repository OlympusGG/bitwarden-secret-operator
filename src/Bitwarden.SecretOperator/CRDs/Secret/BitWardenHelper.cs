﻿using System.Data;
using System.Text;
using Bitwarden.SecretOperator.CliWrapping;
using Bitwarden.SecretOperator.Helpers;
using k8s.Models;

namespace Bitwarden.SecretOperator.CRDs.Secret;

public static class BitWardenHelper
{
    public const string HASH_LABEL_KEY = "bitwarden-secret-operator.io/hash";
    public static async Task<V1Secret> GetSecretAsync(this BitwardenSecretCrd entity, BitwardenCliWrapper wrapper)
    {
        BitwardenSecretSpec spec = entity.Spec;

        // global
        if (spec.BitwardenId is not null)
        {
            foreach (ElementSpec elementSpec in spec.Content.Where(s => s.BitwardenId == null && s.KubernetesSecretValue == null))
            {
                elementSpec.BitwardenId = spec.BitwardenId;
            }
        }

        // bitwarden ID not specified
        if (spec.Content.Any(s => s.BitwardenId == null && s.KubernetesSecretValue == null))
        {
            IEnumerable<string> invalidSpecs = spec.Content.Where(s => s.BitwardenId == null).Select(s => s.KubernetesSecretKey);
            throw new InvalidDataException($"{entity.Name()} is invalid, no bitwarden id specified for kubernetesSecretKeys: {string.Join(',', invalidSpecs)}");
        }


        Dictionary<string, List<ElementSpec>> toFetch = spec.Content.Where(s => s.BitwardenId != null).GroupBy(s => s.BitwardenId).ToDictionary(specs => specs.Key!, specs => specs.ToList());

        var secrets = new Dictionary<string, byte[]>();
        // fetch from bitwarden
        foreach (KeyValuePair<string, List<ElementSpec>> keyValuePair in toFetch)
        {
            BitwardenItem? item = await wrapper.GetAsync(keyValuePair.Key);
            if (item == null)
            {
                throw new InvalidDataException($"Entity: {entity.Name()}, ID: {keyValuePair.Key} couldn't be fetched for secret: {spec.Name}");
            }

            Dictionary<string, ItemField> fields = item.Fields switch
            {
                { Count: >= 1 } => item.Fields.ToDictionary(s => s.Name),
                _ => new Dictionary<string, ItemField>()
            };

            foreach (ElementSpec element in keyValuePair.Value)
            {
                string? value = GetSecretValue(element, item, fields, keyValuePair.Key);

                secrets[element.KubernetesSecretKey] = Encoding.UTF8.GetBytes(value);
            }
        }

        // raw values
        foreach (ElementSpec rawValues in spec.Content.Where(s => s.KubernetesSecretValue != null))
        {
            secrets[rawValues.KubernetesSecretKey] = Encoding.UTF8.GetBytes(rawValues.KubernetesSecretValue!);
        }

        spec.Labels ??= new Dictionary<string, string>();
        spec.Labels[HASH_LABEL_KEY] = secrets.ComputeHash();

        string? destinationName = spec.Name ?? entity.Name();
        string? destinationNamespace = spec.Namespace ?? entity.Namespace();
        return new V1Secret
        {
            Kind = "Secret",
            Type = spec.Type ?? "Opaque",
            ApiVersion = "v1",
            Metadata = new V1ObjectMeta
            {
                Name = destinationName,
                NamespaceProperty = destinationNamespace,
                Labels = spec.Labels,
            },
            Data = secrets,
            StringData = spec.StringData
        };
    }

    private static string GetSecretValue(ElementSpec element, BitwardenItem item, IReadOnlyDictionary<string, ItemField> fields, string bitwardenId)
    {
        return element switch
        {
            _ when element.KubernetesSecretValue is not null => element.KubernetesSecretValue,
            _ when element.BitwardenUseNote is true && item.Note is not null => item.Note,
            _ when element.BitwardenUseNote is true && item.Note is null => throw new DataException($"invalid note for bitwardenId: {bitwardenId}"),
            _ when element.BitwardenSecretField is not null && fields.ContainsKey(element.BitwardenSecretField) => fields[element.BitwardenSecretField].Value,
            _ when element.BitwardenSecretField is not null && !fields.ContainsKey(element.BitwardenSecretField) => throw new DataException(
                $"invalid field {element.BitwardenSecretField} for bitwardenId: {bitwardenId}"),
            _ => throw new DataException($"invalid field for bitwardenId: {bitwardenId}"),
        };
    }
}
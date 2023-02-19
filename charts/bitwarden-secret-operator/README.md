# bitwarden-secret-operator

bitwarden-secret-operator is a kubernetes Operator written in .NET thanks to [KubeOps](https://github.com/buehler/dotnet-operator-sdk).
The goal is to create kubernetes native secret objects from bitwarden, in our case, it was used mainly for our GitOps powered clusters.

<p align="center">
  <img src="https://github.com/OlympusGG/bitwarden-secret-operator/blob/main/logo.png?raw=true" alt="bitwarden secret operator Logo"/>
</p>

> DISCLAIMER:  
> This project wraps the BitWarden CLI as we didn't want to rewrite a client for BitWarden and BitWarden does not offer easy to use public client libraries
> 
> If you need multi-line (SSH key, Certificate...) like we did, use secure note until BitWarden implements [Multiline support](https://community.bitwarden.com/t/add-an-additional-multi-line-text-field/2165)


## Features

- [x] Automatically refreshing secrets through `bw sync`
- [x] Supporting: fields/notes
- [x] host chart on gh pages
- [x] release pipeline

## Getting started

You will need a `ClientID` and `ClientSecret` ([where to get these](https://bitwarden.com/help/personal-api-key/)) as well as your password.
Expose these to the operator as described in this example:

```yaml
env:
  - name: BW_HOST
    value: "https://bitwarden.your.tld.org"
  - name: BW_CLIENTID
    value: "user.your-client-id"
  - name: BW_CLIENTSECRET
    value: "yourClientSecret"
  - name: BW_PASSWORD
    value: "YourSuperSecurePassword"
```


the helm template will use all environment variables from this secret, so make sure to prepare this secret with the key value pairs as described above.

`BW_HOST` can be omitted if you are using the Bitwarden SaaS offering.

After that it is a basic helm deployment:

```bash
helm repo add bitwarden-operator https://olympusgg.github.io/bitwarden-secret-operator
helm repo update 
kubectl create namespace bw-operator
helm upgrade --install --namespace bw-operator -f values.yaml bw-operator bitwarden-operator/bitwarden-secret-operator
```

## BitwardenSecret

And you are set to create your first secret using this operator. For that you need to add a CRD Object like this to your cluster:

```yaml
---
apiVersion: bitwarden-secret-operator.io/v1beta1
kind: BitwardenSecret
metadata:
  name: my-secret-from-bitwarden
spec:
  name: "my-secret-from-spec" # optional, will use the same name as CRD if not specified
  namespace: "my-namespace" # optional, will use the same namespace as CRD if not specified
  bitwardenId: 00000000-0000-0000-0000-000000000000 # optional, this id applies to all elements without `bitwardenId` specified 
  content:
  - element:
      bitwardenId: d4ff5941-53a4-4622-9385-2fcf910ae7e7 # optional, can be specified for a specific secret
      bitwardenSecretField: myBitwardenField # optional, mutually exclusive with `bitwardenSecretField` but acts as a second choice
      bitwardenNote: false # optional, mutually exclusive and prioritized over `bitwardenSecretField`
      kubernetesSecretKey: MY_KUBERNETES_SECRET_KEY # required
  - element:
      bitwardenNote: true # boolean, exclusive and prioritized over `bitwardenSecretField`
      kubernetesSecretKey: MY_KUBERNETES_SECRET_KEY # required
```

## Credits/Thanks

- [Bitwarden](https://bitwarden.com/) For their product
- [Lerentis](https://github.com/Lerentis) For his BitWarden Operator project that motivated us to do our own one (mostly to fit our specific needs)
- [KubeOps Contributors](https://github.com/buehler/dotnet-operator-sdk) For their work on KubeOps library that helped us building this
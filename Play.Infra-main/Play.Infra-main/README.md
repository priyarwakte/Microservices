# Play.Infra
Play Economy Infrastructure components

## Add the GitHub package source

owner="Microservices-DotNet"
gh_pat="[PAT HERE]"

dotnet nuget add source --username USERNAME --password $gh_pat --store-password-in-clear-text --name github "https://nuget.pkg.github.com/$owner/index.json"
```
## Creating the azure resource group
appname="playeconomy26" 
az group create --name $appname --location eastus

## Creating the Cosmos DB account

appname="playeconomy26" 
az cosmosdb create --name $appname --resource-group $appname --kind MongoDB --enable-free-tier
```

## Creating the Service Bus namespace

az servicebus namespace create --name $appname --resource-group $appname --sku Standard

## Creating the Container registry
```
az acr create --name $appname --resource-group $appname --sku Basic

## Creating the AKS cluster

az feature register --name EnablePodIdentityPreview --namespace Microsoft.ContainerService
az extension add --name aks-preview

az aks create -n $appname -g $appname --node-vm-size Standard_B2s --node-count 2 --attach-acr $appname --enable-pod-identity --network-plugin azure

az aks get-credentials --name $appname --resource-group $appname
```
## Creating the Azure Key Vault

az keyvault create -n $appname -g $appname

## Installing Emissary-ingress

helm repo add datawire https://app.getambassador.io
helm repo update

kubectl apply -f https://app.getambassador.io/yaml/emissary/2.1.0/emissary-crds.yaml
kubectl wait --timeout=90s --for=condition=available deployment emissary-apiext -n emissary-system 

namespace="emissary"
helm install emissary-ingress datawire/emissary-ingress --set service.annotations."service\.beta\.kubernetes\.io/azure-dns-label-name"=$appname -n $namespace --create-namespace 

kubectl rollout status deployment/emissary-ingress -n $namespace -w


## Configuring Emissary-ingress routing

kubectl apply -f ./emissary-ingress/listener.yaml -n $namespace
kubectl apply -f ./emissary-ingress/mappings.yaml -n $namespace
```
## Installing cert-manager
helm repo add jetstack https://charts.jetstack.io
helm repo update

helm install cert-manager jetstack/cert-manager --version v1.12.0 --set installCRDs=true --namespace $namespace

# Creating the cluster issuer
kubectl apply -f ./cert-manager/cluster-issuer.yaml -n $namespace
kubectl apply -f ./cert-manager/acme-challenge.yaml -n $namespace
```
# Creating the tls certificate
kubectl apply -f ./emissary-ingress/tls-certificate.yaml -n $namespace
```

## Enabling TLS and HTTPS
kubectl apply -f ./emissary-ingress/host.yaml -n $namespace

## Packaging and publishing the microservice Helm chart
helm package ./helm/microservice


helmUser="00000000-0000-0000-0000-000000000000"

helmPassword=$(az acr login --name playeconomy26 --expose-token --output tsv --query accessToken)
export HELM_EXPERIMENTAL_OCI=1
helm registry login "playeconomy26.azurecr.io" --username $helmUser --password $helmPassword

helm push microservice-0.1.0.tgz oci://$appname.azurecr.io/helm

## Create GitHub service principal
appId=$(az ad sp create-for-rbac -n "GitHub" --skip-assignment --query appId --output tsv)

az role assignment create --assignee $appId --role "AcrPush" --resource-group $appname
az role assignment create --assignee $appId --role "Azure Kubernetes Service Cluster User Role" --resource-group $appname
az role assignment create --assignee $appId --role "Azure Kubernetes Service Contributor Role" --resource-group $appname


## Deploying Seq to AKS
```powershell
helm repo add datalust https://helm.datalust.co
helm repo update

helm install seq datalust/seq -n observability --create-namespace
# Play.Catalog
Play Economy Catalog microservice

## Create and publish package
```powershell
version="1.0.4"
owner="Microservices-DotNet"
gh_pat="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"

dotnet pack src/Play.Catalog.Contracts/ --configuration Release -p:PackageVersion=$version -p:RepositoryUrl=https://github.com/$owner/Play.Catalog -o ../packages

dotnet nuget push ../packages/Play.Catalog.Contracts.$version.nupkg --api-key $gh_pat --source "github"

## Build the docker image

export GH_OWNER="Microservices-DotNet"
appname="playeconomy26" 
export GH_PAT="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"
docker build --secret id=GH_OWNER --secret id=GH_PAT -t "$appname.azurecr.io/play.catalog:$version" .
docker build --secret id=GH_OWNER --secret id=GH_PAT -t play.catalog:$version .

## Run the docker image

export cosmosDbConnString="[CONN STRING HERE]"
"mongodb://playeconomy26:2u3mzneRiPTGVJ142vKB9GQMc3JCEc56LZ6CdcumwvTwiUM42Ex35JtDihyWmLM2y5uOc7Egbs3SACDbR7Iagg==@playeconomy26.mongo.cosmos.azure.com:10255/?ssl=true&replicaSet=globaldb&retrywrites=false&maxIdleTimeMS=120000&appName=@playeconomy26@"
export serviceBusConnString="[CONN STRING HERE]"
"Endpoint=sb://playeconomy26.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=fUJcRoUknMJpyfML7pSXwJUnv5nwJ904z+ASbEOZW2s="
docker run -it --rm -p 5142:5142 --name catalog -e MongoDbSettings__ConnectionString=$cosmosDbConnString -e ServiceBusSettings__ConnectionString=$serviceBusConnString -e ServiceSettings__MessageBroker="SERVICEBUS" play.catalog:$version

## Publishing the Docker image
```powershell
az acr login --name $appname
docker push "$appname.azurecr.io/play.catalog:$version"

## Creating the pod managed identity
```powershell
namespace="catalog"
az identity create --resource-group $appname --name $namespace
$IDENTITY_RESOURCE_ID=az identity show -g $appname -n $namespace --query id -otsv

az aks pod-identity add --resource-group $appname --cluster-name $appname --namespace $namespace --name $namespace --identity-resource-id $IDENTITY_RESOURCE_ID
```

## Granting access to Key Vault secrets
```powershell
$IDENTITY_CLIENT_ID=az identity show -g $appname -n $namespace --query clientId -otsv
az keyvault set-policy -n $appname --secret-permissions get list --spn $IDENTITY_CLIENT_ID

## Creating the Kubernetes resources
```powershell
kubectl apply -f ./kubernetes/catalog.yaml -n $namespace
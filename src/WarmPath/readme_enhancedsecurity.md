# Deploy warm path

1. In the `src/WarmPath/WarmPathDeployment_EnhancedSecurity` directory, open the `azuredeploy.parameters.json` file. Add values for the following parameters:

    - `appName`: A name for the Function app (of less than 11 characters of lowercase letter and numbers only).
    - `eventHubConnectionString`: The Event Hub-compatible endpoint connection string.
    - `eventHubName`: The Event Hub-compatible name.
    - `eventHubConsumerGroup`: The name of the Event Hub consumer group.
    - `dnsSuffix`: Root domain associated with the ASE (e.g. contoso.com).
    - `aseLocation`: Location of the App Service Environment (e.g. Central US).

2. Deploy the Azure resources for the warm path

> Note: the next steps gets Warm path deployed into a vnet associated to an Azure Application Service Environment with Internal Load Balancer. If you have errors when creating the Cosmos database check the Cosmos DB Account firewall settings on the portal and allow your current IP.

```bash
cd src/WarmPath/WarmPathDeployment_EnhancedSecurity

# Create the resource group
az group create -n <resource-group> -l <location>

# Deploy the resources
az group deployment create --name <deployment-name> \
  -g <resource-group> --template-file azuredeploy.json \
  --parameters azuredeploy.parameters.json

# Get the Cosmos DB account name
az cosmosdb list -g <resource-group> --query [*].name

# Create the Cosmos DB database
# The value of <database-name> must match the cosmosDbDatabase template parameter.
az cosmosdb database create -g <resource-group> --name <db-account> \
  --db-name <database-name>

# Create the collection
# The value of <collection-name> must match the cosmosDBCollection template parameter.
# Note: The throughput parameter ensures the collection can be scaled past 10K RU
az cosmosdb collection create -g <resource-group> --name <db-account> \
  --db-name <database-name> -c <collection-name> \
  --partition-key-path /deviceId --throughput 100000

# Follow the ASE instructions post ILB creation to setup your ILB X509 Certificate

https://docs.microsoft.com/en-us/azure/app-service/environment/create-ilb-ase#post-ilb-ase-creation-validation 

# Ensure the subnet created for your vnet is associated to the network security group

az network vnet subnet update --resource-group <resource-group> --name <storageaccount-name> --network-security-group <appname-ase-NSG>

# Change storage account to Deny as default action 

az storage account update --resource-group <resource-group> --name <storageaccount-name> --default-action Deny

```

Troubleshooting

a. if your Azure Function is failing with a NotFound status code. Please review the Cosmos DB has been configured to allow access to your Azure Virtual Network. For more information please refer to [Azure Docs](https://docs.microsoft.com/en-us/azure/cosmos-db/vnet-service-endpoint)
b. you may need access to kudu for development purposes, if that is the case you will need to setup the FTP credentials from Azure Functions Portal. Please note that in the end to access SCM/Kudu site, it would require either to make the Azure Function accesible using [WAF](https://docs.microsoft.com/en-us/azure/app-service/environment/integrate-with-application-gateway) or create a [VM within the same ASE vnet](https://docs.microsoft.com/en-us/azure/app-service/environment/create-ilb-ase#post-ilb-ase-creation-validation )

3. Secure configuration ends just here. The rest of the setup can be achived by resuming from step #2 in [readme.md](readme.md) 

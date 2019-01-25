# Deployment of Resources for Drone Asa Application
## Requirements
* Cosmosdb
* Azure stream Analytics
* Azure function
* App service
* Application insights
* IoT Hub 

Assumes IoT Hub has been previously deployed. Otherwise follow [this guide](../../README.md)

## Step 1: Deploy Asa Dependent Resources

```bash
cd src/HotPath/HotPathDeployment/AsaResourcesDeployment/HotPathDeployment

az group deployment create -n <deployment-name> \
  -g <resource-group> --template-file azuredeploy.json \
  --parameters azuredeploy.parameters.json
```

## Step 2: Deploy Azure Function application

Open `src\HotPath\DroneHotPathFunction.sln` solution.
Right click on *DroneHotPathFunction* project then select *Publish*. Deploy in the function application created in above step.


## Step 3: Get Function Key

- Open Portal and click on the function application, then click on the function deployed in previous step.

- Click on the *hottelemetry* function.

- Click on manage, then click on *click to show* under function keys.

## Step 4: Get Cosmosdb key

- Open portal click on Cosmos DB account created on the resource group and click on Keys, then click on primarary key (read and write).

- Or run the following az commands.

```Bash
 az cosmosdb list -o table
 az cosmosdb list-keys -n <cosmos_db_account> -g <resource_group_name>
```
 
## Step 5: Create 2 collections one for hotTelemetry and other for absenceTelemetry

- Create the database

```bash
az cosmosdb database create -n <cosmos_db_account> -d hottelemetrydb -g <resource_group_name>
```

- Create absenceTelemetry collection

```Bash
az cosmosdb collection create -c absencetelemetrycol -n <cosmos_db_account> -d hottelemetrydb -g <resource_group_name> --partition-key-path '/deliveryid'
```

- Create hotTelemetry collection

```Bash
az cosmosdb collection create -c hottelemetrycol -n <cosmos_db_account> -d hottelemetrydb -g <resource_group_name> --partition-key-path '/deliveryid'
```
## Step 6: Get IOT hub name and key

- List all IoT hubs in subscription
```Bash
az iot hub list -o table
```

- Get Azure IoT Hub key and copy the portion after SharedAccessKey
```Bash
az iot hub show-connection-string --name <iot_hub_name> -o table
```

## Step 7: Deploy ASA
Open `src\HotPath\HotPathDeployment\AsaDeployment\AsaDeployment.sln` solution. Right click deploy. Click *Edit parameters* and replace the following required parameters, leaving the other ones as default:

Input_droneTelemetry_iotHubNamespace : IoT Hub name

Input_droneTelemetry_iotHubResourceGroup : IoT Hub resource group

Input_droneTelemetry_sharedAccessPolicyKey : IoT Hub SharedAccessKey obtained in step 6

Output_functionAppName : Function App name

Output_function_apiKey: Function App application key obtained in Step 3

Output_cosmos_accountId: Cosmos DB account name

Output_cosmos_accountKey: Cosmos primary key obtained in Step 4

AppInsightsNameInFunctionApp: App Insights name asociated to the Azure Function App

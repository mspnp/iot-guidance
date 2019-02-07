# Deploy cold path analysis using HDInsight

This guide assumes IoT Hub has been previously deployed. Otherwise follow [this guide](../../README.md)

## Step 1:

Fill in the following parameters of src/ColdPath/ColdPathDeployment/azuredeploy.parameters.json:

  clusterName: A name for the HDInsight cluster  
  clusterLoginUserName: an username for the HDInsight cluster  
  clusterLoginPassword: a password for the HDInsight cluster  
  IotHubName: existing IoT Hub name  
  IotHubResourceGroup: resource group name of the IoT Hub

```bash
cd src/ColdPath/ColdPathDeployment

az group deployment create -n <deployment-name> \
  -g <resource-group> --template-file azuredeploy.json \
  --parameters azuredeploy.parameters.json
```

## Step 2
Create a blob storage container named "telemetry" in the telemetry storage account created in step 1.

```bash
az storage container create --name telemetry --account-name <telemetry-storage-account-name> --account-key <telemetry-storage-access-key>
```

## Step 3
Configure IoT Hub instance with a custom Azure Storage container endpoint pointing to the cold telemetry storage container created in step 2.  
Message routing -> Custom endpoints -> Add -> Blob Storage -> Select telemetry storage connainter created in step 2.

More info: https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-devguide-endpoints#custom-endpoints

## Step 4
Configure IoT Hub instance with a default message route that sends device event messages to the default events endpoint.

```bash
az iot hub update -n <iot-hub-name> -g <iot-hub-resource-group> --add properties.routing.routes "{'condition':'true', 'endpointNames':['events'], 'isEnabled':True, 'name':'defaulteventroute', 'source':'DeviceMessages'}"
```

## Step 5
Configure IoT Hub instance with a custom route that sends device event messages to the custom storage endpoint created in step 3.

```bash
az iot hub update -n <iot-hub-name> -g <iot-hub-resource-group-name> --add properties.routing.routes "{'condition':'true', 'endpointNames':['<custom-storage-endpoint-name>'], 'isEnabled':True, 'name':'storageroute', 'source':'DeviceMessages'}"
```

More info: https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-devguide-messages-read-custom

## Step 6
Upload src\ColdPath\HiveApplication\HiveQueries\telemetry.avsc schema to the cold telemetry blob container created in step 2.

> Note: The telemetry.avsc file was generated using the following steps:
> 1. Download the Avro tools (http://avro.apache.org/releases.html#Download).
> 2. Run the following command to extract the schema from a sample of the telemetry
> ```bash
> java -jar $AVRO_TOOLS_PATH/avro-tools-1.8.2.jar getschema $SAMPLE_TELEMETRY_FILE > telemetry.avsc
> ```

## Step 7
Update src\ColdPath\HiveApplication\HiveQueries\createrawtelemetrytable.hql with the Azure Storage account of cold telemetry account created in step 1.
Also update src\ColdPath\HiveApplication\HiveQueries\createrawtelemetrytable.hql with the IoT Hub name.

## Step 8
Using the Hive View of the HDInsight cluster created in step 1, run the following queries:
1. createrawtelemetrytable.hql to create an external Hive table that represents the raw telemetry
2. enrichtelemetry.hql to create a Hive table that enriches the data in the rawtelemetry Hive table.
3. summarizedeliveries.hql to create a Hive table that summarizes the enrichedtelemetrytable by deliveryId.

## Next step:
Use Power BI Desktop to analyze the data in the deliverysummarytable.

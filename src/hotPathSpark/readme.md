# Deployment of Resources for DroneSpark Application
## Requirements
* Scala Version 2.11
* Sbt to assembly necessary jars for Spark Application
* Azure Databricks Premium
* Databricks CLI
* Cosmosdb
* Azure function
* Databricks
* Event Hub
* loganalytics workspace
* App service
* Application Insights
* IoT Hub

Assumes IoT Hub has been previously deployed. Otherwise follow [this guide](../../README.md)

## Deploy Databricks and Dependent Resources
Open `src\HotPathSpark\SparkResourcesDeployment\HotPathResourcesDeployment.sln`
solution.

Right click on project and select deploy.


## Deploy Azure Function application
1. Open `src\HotPath\DroneHotPathFunction.sln` solution.

2. Right click on DroneHotPathFunction project then select Publish. Deploy in the function application created in above step.


## Get Function Key and endpoint
1. Open Portal and click on the function application, then click on function deployed in previous step.

2. Click on the function hottelemetry.

3. Click on function Url to get function endpoint.

4. Click on manage, then click on click to show under function keys.

## Get Cosmosdb key cosmosdb endpoint

1. Open portal click on cosmosdb created on the resource group and click on keys, then click on copy primarary key( read and write) and endpoint.

2. Or run the following az commands:

```Bash
az cosmosdb show --name <cosmos_db_account> -g <resource_group_name>

az cosmosdb list -o table
az cosmosdb list-keys -n <cosmos_db_account> -g <resource_group_name>
```

 ## Create 2 collections one for hotTelemetry and other for absenceTelemetry
1. Run the below command to create the database.

```bash
az cosmosdb database create -n <cosmos_db_account> -d hottelemetrydb -g <resource_group_name>
```

2. Run the below command to create collection for absenceTelemetry.

```Bash
az cosmosdb collection create -c absencetelemetrycol -n <cosmos_db_account> -d hottelemetrydb -g <resource_group_name> --partition-key-path '/deliveryid'
```

3. Run the below command to create collection for hotTelemetry.

```Bash
az cosmosdb collection create -c hottelemetrycol -n <cosmos_db_account> -d hottelemetrydb -g <resource_group_name> --partition-key-path '/deliveryid'
```


## Get the Event Hub compatible endpoint and name for IoT Hub
- On the portal open IoT Hub click on *Built-in endpoints*.

- From *Events* copy Event hub-compatible name and Event hub-compatible endpoint.

## Create 6 consumer  groups in IoT Hub
```Bash
az iot hub consumer-group create --hub-name <iot_hub_name> --resource-group <resource_group_name> --name hotpath2

az iot hub consumer-group create --hub-name <iot_hub_name> --resource-group <resource_group_name> --name hotpath3

az iot hub consumer-group create --hub-name <iot_hub_name> --resource-group <resource_group_name> --name hotpath4

az iot hub consumer-group create --hub-name <iot_hub_name> --resource-group <resource_group_name> --name hotpath5

az iot hub consumer-group create --hub-name <iot_hub_name> --resource-group <resource_group_name> --name hotpath6

az iot hub consumer-group create --hub-name <iot_hub_name> --resource-group <resource_group_name> --name hotpath7
```

## Install Scala and SBT

[Download and install Scala and SBT](https://www.scala-lang.org/download/2.11.12.html)


## Get Event Hub's connection string

On the portal open *Event Hubs Namespace* resource created previously. Click on *Shared access policies* -> *RootManageSharedAccessKey* and copy the *Connection string-primary key*

## Get Log analytics workspace id and primary key

On the portal open *Log analytics workspace* resource created before. Click on *Advanced Settings*. Copy workspace id and primary key.

## Build DroneManagementWrite Application

This step will build the jar for Cosmos writer and Log Analytics writer for the Spark application

Go to `src\HotPathSpark\DroneSparkApplication\DroneManagementWriter` and type:

```
sbt assembly
```

This will create the jar files with all dependencies in one jar into folder `src\hotPathSpark\DroneSparkApplication\DroneManagementWriter\target\scala-2.11`.

## Install databricks CLI and set up authentication by following below instructions

 https://docs.azuredatabricks.net/user-guide/dev-tools/databricks-cli.html

## Create the databricks scope and secrets

```Bash
databricks secrets create-scope --scope hotpathscope

databricks secrets put --scope hotpathscope --key alertingconnstring --string-value "event-hub-connection-string"

databricks secrets put --scope hotpathscope --key cosmosdbendpoint --string-value "cosmosdb-endpoint"

databricks secrets put --scope hotpathscope --key cosmosdbmasterkey --string-value "cosmosdb-primary-key"

databricks secrets put --scope hotpathscope --key functionendpoint --string-value "hottelemetry-function-url"

databricks secrets put --scope hotpathscope --key functionkey --string-value "hotetelemetry-function-key"

databricks secrets put --scope hotpathscope --key iothubconnstring --string-value "iot-hub-eventhub-compatible-connection-string"


databricks secrets put --scope hotpathscope --key workspaceId --string-value "log-analytics-workspace-id"

databricks secrets put --scope hotpathscope --key omskey --string-value "log-analytics-primary-key"

```


## Create a databricks cluster

You can use the Portal: https://docs.databricks.com/user-guide/clusters/create.html

Or use cli commands: https://docs.databricks.com/api/latest/examples.html

## Install Libraries in the cluster

- Shared libraries documentation: https://docs.databricks.com/user-guide/libraries.html

- Install Maven coordinates

    - Click on *Workspace*, right click on shared and select create library. Select maven as the library source.
    - Install the maven coordinates specified below:

        com.microsoft.azure:azure-cosmosdb-spark_2.2.0_2.11:1.1.1

        com.microsoft.azure:azure-eventhubs-spark_2.11:2.3.5

    - Click on create and then install on cluster for each of the coordinates.

- Upload jar

    - Right click on shared, select create library and select upload:

    - Browse to the jar at `src\HotPathSpark\DroneSparkApplication\DroneManagementWriter\target\scala-2.11\DroneManagementWriter-assembly-0.1.jar`.

    - Click on create and then install on cluster.

## Import notebooks

- Import the following notebooks located at `src\HotPathSpark\DroneSparkApplication`.

    AlertingPipeline.scala
    HotTemperaturePipeline.scala
    NoTelemetryPipeline.scala

- Notebooks documentation: https://docs.databricks.com/user-guide/notebooks/index.html


## Run notebooks

- Open HotTemperaturePipeline.scala and NoTelemetryPipeline.scala by clicking them on the workspace.  replace in 4 places the name replacewitheventhubcompatiblename  with the event hub name compatible name of iot hub as in code snippet below:

    .setEventHubName("replacewitheventhubcompatiblename")

- Open all notebooks and click run all.

## Check telemetry in Log Analytics workspace

- Open log analytics resource, on *Connect a data source* section choose to connect to the *Azure Activity logs*.

- Click on *Logs* and on run the following queries to get the input rows / sec processed rows /sec and the trigger latency execution in miliseconds:

    hottemppipeline_CL
    | where inputRowsPerSecond_d > 0

    hottemppipeline_CL
    | where procRowsPerSecond_d > 0

    hottemppipeline_CL
    | where triggerExecution_d > 0

- You can do the same for hotalertpipeline_CL, notelpipeline_CL and notelalerting_CL.

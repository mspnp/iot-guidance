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


## Create a Resource
Go to the portal and create a resource group or run the command

```bash
az group create --name resourceGroupName --location "ResourceGroupLocation"
```


## Deploy Databricks and Dependent Resources
Open

IotGuidance\src\HotPathSpark\SparkResourcesDeployment\HotPathResourcesDeployment.sln
solution.

Right click on project and select deploy


## Deploy Azure Function application
1-Open IotGuidance\src\HotPath\DroneHotPathFunction.sln solution

2-right click on DroneHotPathFunction project then select Publish. Deploy in the function application created in above step.



## Get Function Key and endpoint
1-Open Portal and click on the function application, then click on function deployed in previous step.

2-click on the function hottelemetry

3-click on function Url to get function endpoint

3-click on manage, then click on click to show under function keys

## Get Cosmosdb key cosmosdb endpoint

1-Open portal click on cosmosdb created on the resource group and click on keys, then click on copy primarary key( read and write) and endpoint.

2-Or run the following az commands.

```Bash
az cosmosdb show --name cosmosdbname -g resourcegroup

 az cosmosdb list -o table
 az cosmosdb list-keys -n cosmosdbname -g resourcegroup
```
 ## Create 2 collections one for hotTelemetry and other for absenceTelemetry
1-run the below command to create the database

```bash
az cosmosdb database create -n cosmosdbname -d hottelemetrydb -g resourcegroup
```

2-run the below command to create collection for absenceTelemetry

```Bash
az cosmosdb collection create -c absencetelemetrycol -n cosmosdbaccount -d hottelemetrydb -g resourcegroup --partition-key-path '/deliveryid'
```

3-run the below command to create collection for hotTelemetry

```Bash
az cosmosdb collection create -c hottelemetrycol -n cosmosdbaccount -d hottelemetrydb -g resourcegroup --partition-key-path '/deliveryid'
```


## Get the eventhub compatible endpoint and eventhub compatible name for iothub
open iothub click on endpoints the click on events

copy event-hub-compatible name and eventHub-compatible-endpoint

## create 6 consumer  groups in iothub
```Bash
az iot hub consumer-group create --hub-name iothubname --resource-group resourcegroupname --name hotpath2

az iot hub consumer-group create --hub-name iothubname --resource-group resourcegroupname --name hotpath3

az iot hub consumer-group create --hub-name iothubname --resource-group resourcegroupname --name hotpath4

az iot hub consumer-group create --hub-name iothubname --resource-group resourcegroupname --name hotpath5

az iot hub consumer-group create --hub-name iothubname --resource-group resourcegroupname --name hotpath6

az iot hub consumer-group create --hub-name iothubname --resource-group resourcegroupname --name hotpath7
```



## Install Scala and SBT from below

 https://www.scala-lang.org/download/2.11.12.html




## Get Event Hub connection-string-primarykey

Open eventHub resource created above. click on shared access policies->RootManageSharedAccessKey and copy the connection string-primary-key

## Get Log analytics workspace id and primary key

Open loganalytics resource created above. Click on Advanced Settings. Copy workspaceid and primary key

## Get the eventhub compatible endpoint and eventhub compatible name for iothub

open iothub compatible endpoint click on endpoints->eventhubconnectionstring

copy event-hub-compatible name and eventHub-compatible-endpoint

## Build DroneManagementWrite Application

This step will build the jar for cosmos writer and loganalytics writer for the spark Application

Go to \HotPathSpark\DroneSparkApplication\DroneManagementWriter and type

 sbt assembly

 this will create the jar files with all dependencies in one jar

 ## Install databricks CLI and set up authentication by following below instructions


 https://docs.azuredatabricks.net/user-guide/dev-tools/databricks-cli.html

## Create the databricks scope and  secrets


```Bash
databricks secrets create-scope --scope hotpathscope

databricks secrets put --scope hotpathscope --key alertingconnstring --string-value "youreventhubconnectionstring"

databricks secrets put --scope hotpathscope --key cosmosdbendpoint --string-value "yourcosmosdbendpoint"

databricks secrets put --scope hotpathscope --key cosmosdbmasterkey --string-value "yourcosmosdbmasterkey"

databricks secrets put --scope hotpathscope --key functionendpoint --string-value "yourfunctionendpoint"

databricks secrets put --scope hotpathscope --key functionkey --string-value "yourfunctionkey"

databricks secrets put --scope hotpathscope --key iothubconnstring --string-value "youreventhubiothubconnstringcompatible"


databricks secrets put --scope hotpathscope --key workspaceId --string-value "yourworkspaceId"

databricks secrets put --scope hotpathscope --key omskey --string-value "youromskey"

```


## Create databricks cluster
you can use the Portal

https://docs.databricks.com/user-guide/clusters/create.html

or use cli commands

https://docs.databricks.com/api/latest/examples.html

## Install Libraries in cluster

# shared Libraries

https://docs.databricks.com/user-guide/libraries.html

Maven coordinates to Install

Right click on shared,select create library. On the drop down select maven coordinates

install below two maven coordinates. you can enter below entries in the text box
and click create library

com.microsoft.azure:azure-cosmosdb-spark_2.1.0_2.11:1.1.2

com.microsoft.azure:azure-eventhubs-spark_2.11:2.3.2

Upload jars
Right click on shared,select create library. On the drop down select upload jars

browse to below location

\IotGuidance\src\HotPathSpark\DroneSparkApplication\DroneManagementWriter\target\scala-2.11\DroneManagementWriter-assembly-0.1.jar

## Import notebooks

follow the instructions to import the following notebooks located at
\IotGuidance\src\HotPath\DroneSparkApplication

AlertingPipeline.scala
HotTemperaturePipeline.scala
NoTelemetryPipeline.scala


https://docs.databricks.com/user-guide/notebooks/index.html


##Run notebooks
open HotTemperaturePipeline.html and NoTelemetryPipeline.html by clicking them on the workspace.  replace in 4 places the name replacewitheventhubcompatiblename  with the event hub name compatible name of iothub as in code snippet below

.setEventHubName("replacewitheventhubcompatiblename")

Open all notebooks and click run all

##check telemetry in loganalytics workspace

Open loganalytics resource then click on logsearch and open a query tab

on the dashboard run below queries to get the input rows / sec processed rows /sec and the trigger latency execution in miliseconds. you can do the same for hotalertpipeline_CL notelpipeline_CL and notelalerting_CL

hottemppipeline_CL
| where inputRowsPerSecond_d > 0

hottemppipeline_CL
| where procRowsPerSecond_d > 0

hottemppipeline_CL
| where triggerExecution_d > 0

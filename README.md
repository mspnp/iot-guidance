# Introduction 

To explore the challenges of building an IoT application, we created a reference implementation called the Drone Delivery application. 

Here is the scenario:

Fabrikam, Inc. runs a drone delivery service. The company manages a fleet of drone aircraft, and customers can request a drone to pick up goods for delivery. The drones send two types of telemetry:

- Flight data: Latitude, longitude, altitude, velocity, and acceleration. Each drone sends this data once every 5 seconds.
- Operating status: Engine temperature and battery level. Each drone sends this data once every 20 seconds.

We assume the drones support IP protocol and MQTT, and that the drones are are mostly-connected devices. That is, they send a constant stream of data while in flight, rather than batching data at intervals.

The following diagram shows the design of the application:

![Diagram of the architecture](./architecture.png)

The architecture includes several data streaming paths that perform different functions:

- **Hot path**. The hot path monitores the drones' operating status to detect anomalies in the engine temperature. It uses Azure Stream Analytics to compute the average engine temperature of each drone over a 2-minute window. Anomolous readings are stored in Cosmos DB and also trigger an alert. The sample application includes stub code for the alerts. You could replace this with code to send an SMS message or a push notification to a mobile app.

- **Warm path**. The warm path uses an Azure Function to write the latest position data for each drone to Cosmos DB. The data can then be queried using geo-spatial queries. For example, you can query for all drones within a given area.

- **Cold path**. The cold path captures all of the raw telemetry and then processes it using HDInsight. The sample application includes a Hive query that summarizes the pickup and dropoff times for each delivery, based on the location data from the drones. You can then use Power BI to explore and visualize the results.

# Provision and Deploy

## Deploy the IoT Hub

On src/CloudGateway/IoTHubDeployment/azuredeploy.parameters.json edit **iothub_name** parameter **value** to specify the name of the IoT Hub.

```bash
cd src/CloudGateway/IoTHubDeployment

az group create -n <resource-group> -l <location>

az group deployment create --name <deployment-name> \
  -g <resource-group> --template-file azuredeploy.json \
  --parameters azuredeploy.parameters.json
```

- [Things/IoT Devices](src/Things/IoTDevices/readme.md)
- [Hot Path](src/HotPath/readme.md)
- [Warm Path](src/WarmPath/readme.md)
- [Cold Path](src/ColdPath/readme.md)

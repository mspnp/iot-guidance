# Deploy warm path batch to Cassandra

1. In the Azure Portal, navigate to the IoT Hub resource and click the **Built-in endpoints** blade. 

2. Under **Events** -> **Consumer groups** enter a name for a new consumer group. Here you will also find the Event Hub values needed as parameters in next steps.

3. Create a *DataStax Enterprise* resource on Azure
    - Take note of the "C* password" which is your Cassandra password.

4. Create the key space and table in Cassandra
   - Connect to a node in your DataStax Enterprise cluster using SSH.
   - Connect to Cassandra using *cqlsh*. Default user is *cassandra*. The password was specified when creating your DataStax Enterprise resource on azure (C* password)

```
cqlsh -u cassandra -p <data-stax-c*-password>
```

   - Create the key space and table

```
CREATE KEYSPACE device_telemetry
WITH replication = {'class': 'SimpleStrategy', 'replication_factor': '2'};

CREATE TABLE device_telemetry.positions1 (
device_id text,
    event_time timestamp,
    location 'PointType',
    PRIMARY KEY (device_id, event_time, location)
);
```

5. In the `src/WarmPathBatchToCassandra/WarmPathDeployment` directory, open the `azuredeploy.parameters.json` file. Add values for the following parameters:

    - `appName`: A name for the Function app (of less than 11 characters of lowercase letter and numbers only).
    - `eventHubConnectionString`: The Event Hub-compatible endpoint connection string.
    - `eventHubName`: The Event Hub-compatible name.
    - `eventHubConsumerGroup`: The name of the Event Hub consumer group.
    - `storageAccountType`: One of "Standard_LRS", "Standard_ZRS", "Standard_GRS", "Standard_RAGRS".
    - `cassandraPassword`: The password specified when creating your DataStax Enterprise resource on azure (C* password).
    - `cassandraContactPoints`: DNS name or IP address of DataStax Enterprise's node/s.

6. Deploy the Azure resources

```bash
cd src/WarmPathBatchToCassandra/WarmPathDeployment

# Deploy the resources
az group deployment create --name <deployment-name> \
  -g <resource-group> --template-file azuredeploy.json \
  --parameters azuredeploy.parameters.json
```

7. Use Visual Studio to publish the *WarmPathFunction* project on `src/WarmPathBatchToCassandra/WarmPathFunction` to the Azure Function app created in step 1.
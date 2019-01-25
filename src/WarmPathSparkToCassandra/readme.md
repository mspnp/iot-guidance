
## DataStax Enterprise

- Create a DataStax Enterprise resource on Azure
    - Take note of the "C* password" which is your Cassandra password that you'll be using to configure the Databricks cluster.

- Connect to a node in your DataStax Enterprise cluster using SSH.

- Generate the byos.properties file using the dse client-tool command.
```bash
    dse client-tool configuration byos-export ~/byos.properties
```
- Logout and copy the byos.properties file you previously generated from the DataStax Enterprise node.
```
    scp <admin-username>@<vm-dns-name>:~/byos.properties .
```
- Copy the byos-version.jar file from the clients directory from a node in your DataStax Enterprise cluster.
```
    scp user@dsenode1.example.com:/usr/share/dse/clients/dse-byos_2.11-6.0.0.jar byos-6.0.jar
```

Note: The byos-version.jar file location depends on the type of installation. If the file is not found connect to the node and run:
```
    sudo find / -type f -name "dse-byos*"
```

## Cassandra

- Connect to a node in your DataStax Enterprise cluster using SSH.

- Connect to cassandra using *cqlsh*. Default user is *cassandra*. The password was specified when creating your DataStax Enterprise resource on azure (C* password)
```
 cqlsh -u cassandra -p <data-stax-c*-password>
 ```

- Create the key space and table

```
CREATE KEYSPACE device_telemetry
WITH replication = {'class': 'SimpleStrategy', 'replication_factor': '1'};

CREATE TABLE device_telemetry.positions2 (
    device_id text,
    event_time text,
    location text,
    PRIMARY KEY (device_id, event_time, location)
);
```

Take note of the key space and table name for future use on the scala notebook


## Azure Databricks

- [Create an Azure Databricks resource](https://docs.azuredatabricks.net/getting-started/try-databricks.html#step-2-create-a-databricks-workspace)

- [Launch the Databricks workspace](https://docs.azuredatabricks.net/getting-started/try-databricks.html#step-3-launch-the-workspace)

- Create Databricks cluster
    - Choose 3.5 LTS the Databricks Runtime Version (includes Apache Spark 2.2.1, Scala 2.11)
    - Copy the configuration from the previously generated *byos.properties* file
        - Add the cassandra username and password:
            spark.cassandra.auth.username cassandra
            spark.cassandra.auth.password <data-stax-c*-password>
        - Remove the line "spark.sql.hive.metastore.sharedPrefixes com.typesafe.scalalogging"

- Import the **azure-eventhubs-spark** library to your Databricks cluster from Maven
    - Go to your workspace and click on Import, that will open the *Import Notebook* dialog. Click on the *click here* link to import a library
    - Choose Maven as the source of your library and click on search
    - Switch to search on *Maven Central*
    - Pate *azure-eventhubs-spark* and choose the *com.microsoft.azure* one and select the *2.2.5* version
    - Attach the library to the cluster

- Import the *byos-version.jar* 
    - Go to your workspace and click on Import, that will open the *Import Notebook* dialog. Click on the *click here* link to import a library
    - Upload the JAR
    - Attach the library to the cluster

- Open src/WarmPathSparkToCassandra/WarmPathSparkToCassandra.scala file and replace the following:
 - Connection string: Event-hub compatible endpoint (from Built-in endpoints)
 - Event hub name: Event-hub compatible name (from Built-in endpoints)
 - Consumer group: created on Step 3 of [WarmPath read me](../WarmPath/readme.md)
 - Cassandra key space and table name: created on a previous step

- Import *WarmPathSparkStreaming.scala* as a Notebook and then run.
    - Go to your workspace and click on Import, that will open the *Import Notebook* dialog
    - Upload *WarmPathSparkStreaming.scala*
    - Attach the notebook to the cluster and the click on *Run All*
# Deploy warm path

For the warm path, there are three versions of the Azure Function, depending on data store technology of choice and throughput requirements:

- WarmPathFunction supports up to 2500 requests/sec, based on our load testing.
- WarmPathFunction_OptimizedForCosmosDb supports more than 2500 requests/sec.
- WarmPathFunction_OptimizedForSqlDb supports more than 2500 requests/sec.

Use the following steps to provision the warm path.

1. Deploy the IoT Hub instance. See the `/src/CloudGateway/IoTHubDeployment` project.

2. In the Azure Portal, navigate to the IoT Hub resource and click the **Built-in endpoints** blade. 

3. Under **Events** -> **Consumer groups** enter a name for a new consumer group for the warm path. Here you will also find the Event Hub values needed as parameters in the next step.

4. In the `/src/WarmPath/WarmPathDeployment` directory, open the `azuredeploy.parameters.json` file. Add values for the following parameters:

    For CosmosDB:

    - `appName`: A name for the Function app (of less than 11 characters of lowercase letter and numbers only).
    - `eventHubConnectionString`: The Event Hub-compatible endpoint connection string.
    - `eventHubName`: The Event Hub-compatible name.
    - `eventHubConsumerGroup`: The name of the Event Hub consumer group.
    - `cosmosDbDatabase`: A name for the Cosmos DB account name.
    - `cosmosDBCollection`: A name for the Cosmos DB collection name.
    - `appInsightsLocation`: Azure region for the Application Insights instance

    For Azure SQL DBatabase:

    - `appName`: A name for the Function app (of less than 11 characters of lowercase letter and numbers only).
    - `eventHubConnectionString`: The Event Hub-compatible endpoint connection string.
    - `eventHubName`: The Event Hub-compatible name.
    - `eventHubConsumerGroup`: The name of the Event Hub consumer group.
    - `SQLDBConnectionString`: A connection string for your Azure SQL Database server.
    - `appInsightsLocation`: Azure region for the Application Insights instance

5. Deploy the Azure resources.

    ```bash
    cd src/WarmPath/WarmPathDeployment

    # Deploy the resources
    az group deployment create --name <deployment-name> \
      -g <resource-group> --template-file azuredeploy.json \
      --parameters azuredeploy.parameters.json
    ```
    
6. Create the Cosmos DB database and collection.

    > Note: This step is only required when deploying the WarmPathFunction_OptimizedForCosmosDb project. If you deploy the WarmPathFunction project, the Function app automatically creates the database and the collection.

    ```bash
    # Get the Cosmos DB account name
    az cosmosdb list -g <resource-group> --query [*].name

    # Create the Cosmos DB database
    # The value of <database-name> must match the cosmosDbDatabase template parameter.
    az cosmosdb database create -g <resource-group> --name <db-account> \
      --db-name <database-name>

    # Create the collection
    # The value of <collection-name> must match the cosmosDBCollection template parameter.
    az cosmosdb collection create -g <resource-group> --name <db-account> \
      --db-name <database-name> -c <collection-name> \
      --partition-key-path /deviceId --throughput 100000
    ```

7. Use Visual Studio to deploy one of the following projects to the Azure Function app:

   - WarmPathFunction supports up to 2500 requests/sec.
   - WarmPathFunction_OptimizedForCosmosDb supports > 2500 requests/sec.
   - WarmPathFunction_OptimizedForSqlDb supports > 2500 requests/sec.

  Right click on the project and click *Publish...*. Select the function app created in the previous step.

Use Application Insights Analytics to understand the execution of the WarmPath Azure Function app.

```
# Degree of paralalization in the optimized path
traces | where message startswith "Starting Upserts with " | order by timestamp desc

# Errors
traces | where message startswith "Error processing message" | order by timestamp desc
```